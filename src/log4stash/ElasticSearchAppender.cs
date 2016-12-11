using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using log4stash.LogEventFactory;
using log4stash.SmartFormatters;
using log4net.Util;
using log4net.Appender;
using log4net.Core;
using log4stash.Authentication;
using log4stash.Configuration;

namespace log4stash
{
    public class ElasticSearchAppender : AppenderSkeleton, ILogEventFactoryParams
    {
        private List<InnerBulkOperation> _bulk = new List<InnerBulkOperation>();
        private IElasticsearchClient _client;
        private LogEventSmartFormatter _indexName;
        private LogEventSmartFormatter _indexType;

        private readonly Timer _timer;
        private bool _finishedPuttingTemplate;

        public FixFlags FixedFields { get; set; }
        public bool SerializeObjects { get; set; }

        public int BulkSize { get; set; }
        public int BulkIdleTimeout { get; set; }
        public int TimeoutToWaitForTimer { get; set; }

        // elastic configuration
        public string Server { get; set; }
        public int Port { get; set; }
        public ServerDataCollection Servers { get; set; }
        public int ElasticSearchTimeout { get; set; }
        public bool Ssl { get; set; }
        public bool AllowSelfSignedServerCert { get; set; }
        public AuthenticationMethodChooser AuthenticationMethod { get; set; }
        public bool IndexAsync { get; set; }
        public TemplateInfo Template { get; set; }
        public ElasticAppenderFilters ElasticFilters { get; set; }
        public ILogEventFactory LogEventFactory { get; set; }

        public string IndexName
        {
            set { _indexName = value; }
        }

        public string IndexType
        {
            set { _indexType = value; }
        }

        public ElasticSearchAppender()
        {
            FixedFields = FixFlags.Partial;
            SerializeObjects = true;

            BulkSize = 2000;
            BulkIdleTimeout = 5000;
            TimeoutToWaitForTimer = 5000;

            Servers = new ServerDataCollection();
            ElasticSearchTimeout = 10000;
            IndexName = "LogEvent-%{+yyyy.MM.dd}";
            IndexType = "LogEvent";
            IndexAsync = true;
            Template = null;
            LogEventFactory = new BasicLogEventFactory();

            _timer = new Timer(TimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            ElasticFilters = new ElasticAppenderFilters();

            AllowSelfSignedServerCert = false;
            Ssl = false;
            AuthenticationMethod = new AuthenticationMethodChooser();
            _finishedPuttingTemplate = false;
        }

        public override void ActivateOptions()
        {
            AddOptionalServer();
            _client = new WebElasticClient(Servers, ElasticSearchTimeout, Ssl, AllowSelfSignedServerCert, AuthenticationMethod);

            LogEventFactory.Configure(this);

            if (Template != null && Template.IsValid)
            {
                _finishedPuttingTemplate = !Template.ForcePut ||
                    _client.PutTemplateRaw(Template.Name, File.ReadAllText(Template.FileName));
            }
            else
            {
                _finishedPuttingTemplate = true;
            }

            ElasticFilters.PrepareConfiguration(_client);

            RestartTimer();
        }

        private void AddOptionalServer()
        {
            if (Server != null && Port != 0)
            {
                var serverData = new ServerData { Address = Server, Port = Port };
                Servers.Add(serverData);
            }
        }

        private void RestartTimer()
        {
            var timeout = TimeSpan.FromMilliseconds(BulkIdleTimeout);
            _timer.Change(timeout, timeout);
        }

        /// <summary>
        /// On case of error or when the appender is closed before loading configuration change.
        /// </summary>
        protected override void OnClose()
        {
            DoIndexNow();

            // let the timer finish its job
            WaitHandle notifyObj = new AutoResetEvent(false);
            _timer.Dispose(notifyObj);
            notifyObj.WaitOne(TimeoutToWaitForTimer);
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

            var logEvent = LogEventFactory.CreateLogEvent(loggingEvent);
            PrepareAndAddToBulk(logEvent);

            if (_bulk.Count >= BulkSize && BulkSize > 0)
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
            ElasticFilters.PrepareEvent(logEvent);

            var indexName = _indexName.Format(logEvent).ToLower();
            var indexType = _indexType.Format(logEvent);

            var operation = new InnerBulkOperation
            {
                Document = logEvent,
                IndexName = indexName,
                IndexType = indexType
            };

            lock (_bulk)
            {
                _bulk.Add(operation);
            }
        }

        public void TimerElapsed(object state)
        {
            DoIndexNow();
        }

        private void VerifyTemplateIsPut()
        {
            if (!_finishedPuttingTemplate)
            {
                PutTemplateToElastic();
            }
        }

        /// <summary>
        /// Send the bulk to Elasticsearch and creating new bluk.
        /// </summary>
        private void DoIndexNow()
        {
            var bulkToSend = Interlocked.Exchange(ref _bulk, new List<InnerBulkOperation>());
            VerifyTemplateIsPut();
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
