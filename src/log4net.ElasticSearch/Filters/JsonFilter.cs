using System.Collections.Generic;
using log4net.ElasticSearch.Extensions;
using log4net.ElasticSearch.SmartFormatters;
using Newtonsoft.Json.Linq;

namespace log4net.ElasticSearch.Filters
{
    public class JsonFilter : IElasticAppenderFilter
    {
        private LogEventSmartFormatter _sourceKey;

        [PropertyNotEmpty]
        public string SourceKey
        {
            get { return _sourceKey; }
            set { _sourceKey = value; }
        }

        public JsonFilter()
        {
            SourceKey = "JsonRaw";
        }

        public void PrepareConfiguration(IElasticsearchClient client)
        {
        }

        public void PrepareEvent(Dictionary<string, object> logEvent)
        {
            var key = _sourceKey.Format(logEvent);
            string input;
            if (!logEvent.TryGetStringValue(key, out input))
                return;

            var token = JToken.Parse(input);
            logEvent[key] = token;
        }
    }
}
