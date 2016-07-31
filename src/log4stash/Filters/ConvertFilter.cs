using System;
using System.Collections.Generic;
using log4stash.SmartFormatters;
using converterKeyValuePair = System.Collections.Generic.KeyValuePair
                                <log4stash.SmartFormatters.LogEventSmartFormatter,
                                 System.Func<object, object>>;

namespace log4stash.Filters
{
    /// <summary>
    /// Convert Filter
    /// Convert from an object at a given key to:
    ///    - <see cref="Convert.ToString(object)">to string</see>
    ///    - <see cref="ConvertToLower">to lowercase string</see>
    ///    - <see cref="ConvertToUpper">to uppercase string</see>
    ///    - to array using the <see cref="ConvertToArrayFilter">ConvertToArrayFilter</see>
    /// </summary>
    public class ConvertFilter : IElasticAppenderFilter
    {

        private readonly List<converterKeyValuePair> _converters;

        public ConvertFilter()
        {
            _converters = new List<converterKeyValuePair>();
        }

        public void AddToString(string sourceKey)
        {
            _converters.Add(new converterKeyValuePair(sourceKey, Convert.ToString));
        }

        public void AddToLower(string sourceKey)
        {
            _converters.Add(new converterKeyValuePair(sourceKey, ConvertToLower));
        }

        public void AddToUpper(string sourceKey)
        {
             _converters.Add(new converterKeyValuePair(sourceKey, ConvertToUpper));
        }

        public void AddToArray(ConvertToArrayFilter toArrayFilter)
        {
             _converters.Add(new converterKeyValuePair(toArrayFilter.SourceKey, toArrayFilter.ValueToArrayObject));
        }

        public void PrepareConfiguration(IElasticsearchClient client)
        {
        }

        public void PrepareEvent(Dictionary<string, object> logEvent)
        {
            foreach (var converterPair in _converters)
            {
                var key = converterPair.Key.Format(logEvent);
                object value;

                if (logEvent.TryGetValue(key, out value))
                {
                    logEvent[key] = converterPair.Value(value);
                }
            }
        }

        private object ConvertToLower(object arg)
        {
            return Convert.ToString(arg).ToLower();
        }

        private object ConvertToUpper(object arg)
        {
            return Convert.ToString(arg).ToUpper();
        }
    }
}
