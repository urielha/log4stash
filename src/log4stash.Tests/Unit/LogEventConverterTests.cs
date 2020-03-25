using System;
using log4net.Core;
using log4stash.LogEvent;
using NUnit.Framework;

namespace log4stash.Tests.Unit
{
    [TestFixture]
    class LogEventConverterTests
    {
        private readonly ILogEventConverter _logEventConverter;

        public LogEventConverterTests()
        {
            _logEventConverter = new BasicLogEventConverter(FixFlags.All, true);
        }

        [Test]
        public void Log_message()
        {
            var loggingEvent = new LoggingEvent(
                this.GetType(),
                null,
                "logger.name",
                Level.Info,
                "message",
                null);

            var logEvent = _logEventConverter.ConvertLogEventToDictionary(loggingEvent);
            Assert.AreEqual(loggingEvent.RenderedMessage, logEvent["Message"]);
        }

        [Test]
        public void Log_exception()
        {
            var loggingEvent = new LoggingEvent(
                this.GetType(),
                null,
                "logger.name",
                Level.Info,
                "message",
                new Exception("Exception string"));

            var logEvent = _logEventConverter.ConvertLogEventToDictionary(loggingEvent);
            Assert.AreEqual(loggingEvent.RenderedMessage, logEvent["Message"]);
            Assert.AreEqual(loggingEvent.GetExceptionString(), logEvent["Exception"]);
            Assert.AreEqual(loggingEvent.ExceptionObject.Message, ((JsonSerializableException)logEvent["ExceptionObject"]).Message);
        }

        [Test]
        public void Log_exception_string_without_object()
        {
            var eventData = new LoggingEventData()
            {
                LoggerName = "logger",
                Message = "the message",
                ExceptionString = "Exception string",
            };
            var loggingEvent = new LoggingEvent(eventData);

            var logEvent = _logEventConverter.ConvertLogEventToDictionary(loggingEvent);
            Assert.AreEqual(loggingEvent.RenderedMessage, logEvent["Message"]);
            Assert.AreEqual(loggingEvent.GetExceptionString(), logEvent["Exception"]);
            Assert.IsFalse(logEvent.ContainsKey("ExceptionObject"));
        }
    }

}
