using System.Collections.Generic;
using log4net.ElasticSearch.Extensions;
using log4net.ElasticSearch.JsonConverters;
using log4net.ElasticSearch.SmartFormatters;

namespace log4net.ElasticSearch.Filters
{
    public class JsonStringFilter : IElasticAppenderFilter
    {
        private LogEventSmartFormatter _key;

        [PropertyNotEmpty]
        public string SourceKey
        {
            get { return _key; }
            set { _key = value; }
        }

        public void PrepareConfiguration(IElasticsearchClient client)
        {

        }

        public void PrepareEvent(Dictionary<string, object> logEvent)
        {
            var key = _key.Format(logEvent);
            string value;
            if (logEvent.TryGetStringValue(key, out value))
            {
                logEvent[key] = new StringJsonConverter.JsonString(value);
            }
        }
    }
}
