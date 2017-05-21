using log4stash.SmartFormatters;
using System.Collections.Generic;
using System.Linq;

namespace log4stash.Configuration
{
    public class IndexOperationParamsDictionary
    {
        private readonly IDictionary<string, LogEventSmartFormatter> _parametersDictionary;

        public IndexOperationParamsDictionary()
        {
            _parametersDictionary = new Dictionary<string, LogEventSmartFormatter>();
        }

        public void AddParameter(IndexOperationParam parameter)
        {
            _parametersDictionary[parameter.Key] = parameter.Value;
        }

        public Dictionary<string, string> ToDictionary(Dictionary<string, object> logEvent)
        {
            return _parametersDictionary.ToDictionary(
                param => param.Key,
                param => param.Value.Format(logEvent));
        }

        // For Testing
        public bool TryGetValue(string key, out string stringValue)
        {
            stringValue = null;
            LogEventSmartFormatter value;
            if(_parametersDictionary.TryGetValue(key, out value))
            {
                stringValue = value.Raw;
                return true;
            }
            return false;
        }

        public void Remove(string key)
        {
            _parametersDictionary.Remove(key);
        }
    }
}
