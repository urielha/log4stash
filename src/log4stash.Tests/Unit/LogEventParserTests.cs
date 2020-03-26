using System;
using System.Collections.Generic;
using FluentAssertions;
using log4net.Core;
using log4stash.LogEvent;
using NSubstitute;
using NUnit.Framework;

namespace log4stash.Tests.Unit
{
    [TestFixture]
    class LogEventParserTests
    {
        private readonly IJsonSerializableExceptionFactory _exceptionFactory;

        public LogEventParserTests()
        {
            _exceptionFactory = Substitute.For<IJsonSerializableExceptionFactory>();
        }

        [Test]
        public void PARSER_SHOULD_ADD_TIMESTAMP_IN_UNIVERSAL_FORMAT()
        {
            //Arrange
            var universalDate = DateTime.UtcNow;
            var localTime = universalDate.ToLocalTime();
#pragma warning disable 618
            var loggingEvent = new LoggingEvent(new LoggingEventData {TimeStamp = localTime});
#pragma warning restore 618
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.None, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseBasicFields(loggingEvent, resultDictionary);

            //Assert
            resultDictionary["@timestamp"].Should().BeEquivalentTo(universalDate.ToString("O"));
        }

        [Test]
        public void PARSER_SHOULD_ADD_LOGGER_NAME()
        {
            //Arrange
            const string loggerName = "logger";
            var loggingEvent = new LoggingEvent(new LoggingEventData {LoggerName = loggerName});
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.None, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseBasicFields(loggingEvent, resultDictionary);

            //Assert
            resultDictionary["LoggerName"].Should().BeEquivalentTo(loggerName);
        }

        [Test]
        public void PARSER_SHOULD_ADD_HOST_NAME()
        {
            //Arrange
            const string hostname = "machine";
            var loggingEvent = new LoggingEvent(new LoggingEventData());
            var parser = new BasicLoggingEventParser(hostname, FixFlags.None, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseBasicFields(loggingEvent, resultDictionary);

            //Assert
            resultDictionary["HostName"].Should().BeEquivalentTo(hostname);
        }

        [Test]
        public void PARSER_SHOULD_ADD_THREAD_NAME_IF_REQUIRED()
        {
            //Arrange
            const string threadName = "thread";
            var loggingEvent = new LoggingEvent(new LoggingEventData {ThreadName = threadName});
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.ThreadName, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseBasicFields(loggingEvent, resultDictionary);

            //Assert
            resultDictionary["ThreadName"].Should().BeEquivalentTo(threadName);
        }

        [Test]
        public void PARSER_SHOULD_NOT_ADD_THREAD_NAME_IF_NOT_REQUIRED()
        {
            //Arrange
            const string threadName = "thread";
            var loggingEvent = new LoggingEvent(new LoggingEventData {ThreadName = threadName});
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.None, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseBasicFields(loggingEvent, resultDictionary);

            //Assert
            resultDictionary.ContainsKey("ThreadName").Should().BeFalse();
        }

        [Test]
        public void PARSER_SHOULD_ADD_DOMAIN_IF_REQUIRED()
        {
            //Arrange
            const string domain = "domain";
            var loggingEvent = new LoggingEvent(new LoggingEventData {Domain = domain});
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.Domain, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseBasicFields(loggingEvent, resultDictionary);

            //Assert
            resultDictionary["AppDomain"].Should().BeEquivalentTo(domain);
        }

        [Test]
        public void PARSER_SHOULD_NOT_ADD_DOMAIN_IF_NOT_REQUIRED()
        {
            //Arrange
            const string domain = "domain";
            var loggingEvent = new LoggingEvent(new LoggingEventData {Domain = domain});
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.None, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseBasicFields(loggingEvent, resultDictionary);

            //Assert
            resultDictionary.ContainsKey("AppDomain").Should().BeFalse();
        }

        [Test]
        public void PARSER_SHOULD_ADD_IDENTITY_IF_REQUIRED()
        {
            //Arrange
            const string identity = "identity";
            var loggingEvent = new LoggingEvent(new LoggingEventData { Identity = identity });
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.Identity, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseBasicFields(loggingEvent, resultDictionary);

            //Assert
            resultDictionary["Identity"].Should().BeEquivalentTo(identity);
        }

        [Test]
        public void PARSER_SHOULD_NOT_ADD_IDENTITY_IF_NOT_REQUIRED()
        {
            //Arrange
            const string identity = "identity";
            var loggingEvent = new LoggingEvent(new LoggingEventData { Identity = identity});
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.None, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseBasicFields(loggingEvent, resultDictionary);

            //Assert
            resultDictionary.ContainsKey("Identity").Should().BeFalse();
        }

        [Test]
        public void PARSER_SHOULD_ADD_USERNAME_IF_REQUIRED()
        {
            //Arrange
            const string username = "user";
            var loggingEvent = new LoggingEvent(new LoggingEventData { UserName = username });
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.UserName, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseBasicFields(loggingEvent, resultDictionary);

            //Assert
            resultDictionary["UserName"].Should().BeEquivalentTo(username);
        }

        [Test]
        public void PARSER_SHOULD_NOT_ADD_USERNAME_IF_NOT_REQUIRED()
        {
            //Arrange
            const string username = "user";
            var loggingEvent = new LoggingEvent(new LoggingEventData { UserName = username });
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.None, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseBasicFields(loggingEvent, resultDictionary);

            //Assert
            resultDictionary.ContainsKey("UserName").Should().BeFalse();
        }

        [Test]
        public void PARSER_SHOULD_ADD_LEVEL_WHEN_EXISTS()
        {
            //Arrange
            var level = Level.Emergency;
            var loggingEvent = new LoggingEvent(new LoggingEventData {Level = level});
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.None, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseBasicFields(loggingEvent, resultDictionary);

            //Assert
            resultDictionary["Level"].Should().BeEquivalentTo(level.DisplayName);
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
            var parser = new BasicLoggingEventParser("machine", FixFlags.Partial, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();
            parser.ParseMessage(loggingEvent, resultDictionary);
            Assert.AreEqual(loggingEvent.RenderedMessage, resultDictionary["Message"]);
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

            var parser = new BasicLoggingEventParser("machine", FixFlags.Partial, true);
            var resultDictionary = new Dictionary<string, object>();
            parser.ParseMessage(loggingEvent, resultDictionary);
            parser.ParseException(loggingEvent, resultDictionary);

            Assert.AreEqual(loggingEvent.RenderedMessage, resultDictionary["Message"]);
            Assert.AreEqual(loggingEvent.GetExceptionString(), resultDictionary["Exception"]);
            Assert.AreEqual(loggingEvent.ExceptionObject.Message,
                ((JsonSerializableException) resultDictionary["ExceptionObject"]).Message);
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

            var parser = new BasicLoggingEventParser("machine", FixFlags.Partial, true);
            var resultDictionary = new Dictionary<string, object>();
            parser.ParseMessage(loggingEvent, resultDictionary);
            parser.ParseException(loggingEvent, resultDictionary);

            Assert.AreEqual(loggingEvent.RenderedMessage, resultDictionary["Message"]);
            Assert.AreEqual(loggingEvent.GetExceptionString(), resultDictionary["Exception"]);
            Assert.IsFalse(resultDictionary.ContainsKey("ExceptionObject"));
        }
    }
}