using System.Collections.Generic;
using log4stash.Extensions;
using log4stash.SmartFormatters;
using Newtonsoft.Json.Linq;

namespace log4stash.Filters
{
    public class JsonFilter : IElasticAppenderFilter
    {
        public static readonly string DefaultSeparator = "_";
        private LogEventSmartFormatter _sourceKey;

        [PropertyNotEmpty]
        public string SourceKey
        {
            get { return _sourceKey; }
            set { _sourceKey = value; }
        }

        public bool FlattenJson { get; set; }

        public string Separator { get; set; }

        public JsonFilter()
        {
            SourceKey = "JsonRaw";
            FlattenJson = false;
            Separator = DefaultSeparator;
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
            if (FlattenJson)
            {
                ScanToken(logEvent, token, "");
            }
            else
            {
                logEvent[key] = token;
            }
        }

        private void ScanToken(IDictionary<string, object> logEvent, JToken token, string prefix)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in token.Children<JProperty>())
                    {
                        ScanToken(logEvent, prop.Value, Join(prefix, prop.Name));
                    }
                    break;

                case JTokenType.Array:
                    var index = 0;
                    foreach (var child in token.Children())
                    {
                        ScanToken(logEvent, child, Join(prefix, index.ToString()));
                        index++;
                    }
                    break;

                default:
                    var value = ((JValue)token).Value;
                    if (value != null)
                        logEvent.Add(prefix, ((JValue)token).Value);
                    break;
            }
        }

        private string Join(string prefix, string name)
        {
            return (string.IsNullOrEmpty(prefix) ? name : string.Concat(prefix, Separator, name));
        }
    }
}
