using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using log4stash.Extensions;
using log4stash.SmartFormatters;

namespace log4stash.Filters
{
    public class ConvertToArrayFilter : IElasticAppenderFilter
    {
        private Regex _seperateRegex;
        private LogEventSmartFormatter _sourceKey;

        [PropertyNotEmpty]
        public string SourceKey
        {
            get { return _sourceKey; }
            set { _sourceKey = value; }
        }

        [PropertyNotEmpty]
        public string Seperators
        {
            get { return _seperateRegex != null ? _seperateRegex.ToString() : string.Empty; }
            set { _seperateRegex = new Regex("[" + value + "]+", RegexOptions.Compiled | RegexOptions.Multiline); }
        }

        public ConvertToArrayFilter()
        {
            SourceKey = "Message";
            Seperators = ", ";
        }

        public void PrepareConfiguration(IElasticsearchClient client)
        {
        }

        public void PrepareEvent(Dictionary<string, object> logEvent)
        {
            string formattedKey = _sourceKey.Format(logEvent);
            string value;
            if (!logEvent.TryGetStringValue(formattedKey, out value))
            {
                return;
            }

            logEvent[formattedKey] = _seperateRegex.Split(value).Where(s => !string.IsNullOrEmpty(s)).ToList();
        }
    }
}
