using System;
using System.Collections.Generic;
using log4net.Util;
using log4stash.ErrorHandling;
using log4stash.SmartFormatters;

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

        private readonly List<ConverterDetails> _converters;
        private readonly IExternalEventWriter _eventWriter;

        public ConvertFilter() : this(new LogLogEventWriter())
        {
            _converters = new List<ConverterDetails>();
        }

        public ConvertFilter(IExternalEventWriter eventWriter)
        {
            _eventWriter = eventWriter;
        }

        public void AddToString(string sourceKey)
        {
            _converters.Add(new ConverterDetails(sourceKey, Convert.ToString));
        }

        public void AddToLower(string sourceKey)
        {
            _converters.Add(new ConverterDetails(sourceKey, ConvertToLower));
        }

        public void AddToUpper(string sourceKey)
        {
            _converters.Add(new ConverterDetails(sourceKey, ConvertToUpper));
        }

        public void AddToInt(string sourceKey)
        {
            _converters.Add(new ConverterDetails(sourceKey, ConvertToInt));
        }

        public void AddToArray(ConvertToArrayFilter toArrayFilter)
        {
            _converters.Add(new ConverterDetails(toArrayFilter.SourceKey, toArrayFilter.ValueToArrayObject));
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
                    logEvent[key] = converterPair.ConvertFunc(value);
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

        private object ConvertToInt(object arg)
        {
            if (arg is int)
            {
                return arg;
            }

            int num;
            if (int.TryParse(arg.ToString(), out num))
            {
                return num;
            }

            LogLog.Warn(GetType(), 
                string.Format("Could not convert {0} of type: {1} to int", arg, arg.GetType()));
            return 0;
        }
    }

    class ConverterDetails
    {
        public LogEventSmartFormatter Key { get; private set; }
        public Func<object, object> ConvertFunc { get; private set; }

        public ConverterDetails(LogEventSmartFormatter key, Func<object, object> convertFunc)
        {
            Key = key;
            ConvertFunc = convertFunc;
        }
    }
}
