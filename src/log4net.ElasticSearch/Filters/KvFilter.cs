using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using log4net.ElasticSearch.Models;

namespace log4net.ElasticSearch.Filters
{
    public class KvFilter : IElasticAppenderFilter
    {
        private const string FailedKv = "KvFilterFailed";
        private Regex _kvRegex;
        private string _trimValue;
        private string _trimKey;

        [PropertyNotEmpty]
        public string ValueSplit { get; set; }
        [PropertyNotEmpty]
        public string FieldSplit { get; set; }
        [PropertyNotEmpty]
        public string SourceKey { get; set; }

        public string TrimValue
        {
            get { return _trimValue; }
            set { _trimValue = value; }
        }

        public string TrimKey
        {
            get { return _trimKey; }
            set { _trimKey = value; }
        }

        public bool Recursive { get; set; }

        public KvFilter()
        {
            SourceKey = "Message";
            ValueSplit = "=:";
            FieldSplit = " ,";
            TrimValue = "";
            TrimKey = "";
        }

        public void PrepareConfiguration(IElasticsearchClient client)
        {
            var valueRxString = "(?:\"([^\"]+)\"" +
                         "|'([^']+)'" +
                         "|\\(([^\\)]+)\\)" +
                         "|\\[([^\\]]+)\\]" +
                         "|([^" + FieldSplit + "]+))";
            _kvRegex = new Regex(
                string.Format("([^{0}{1}]+)\\s*[{1}]\\s*{2}", FieldSplit, ValueSplit, valueRxString)
                , RegexOptions.Compiled | RegexOptions.Multiline);
        }

        public void PrepareEvent(Dictionary<string, object> logEvent)
        {
            string input;
            if (!logEvent.TryGetStringValue(SourceKey, out input))
            {
                //logEvent.AddTag(FailedKv);
                return;
            }

            ScanMessage(logEvent, input);
        }

        protected void ScanMessage(Dictionary<string, object> logEvent, string input)
        {
            foreach (Match match in _kvRegex.Matches(input))
            {
                var groups = match.Groups.Cast<Group>().Where(g => g.Success).ToList();
                var key = groups[1].Value;
                var value = groups[2].Value;

                if (!string.IsNullOrEmpty(_trimKey))
                {
                    key = key.Trim(_trimKey.ToCharArray());
                }
                if (!string.IsNullOrEmpty(_trimValue))
                {
                    value = value.Trim(_trimValue.ToCharArray());
                }

                ProcessValueAndStore(logEvent, key, value);
            }
        }

        private void ProcessValueAndStore(Dictionary<string, object> logEvent, string key, string value)
        {   
            if (Recursive)
            {
                var innerEvent = new Dictionary<string, object>();
                ScanMessage(innerEvent, value);

                if (innerEvent.Count > 0)
                {
                    logEvent.AddOrSet(key, innerEvent);
                    return;
                }
            }

            logEvent.AddOrSet(key, value);
        }
    }
}
