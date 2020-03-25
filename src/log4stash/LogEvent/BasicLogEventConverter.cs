using System;
using System.Collections.Generic;
using log4net.Core;

namespace log4stash.LogEvent
{
    public class BasicLogEventConverter : ILogEventConverter
    {
        private readonly ILoggingEventParser _eventParser;

        public BasicLogEventConverter(FixFlags fixedFields, bool serializeObjects)
            : this (new BasicLoggingEventParser(Environment.MachineName, fixedFields, serializeObjects))
        {
        }

        public BasicLogEventConverter(ILoggingEventParser eventParser)
        {
            _eventParser = eventParser;
        }

        public Dictionary<string, object> ConvertLogEventToDictionary(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                throw new ArgumentNullException("loggingEvent");
            }

            var resultDictionary = new Dictionary<string, object>();

            _eventParser.ParseBasicFields(loggingEvent, resultDictionary);

            _eventParser.ParseLocationInfo(loggingEvent, resultDictionary);

            _eventParser.ParseMessage(loggingEvent, resultDictionary);

            _eventParser.ParseException(loggingEvent, resultDictionary);

            _eventParser.ParseProperties(loggingEvent, resultDictionary);

            return resultDictionary;
        }

    }
}