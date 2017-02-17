using System.Collections.Generic;
using System.Xml;
using log4stash.Extensions;
using log4stash.SmartFormatters;
using Newtonsoft.Json;

namespace log4stash.Filters
{
    public class XmlFilter : IElasticAppenderFilter
    {
        private LogEventSmartFormatter _sourceKey;
        private JsonFilter _jsonFilter;

        [PropertyNotEmpty]
        public string SourceKey
        {
            get { return _sourceKey; }
            set { _sourceKey = value; }
        }

        public bool FlattenXml { get; set; }

        public XmlFilter()
        {
            SourceKey = "XmlRaw";
            FlattenXml = false;
        }

        public void PrepareConfiguration(IElasticsearchClient client)
        {
            _jsonFilter = new JsonFilter {FlattenJson = FlattenXml, SourceKey = SourceKey};
            _jsonFilter.PrepareConfiguration(client);
        }

        public void PrepareEvent(Dictionary<string, object> logEvent)
        {
            var key = _sourceKey.Format(logEvent);
            string input;
            if (!logEvent.TryGetStringValue(key, out input))
            {
                return;
            }

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(input);
            var jsonDoc = JsonConvert.SerializeXmlNode(xmlDoc);
            logEvent[key] = jsonDoc;
            _jsonFilter.PrepareEvent(logEvent);
        }
    }
}
