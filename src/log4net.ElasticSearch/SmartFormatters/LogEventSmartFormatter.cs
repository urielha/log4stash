using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace log4net.ElasticSearch.SmartFormatters
{
    /// <summary>
    /// A SmartFormatter that replace all the keys in the input using the LogEvent.
    /// Key might look like this "sometext {key}".
    /// It also formats keys that start with "+" as time.
    /// For example: "the day is {+yyyy-MM-dd}"
    /// </summary>
    public class LogEventSmartFormatter : SmartFormatter
    {
        private static readonly Regex InnerRegex = new Regex(@"%\{([^\}]+)\}", RegexOptions.Compiled);
        

        public LogEventSmartFormatter(string input) 
            : base(input, InnerRegex.Matches(input))
        {

        }

        protected override bool TryProcessMatch(Dictionary<string, object> logEvent, Match match, out string replacementString)
        {
            replacementString = string.Empty;
            string innerMatch = match.Groups[1].Value;

            // "+" means dateTime format
            if (innerMatch.StartsWith("+"))
            {
                replacementString = DateTime.Now.ToString(innerMatch.Substring(1), CultureInfo.InvariantCulture);
                return true;
            }

            object token;
            if (logEvent.TryGetValue(innerMatch, out token))
            {
                replacementString = token.ToString();
                return true;
            }

            return false;
        }

        public static implicit operator LogEventSmartFormatter(string s)
        {
            return new LogEventSmartFormatter(s);
        }
    }
}