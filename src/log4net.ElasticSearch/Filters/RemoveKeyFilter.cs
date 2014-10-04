using System.Collections.Generic;
using log4net.ElasticSearch.SmartFormatters;

namespace log4net.ElasticSearch.Filters
{
    public class RemoveKeyFilter : IElasticAppenderFilter
    {
        private LogEventSmartFormatter _key;

        [PropertyNotEmpty]
        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public void PrepareConfiguration(IElasticsearchClient client)
        {
        }

        public void PrepareEvent(Dictionary<string, object> logEvent)
        {
            logEvent.Remove(_key.Format(logEvent));
        }
    }
}