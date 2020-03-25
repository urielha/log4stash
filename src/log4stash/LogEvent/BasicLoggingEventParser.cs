using System;
using System.Collections.Generic;
using log4net.Core;
using log4stash.Extensions;

namespace log4stash.LogEvent
{
    public class BasicLoggingEventParser : ILoggingEventParser
    {
        private readonly string _machineName;
        private readonly FixFlags _fixedFields;
        private readonly bool _serializeObjects;

        public BasicLoggingEventParser(string machineName, FixFlags fixedFields, bool serializeObjects)
        {
            _machineName = machineName;
            _fixedFields = fixedFields;
            _serializeObjects = serializeObjects;
        }

        public void ParseBasicFields(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary)
        {
            resultDictionary["@timestamp"] = loggingEvent.TimeStamp.ToUniversalTime().ToString("O");
            resultDictionary["LoggerName"] = loggingEvent.LoggerName;
            resultDictionary["HostName"] = _machineName;

            if (_fixedFields.ContainsFlag(FixFlags.ThreadName))
            {
                resultDictionary["ThreadName"] = loggingEvent.ThreadName;
            }

            if (_fixedFields.ContainsFlag(FixFlags.Domain))
            {
                resultDictionary["AppDomain"] = loggingEvent.Domain;
            }

            if (loggingEvent.Level != null)
            {
                resultDictionary["Level"] = loggingEvent.Level.DisplayName;
            }

            if (_fixedFields.ContainsFlag(FixFlags.Identity))
            {
                resultDictionary["Identity"] = loggingEvent.Identity;
            }

            if (_fixedFields.ContainsFlag(FixFlags.UserName))
            {
                resultDictionary["UserName"] = loggingEvent.UserName;
            }
        }

        public void ParseLocationInfo(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary)
        {
            var locationInformation = loggingEvent.LocationInformation;
            if (_fixedFields.ContainsFlag(FixFlags.LocationInfo) && locationInformation != null)
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

        public void ParseMessage(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary)
        {
            if (_fixedFields.ContainsFlag(FixFlags.Message))
            {
                resultDictionary["Message"] = loggingEvent.RenderedMessage;

                // Added special handling of the MessageObject since it may be an exception. 
                // Exception Types require specialized serialization to prevent serialization exceptions.
                if (_serializeObjects && loggingEvent.MessageObject != null && !(loggingEvent.MessageObject is string))
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

        public void ParseException(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary)
        {
            if (!_fixedFields.ContainsFlag(FixFlags.Exception)) return;
            var exception = loggingEvent.ExceptionObject;
            var exceptionString = loggingEvent.GetExceptionString();

            // If exceptionString is empty - no exception exists at all.
            // Because GetExceptionString() returns exceptionString if exists or exception.ToString().
            if (string.IsNullOrEmpty(exceptionString)) return;
            resultDictionary["Exception"] = exceptionString;

            if (_serializeObjects && exception != null)
            {
                resultDictionary["ExceptionObject"] = JsonSerializableException.Create(exception);
            }
        }

        public void ParseProperties(LoggingEvent loggingEvent, Dictionary<string, object> resultDictionary)
        {
            if (!_fixedFields.ContainsFlag(FixFlags.Properties)) return;
            var properties = loggingEvent.GetProperties();
            foreach (var propertyKey in properties.GetKeys())
            {
                var value = properties[propertyKey];
                resultDictionary[propertyKey] = value ?? string.Empty;
            }
        }
    }
}
