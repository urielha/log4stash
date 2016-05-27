using System.Collections.Generic;
using log4stash.Extensions;
using log4stash.SmartFormatters;

namespace log4stash.Filters
{
    public class AddValueFilter : IElasticAppenderFilter
    {
        private LogEventSmartFormatter _key;
        private LogEventSmartFormatter _value;

        [PropertyNotEmpty]
        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        [PropertyNotEmpty]
        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public bool Overwrite { get; set; }

        public void PrepareConfiguration(IElasticsearchClient client)
        {
        }

        public void PrepareEvent(Dictionary<string, object> logEvent)
        {
            var key = _key.Format(logEvent);
            var value = _value.Format(logEvent);

            if (Overwrite)
            {
                logEvent[key] = value;
            }
            else
            {
                logEvent.AddOrSet(key, value);
            }
        }
    }
}