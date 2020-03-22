using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using log4stash.Extensions;
using log4stash.LogEventFactory;
using log4stash.SmartFormatters;
using log4net.Util;
using log4net.Appender;
using log4net.Core;
using log4stash.Authentication;
using log4stash.Bulk;
using log4stash.Configuration;
using log4stash.Timing;
using RestSharp.Authenticators;

namespace log4stash
{
    public class ElasticSearchAppender : AppenderSkeleton, ILogEventFactoryParams
    {

        private ILogBulkSet _bulk;
        private IElasticsearchClient _client;
        private LogEventSmartFormatter _indexName;
        private LogEventSmartFormatter _indexType;
        private TolerateCallsBase _tolerateCalls;

        private readonly IIndexingTimer _timer;
        private readonly ITolerateCallsFactory _tolerateCallsFactory;

        public FixFlags FixedFields { get; set; }
        public bool SerializeObjects { get; set; }

        [Obsolete]
        public string DocumentIdSource
        {
            set
            {
                IndexOperationParams.AddParameter(new IndexOperationParam("_id", string.Format("{0}{1}{2}", "%{", value, "}" )));
            }
        }

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
        public ILogEventFactory LogEventFactory { get; set; }
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
        {
            FixedFields = FixFlags.Partial;
            SerializeObjects = true;
            BulkSize = 2000;
            BulkIdleTimeout = 5000;
            DropEventsOverBulkLimit = false;
            TimeoutToWaitForTimer = 5000;

            _tolerateCallsFactory = new TolerateCallsFactory();
            _tolerateCalls = _tolerateCallsFactory.Create(0);

            Servers = new ServerDataCollection();
            ElasticSearchTimeout = 10000;
            IndexName = "LogEvent-%{+yyyy.MM.dd}";
            IndexType = "LogEvent";
            IndexAsync = true;
            Template = null;
            LogEventFactory = new BasicLogEventFactory();

            _timer = new IndexingTimer(Timeout.Infinite) { WaitTimeout = 5000};
            _timer.Elapsed += (o, e) => DoIndexNow();
            ElasticFilters = new ElasticAppenderFilters();

            AllowSelfSignedServerCert = false;
            Ssl = false;
            AuthenticationMethod = new AuthenticationMethodChooser();
            IndexOperationParams = new IndexOperationParamsDictionary();
            _bulk = new LogBulkSet();
        }

        public ElasticSearchAppender(IElasticsearchClient client, LogEventSmartFormatter indexName, LogEventSmartFormatter indexType, IIndexingTimer timer, ITolerateCallsFactory tolerateCallsFactory, ILogBulkSet bulk)
        {
            _client = client;
            _indexName = indexName;
            _indexType = indexType;
            _timer = timer;
            _timer.Elapsed += (o,e) => DoIndexNow();
            _tolerateCallsFactory = tolerateCallsFactory;
            _bulk = bulk;
        }

        public override void ActivateOptions()
        {
            AddOptionalServer();
            CheckObsoleteAuth();
            _client = new WebElasticClient(Servers, ElasticSearchTimeout, Ssl, AllowSelfSignedServerCert, AuthenticationMethod);

            LogEventFactory.Configure(this);

            if (Template != null && Template.IsValid)
            {
                _client.PutTemplateRaw(Template.Name, File.ReadAllText(Template.FileName));
            }

            ElasticFilters.PrepareConfiguration(_client);

            RestartTimer();
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
                LogLog.Warn(GetType(), "BasicAuthUsername & BasicAuthPassword tags are obsolete, Please use AuthenticationMethod new tag");
                var auth = new BasicAuthenticationMethod { Username = BasicAuthUsername, Password = BasicAuthPassword };
                AuthenticationMethod.AddBasic(auth);
            }
        }

        private void RestartTimer()
        {
            _timer.Restart(BulkIdleTimeout);
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
                    LogLog.Warn(GetType(),
                        "Message lost due to bulk overflow! Set DropEventsOverBulkLimit to false in order to prevent that."),
                    GetType(), 0);
                return;
            }

            var logEvent = LogEventFactory.CreateLogEvent(loggingEvent);
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
            if (bulkToSend.Count > 0)
            {
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
                    LogLog.Error(GetType(), "IElasticsearchClient inner exception occurred", ex);
                }
            }
        }
    }
}
