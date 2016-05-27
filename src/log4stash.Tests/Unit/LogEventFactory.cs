using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Core;
using log4net.ElasticSearch.LogEventFactory;
using log4net.Repository.Hierarchy;
using NUnit.Framework;

namespace log4net.ElasticSearch.Tests.Unit
{
    [TestFixture]
    class LogEventFactory
    {
        private readonly ILogEventFactory _logEventFactory = new BasicLogEventFactory();

        public LogEventFactory()
        {
            _logEventFactory.Configure(new LogEventFactoryParams(FixFlags.All, true));
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

            var logEvent = _logEventFactory.CreateLogEvent(loggingEvent);
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

            var logEvent = _logEventFactory.CreateLogEvent(loggingEvent);
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

            var logEvent = _logEventFactory.CreateLogEvent(loggingEvent);
            Assert.AreEqual(loggingEvent.RenderedMessage, logEvent["Message"]);
            Assert.AreEqual(loggingEvent.GetExceptionString(), logEvent["Exception"]);
            Assert.IsFalse(logEvent.ContainsKey("ExceptionObject"));
        }
    }

    internal class LogEventFactoryParams : ILogEventFactoryParams
    {
        public FixFlags FixedFields { get; set; }
        public bool SerializeObjects { get; set; }

        public LogEventFactoryParams(FixFlags fixedFields, bool serializeObjects)
        {
            FixedFields = fixedFields;
            SerializeObjects = serializeObjects;
        }
    }
}
