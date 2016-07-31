using System;
using System.Collections.Generic;
using System.Linq;
using log4net.Core;
using log4net.Util;
using log4stash.Extensions;

namespace log4stash.LogEventFactory
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

        public virtual Dictionary<string, object> CreateLogEvent(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
            {
                throw new ArgumentNullException("loggingEvent");
            }

            var resultDictionary = new Dictionary<string, object>();

            ParseBasicFields(loggingEvent, resultDictionary);

            ParseLocationInfo(loggingEvent.LocationInformation, resultDictionary);

            ParseMessage(loggingEvent, resultDictionary);

            ParseException(loggingEvent, resultDictionary);

            ParseProperties(loggingEvent, resultDictionary);

            return resultDictionary;
        }

        protected void ParseBasicFields(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary)
        {
            resultDictionary["@timestamp"] = loggingEvent.TimeStamp.ToUniversalTime().ToString("O");
            resultDictionary["LoggerName"] = loggingEvent.LoggerName;
            resultDictionary["HostName"] = MachineName;

            if (FixedFields.ContainsFlag(FixFlags.ThreadName))
            {
                resultDictionary["ThreadName"] = loggingEvent.ThreadName;
            }

            if (FixedFields.ContainsFlag(FixFlags.Domain))
            {
                resultDictionary["AppDomain"] = loggingEvent.Domain;
            }

            if (loggingEvent.Level != null)
            {
                resultDictionary["Level"] = loggingEvent.Level.DisplayName;
            }

            if (FixedFields.ContainsFlag(FixFlags.Identity))
            {
                resultDictionary["Identity"] = loggingEvent.Identity;
            }

            if (FixedFields.ContainsFlag(FixFlags.UserName))
            {
                resultDictionary["UserName"] = loggingEvent.UserName;
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

        protected void ParseMessage(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary)
        {
            if (FixedFields.ContainsFlag(FixFlags.Message))
            {
                resultDictionary["Message"] = loggingEvent.RenderedMessage;

                // Added special handling of the MessageObject since it may be an exception. 
                // Exception Types require specialized serialization to prevent serialization exceptions.
                if (SerializeObjects && loggingEvent.MessageObject != null && !(loggingEvent.MessageObject is string))
                {
                    var exceptionObject = loggingEvent.MessageObject as Exception;
                    if (exceptionObject != null)
                    {
                        resultDictionary["MessageObject"] = JsonSerializableException.Create(exceptionObject);
                    }
                    else
                    {
                        resultDictionary["MessageObject"] = loggingEvent.MessageObject;
                    }
                }
            }
        }

        protected void ParseException(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary)
        {
            if (FixedFields.ContainsFlag(FixFlags.Exception))
            {
                var exception = loggingEvent.ExceptionObject;
                var exceptionString = loggingEvent.GetExceptionString();

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

        protected void ParseProperties(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary)
        {
            if (FixedFields.ContainsFlag(FixFlags.Properties))
            {
                var properties = loggingEvent.GetProperties();
                foreach (var propertyKey in properties.GetKeys())
                {
                    object value = properties[propertyKey];
                    resultDictionary[propertyKey] = value ?? string.Empty;
                }
            }
        }
    }
}