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
        protected bool SerializeObjects;

        public virtual void Configure(ILogEventFactoryParams appenderProperties)
        {
            FixedFields = appenderProperties.FixedFields;
            SerializeObjects = appenderProperties.SerializeObjects;
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

            ParseException(sourceLoggingEvent, resultDictionary);

            ParseProperties(sourceLoggingEvent, resultDictionary);

            return resultDictionary;
        }

        protected void ParseBasicFields(LoggingEvent sourceLoggingEvent, Dictionary<string, object> resultDictionary)
        {
            resultDictionary["@timestamp"] = sourceLoggingEvent.TimeStamp.ToUniversalTime().ToString("O");
            resultDictionary["LoggerName"] = sourceLoggingEvent.LoggerName;
            resultDictionary["HostName"] = MachineName;

            if (FixedFields.ContainsFlag(FixFlags.ThreadName))
            {
                resultDictionary["ThreadName"] = sourceLoggingEvent.ThreadName;
            }

            if (FixedFields.ContainsFlag(FixFlags.Domain))
            {
                resultDictionary["AppDomain"] = sourceLoggingEvent.Domain;
            }

            if (sourceLoggingEvent.Level != null)
            {
                resultDictionary["Level"] = sourceLoggingEvent.Level.DisplayName;
            }

            if (FixedFields.ContainsFlag(FixFlags.Identity))
            {
                resultDictionary["Identity"] = sourceLoggingEvent.Identity;
            }

            if (FixedFields.ContainsFlag(FixFlags.UserName))
            {
                resultDictionary["UserName"] = sourceLoggingEvent.UserName;
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

        protected void ParseMessage(LoggingEvent sourceLoggingEvent, Dictionary<string, object> resultDictionary)
        {
            if (FixedFields.ContainsFlag(FixFlags.Message))
            {
                resultDictionary["Message"] = sourceLoggingEvent.RenderedMessage;

                // Added special handling of the MessageObject since it may be an exception. 
                // Exception Types require specialized serialization to prevent serialization exceptions.
                if (SerializeObjects && sourceLoggingEvent.MessageObject != null && !(sourceLoggingEvent.MessageObject is string))
                {
                    var exceptionObject = sourceLoggingEvent.MessageObject as Exception;
                    if (exceptionObject != null)
                    {
                        resultDictionary["MessageObject"] = JsonSerializableException.Create(exceptionObject);
                    }
                    else
                    {
                        resultDictionary["MessageObject"] = sourceLoggingEvent.MessageObject;
                    }
                }
            }
        }

        protected void ParseException(LoggingEvent sourceLoggingEvent, Dictionary<string, object> resultDictionary)
        {
            if (FixedFields.ContainsFlag(FixFlags.Exception))
            {
                var exception = sourceLoggingEvent.ExceptionObject;
                var exceptionString = sourceLoggingEvent.GetExceptionString();

                // If exceptionString is empty - no exception exists at all.
                // Because GetExceptionString() returns exceptionString if exists or exception.ToString().
                if (!string.IsNullOrEmpty(exceptionString))
                {
                    resultDictionary["Exception"] = exceptionString;
                    
                    if (SerializeObjects && exception != null)
                    {
                        resultDictionary["ExceptionObject"] = JsonSerializableException.Create(exception);
                    }
                }
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
                    resultDictionary[propertyKey] = value ?? string.Empty;
                }
            }
        }
    }
}