using System.Collections.Generic;
using log4net.Core;

namespace log4stash.LogEvent
{
    public interface ILoggingEventParser
    {
        void ParseBasicFields(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary);
        void ParseLocationInfo(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary);
        void ParseMessage(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary);
        void ParseException(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary);
        void ParseProperties(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary);
    }
}