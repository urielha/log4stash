using System;
using System.Collections.Generic;
using System.Threading;
using log4stash.Extensions;
using log4stash.SmartFormatters;
using log4net.Appender;
using log4net.Core;
using log4stash.Authentication;
using log4stash.Bulk;
using log4stash.Configuration;
using log4stash.ElasticClient;
using log4stash.ErrorHandling;
using log4stash.FileAccess;
using log4stash.LogEvent;
using log4stash.Timing;

namespace log4stash
{
    public class ElasticSearchAppender : AppenderSkeleton
    {

        private readonly ILogBulkSet _bulk;
        private readonly IFileAccessor _fileAccessor;
        private readonly IExternalEventWriter _eventWriter;
        private IElasticsearchClient _client;
        private readonly IElasticClientFactory _elasticClientFactory;
        private LogEventSmartFormatter _indexName;
        private LogEventSmartFormatter _indexType;
        private TolerateCallsBase _tolerateCalls;
        private ILogEventConverter _logEventConverter;
        private readonly ILogEventConverterFactory _logEventConverterFactory;
        private readonly IIndexingTimer _timer;
        private readonly ITolerateCallsFactory _tolerateCallsFactory;

        

        [Obsolete]
        public string DocumentIdSource
        {
            set
            {
                IndexOperationParams.AddParameter(new IndexOperationParam("_id", string.Format("{0}{1}{2}", "%{", value, "}" )));
            }
        }
        public FixFlags FixedFields { get; set; }
        public bool SerializeObjects { get; set; }
        public IndexOperationParamsDictionary IndexOperationParams { get; set; }
        public int BulkSize { get; set; }
        public int BulkIdleTimeout { get; set; }
        public int TimeoutToWaitForTimer { get; set; }

        public int TolerateLogLogInSec
        {
            set
            {
                _tolerateCalls = _tolerateCallsFactory.Create(value);
            }
        }

        // elastic configuration
        public string Server { get; set; }
        public int Port { get; set; }
        public string Path { get; set; }
        public IServerDataCollection Servers { get; set; }
        public int ElasticSearchTimeout { get; set; }
        public bool Ssl { get; set; }
        public bool AllowSelfSignedServerCert { get; set; }
        public IAuthenticationMethodChooser AuthenticationMethod { get; set; }
        public bool IndexAsync { get; set; }
        public TemplateInfo Template { get; set; }
        public IElasticAppenderFilter ElasticFilters { get; set; }
        public bool DropEventsOverBulkLimit { get; set; }
        [Obsolete]
        public string BasicAuthUsername { get; set; }
        [Obsolete]
        public string BasicAuthPassword { get; set; }

        public string IndexName
        {
            set { _indexName = value; }
            get { return _indexName.ToString();  }
        }

        public string IndexType
        {
            set { _indexType = value; }
            get { return _indexType.ToString(); }
        }

        public ElasticSearchAppender()
            : this(new WebElasticClientFactory(), "LogEvent-%{+yyyy.MM.dd}",
                "LogEvent", new IndexingTimer(Timeout.Infinite) { WaitTimeout = 5000 },
                new TolerateCallsFactory(), new LogBulkSet(),
                new BasicLogEventConverterFactory(), new ElasticAppenderFilters(),
                new BasicFileAccessor(), new LogLogEventWriter())
        {
        }

        public ElasticSearchAppender(IElasticClientFactory clientFactory, LogEventSmartFormatter indexName,
            LogEventSmartFormatter indexType, IIndexingTimer timer, ITolerateCallsFactory tolerateCallsFactory,
            ILogBulkSet bulk, ILogEventConverterFactory logEventConverterFactory, IElasticAppenderFilter elasticFilters,
            IFileAccessor fileAccessor, IExternalEventWriter eventWriter)
        {
            _logEventConverterFactory = logEventConverterFactory;
            _elasticClientFactory = clientFactory;
            IndexName = indexName;
            IndexType = indexType;
            _timer = timer;
            _timer.Elapsed += (o,e) => DoIndexNow();
            _tolerateCallsFactory = tolerateCallsFactory;
            _bulk = bulk;
            _fileAccessor = fileAccessor;
            _eventWriter = eventWriter;

            FixedFields = FixFlags.Partial;
            SerializeObjects = true;
            BulkSize = 2000;
            BulkIdleTimeout = 5000;
            DropEventsOverBulkLimit = false;
            TimeoutToWaitForTimer = 5000;
            ElasticSearchTimeout = 10000;
            IndexAsync = true;
            Template = null;
            AllowSelfSignedServerCert = false;
            Ssl = false; 
            _tolerateCalls = _tolerateCallsFactory.Create(0);
            Servers = new ServerDataCollection();
            ElasticFilters = elasticFilters;
            AuthenticationMethod = new AuthenticationMethodChooser();
            IndexOperationParams = new IndexOperationParamsDictionary();
        }

        public override void ActivateOptions()
        {
            AddOptionalServer();
            CheckObsoleteAuth();

            _client = _elasticClientFactory.CreateClient(Servers, ElasticSearchTimeout, Ssl, AllowSelfSignedServerCert, AuthenticationMethod);

            _logEventConverter = _logEventConverterFactory.Create(FixedFields, SerializeObjects);

            if (Template != null && Template.IsValid)
            {
                if (IndexAsync)
                {
                    _client.PutTemplateRaw(Template.Name, _fileAccessor.ReadAllText(Template.FileName));
                }
                else
                {
                    _client.PutTemplateRaw(Template.Name, _fileAccessor.ReadAllText(Template.FileName));
                }
            }

            ElasticFilters.PrepareConfiguration(_client);

            _timer.Restart(BulkIdleTimeout);
        }

        private void AddOptionalServer()
        {
            if (!string.IsNullOrEmpty(Server) && Port != 0)
            {
                var serverData = new ServerData { Address = Server, Port = Port, Path = Path };
                Servers.Add(serverData);
            }
        }
        private void CheckObsoleteAuth()
        {
            if(!string.IsNullOrEmpty(BasicAuthUsername) && !string.IsNullOrEmpty(BasicAuthPassword))
            {
                _eventWriter.Warn(GetType(), "BasicAuthUsername & BasicAuthPassword tags are obsolete, Please use AuthenticationMethod new tag");
                var auth = new BasicAuthenticationMethod { Username = BasicAuthUsername, Password = BasicAuthPassword };
                AuthenticationMethod.AddBasic(auth);
            }
        }

        /// <summary>
        /// On case of error or when the appender is closed before loading configuration change.
        /// </summary>
        protected override void OnClose()
        {
            DoIndexNow();

            // let the timer finish its job
            _timer.Dispose();
            _client.Dispose();
        }

        /// <summary>
        /// Add a log event to the ElasticSearch Repo
        /// </summary>
        /// <param name="loggingEvent"></param>
        protected override void Append(LoggingEvent loggingEvent)
        {
            if (_client == null || loggingEvent == null)
            {
                return;
            }

            if (DropEventsOverBulkLimit && _bulk.Count >= BulkSize)
            {
                _tolerateCalls.Call(() =>
                    _eventWriter.Warn(GetType(),
                        "Message lost due to bulk overflow! Set DropEventsOverBulkLimit to false in order to prevent that."),
                    GetType(), 0);
                return;
            }

            var logEvent = _logEventConverter.ConvertLogEventToDictionary(loggingEvent);
            PrepareAndAddToBulk(logEvent);

            if (!DropEventsOverBulkLimit && _bulk.Count >= BulkSize && BulkSize > 0)
            {
                DoIndexNow();
            }
        }

        /// <summary>
        /// Prepare the event and add it to the BulkDescriptor.
        /// </summary>
        /// <param name="logEvent"></param>
        private void PrepareAndAddToBulk(Dictionary<string, object> logEvent)
        {
            logEvent.ApplyFilter(ElasticFilters);
            _bulk.AddEventToBulk(logEvent, _indexName, _indexType, IndexOperationParams);
        }

        /// <summary>
        /// Send the bulk to Elasticsearch and creating new bluk.
        /// </summary>
        private void DoIndexNow()
        {
            var bulkToSend = _bulk.ResetBulk();
            if (bulkToSend.Count <= 0) return;
            try
            {
                if (IndexAsync)
                {
                    _client.IndexBulkAsync(bulkToSend);
                }
                else
                {
                    _client.IndexBulk(bulkToSend);
                }
            }
            catch (Exception ex)
            {
                _eventWriter.Error(GetType(), "IElasticsearchClient inner exception occurred", ex);
            }
        }
    }
}
