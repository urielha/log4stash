using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using log4net.ElasticSearch.SmartFormatters;
using log4net.ElasticSearch.Extensions;
using log4net.Util;
using log4net.Appender;
using log4net.Core;

namespace log4net.ElasticSearch
{
    public class ElasticSearchAppender : AppenderSkeleton
    {
        private static readonly string MachineName = Environment.MachineName;

        private List<InnerBulkOperation> _bulk = new List<InnerBulkOperation>();
        private IElasticsearchClient _client;
        private LogEventSmartFormatter _indexName;
        private LogEventSmartFormatter _indexType;

        private readonly Timer _timer;

        public FixFlags FixedFields { get; set; }

        public int BulkSize { get; set; }
        public int BulkIdleTimeout { get; set; }
        public int TimeoutToWaitForTimer { get; set; }

        // elastic configuration
        public string Server { get; set; }
        public int Port { get; set; }
        public bool IndexAsync { get; set; }
        public int MaxAsyncConnections { get; set; }
        public TemplateInfo Template { get; set; }
        public ElasticAppenderFilters ElasticFilters { get; set; }

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

            BulkSize = 2000;
            BulkIdleTimeout = 5000;
            TimeoutToWaitForTimer = 5000;

            Server = "localhost";
            Port = 9200;
            IndexName = "LogEvent-%{+yyyy.MM.dd}";
            IndexType = "LogEvent";
            IndexAsync = true;
            MaxAsyncConnections = 10;
            Template = null;

            _timer = new Timer(TimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            ElasticFilters = new ElasticAppenderFilters();
        }

        public override void ActivateOptions()
        {
            _client = new WebElasticClient(Server, Port);

            if (Template != null && Template.IsValid)
            {
                _client.PutTemplateRaw(Template.Name, File.ReadAllText(Template.FileName));
            }

            ElasticFilters.PrepareConfiguration(_client);

            RestartTimer();
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

            var logEvent = CreateLogEvent(loggingEvent);
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

        /// <summary>
        /// Send the bulk to Elasticsearch and creating new bluk.
        /// </summary>
        private void DoIndexNow()
        {
            // avoid blocking further inserts by creating new bulk before the lock
            var bulkToSend = _bulk;
            _bulk = new List<InnerBulkOperation>();

            try
            {
                lock (bulkToSend)
                {
                    // I didnt use double-check in purpose.
                    if (bulkToSend.Count == 0)
                    {
                        return;
                    }

                    if (IndexAsync)
                    {
                        _client.IndexBulkAsync(bulkToSend);
                    }
                    else
                    {
                        _client.IndexBulk(bulkToSend);
                    }
                }
            }
            catch (Exception ex)
            {
                LogLog.Error(GetType(), "Invalid connection to ElasticSearch", ex);
            }
        }

        private Dictionary<string, object> CreateLogEvent(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                throw new ArgumentNullException("loggingEvent");
            }

            var logEvent = new Dictionary<string, object>();

            logEvent["@timestamp"] = loggingEvent.TimeStamp.ToUniversalTime().ToString("O");
            logEvent["LoggerName"] = loggingEvent.LoggerName;
            logEvent["HostName"] = MachineName;

            if (FixedFields.ContainsFlag(FixFlags.ThreadName))
            {
                logEvent["ThreadName"] = loggingEvent.ThreadName;
            }

            if (FixedFields.ContainsFlag(FixFlags.Message) && loggingEvent.MessageObject != null)
            {
                logEvent["Message"] = loggingEvent.MessageObject.ToString();
                //logEvent["Message"] = loggingEvent.RenderedMessage;
            }

            if (FixedFields.ContainsFlag(FixFlags.Exception) && loggingEvent.ExceptionObject != null)
            {
                logEvent["Exception"] = loggingEvent.ExceptionObject.ToString();
            }

            if (FixedFields.ContainsFlag(FixFlags.Domain))
            {
                logEvent["AppDomain"] = loggingEvent.Domain;
            }

            if (loggingEvent.Level != null)
            {
                logEvent["Level"] = loggingEvent.Level.DisplayName;
            }

            if (FixedFields.ContainsFlag(FixFlags.Identity))
            {
                logEvent["Identity"] = loggingEvent.Identity;
            }

            if (FixedFields.ContainsFlag(FixFlags.UserName))
            {
                logEvent["UserName"] = loggingEvent.UserName;
            }

            if (FixedFields.ContainsFlag(FixFlags.LocationInfo) && loggingEvent.LocationInformation != null)
            {
                var locationInfo = new Dictionary<string, object>();
                logEvent["LocationInformation"] = locationInfo;

                locationInfo["ClassName"] = loggingEvent.LocationInformation.ClassName;
                locationInfo["FileName"] = loggingEvent.LocationInformation.FileName;
                locationInfo["LineNumber"] = loggingEvent.LocationInformation.LineNumber;
                locationInfo["FullInfo"] = loggingEvent.LocationInformation.FullInfo;
                locationInfo["MethodName"] = loggingEvent.LocationInformation.MethodName;
            }

            if (FixedFields.ContainsFlag(FixFlags.Properties))
            {
                var properties = loggingEvent.GetProperties();
                foreach (var propertyKey in properties.GetKeys())
                {
                    logEvent.AddOrSet(propertyKey, properties[propertyKey].ToString());
                }
            }
            return logEvent;
        }
    }
}
