using System;

namespace log4stash.LogEvent
{
    /// <summary>
    /// JsonSerializableException.
    /// Ported from jptoto's log4stash
    /// </summary>
    public class JsonSerializableException
    {
        public int HResult { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string HelpLink { get; set; }
        public string Source { get; set; }
        public string StackTrace { get; set; }
        public string TargetSite { get; set; }
        public System.Collections.IDictionary Data { get; set; }
        public JsonSerializableException InnerException { get; set; }
        
    }
}