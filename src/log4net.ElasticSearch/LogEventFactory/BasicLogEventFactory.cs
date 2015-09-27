using System;
using System.Collections.Generic;
using log4net.Core;
using log4net.ElasticSearch.Extensions;

namespace log4net.ElasticSearch.LogEventFactory
{
    public class BasicLogEventFactory : ILogEventFactory
    {
        private static readonly string MachineName = Environment.MachineName;
        protected FixFlags FixedFields;

        public virtual void Configure(ElasticSearchAppender appenderProperties)
        {
            FixedFields = appenderProperties.FixedFields;
        }

        public virtual Dictionary<string, object> CreateLogEvent(LoggingEvent sourceLoggingEvent)
        {
            if (sourceLoggingEvent == null)
            {
                throw new ArgumentNullException("sourceLoggingEvent");
            }

            var resultDictionary = new Dictionary<string, object>();

            ParseBasicFields(sourceLoggingEvent, resultDictionary);

            ParseLocationInfo(sourceLoggingEvent.LocationInformation, resultDictionary);

            ParseMessage(sourceLoggingEvent, resultDictionary);

            ParseProperties(sourceLoggingEvent, resultDictionary);

            return resultDictionary;
        }

        protected void ParseBasicFields(LoggingEvent sourceLoggingEvent, Dictionary<string, object> logEvent)
        {
            logEvent["@timestamp"] = sourceLoggingEvent.TimeStamp.ToUniversalTime().ToString("O");
            logEvent["LoggerName"] = sourceLoggingEvent.LoggerName;
            logEvent["HostName"] = MachineName;

            if (FixedFields.ContainsFlag(FixFlags.ThreadName))
            {
                logEvent["ThreadName"] = sourceLoggingEvent.ThreadName;
            }

            if (FixedFields.ContainsFlag(FixFlags.Domain))
            {
                logEvent["AppDomain"] = sourceLoggingEvent.Domain;
            }

            if (sourceLoggingEvent.Level != null)
            {
                logEvent["Level"] = sourceLoggingEvent.Level.DisplayName;
            }

            if (FixedFields.ContainsFlag(FixFlags.Identity))
            {
                logEvent["Identity"] = sourceLoggingEvent.Identity;
            }

            if (FixedFields.ContainsFlag(FixFlags.UserName))
            {
                logEvent["UserName"] = sourceLoggingEvent.UserName;
            }
        }

        protected void ParseLocationInfo(LocationInfo locationInformation, Dictionary<string, object> resultDictionary)
        {
            if (FixedFields.ContainsFlag(FixFlags.LocationInfo) && locationInformation != null)
            {
                var locationInfo = new Dictionary<string, object>();
                resultDictionary["LocationInformation"] = locationInfo;

                locationInfo["ClassName"] = locationInformation.ClassName;
                locationInfo["FileName"] = locationInformation.FileName;
                locationInfo["LineNumber"] = locationInformation.LineNumber;
                locationInfo["FullInfo"] = locationInformation.FullInfo;
                locationInfo["MethodName"] = locationInformation.MethodName;
            }
        }

        protected void ParseMessage(LoggingEvent loggingEvent, Dictionary<string, object> logEvent)
        {
            if (FixedFields.ContainsFlag(FixFlags.Message) && loggingEvent.MessageObject != null)
            {
                logEvent["Message"] = loggingEvent.MessageObject.ToString();
                //logEvent["Message"] = sourceLoggingEvent.RenderedMessage;
            }

            if (FixedFields.ContainsFlag(FixFlags.Exception) && loggingEvent.ExceptionObject != null)
            {
                logEvent["Exception"] = loggingEvent.ExceptionObject.ToString();
            }
        }

        protected void ParseProperties(LoggingEvent sourceLoggingEvent, Dictionary<string, object> resultDictionary)
        {
            if (FixedFields.ContainsFlag(FixFlags.Properties))
            {
                var properties = sourceLoggingEvent.GetProperties();
                foreach (var propertyKey in properties.GetKeys())
                {
                    var value = properties[propertyKey];
                    resultDictionary[propertyKey] = value != null ? value.ToString() : string.Empty;
                }
            }
        }
    }
}