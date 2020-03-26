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
        public void PARSER_SHOULD_NOT_ADD_ANY_LOCATION_INFO_WHEN_NOT_REQUIRED()
        {
            //Arrange
            var loggingEvent = new LoggingEvent(new LoggingEventData {LocationInfo = new LocationInfo(GetType())});
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.None, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseLocationInfo(loggingEvent, resultDictionary);

            //Assert
            resultDictionary.ContainsKey("LocationInformation").Should().BeFalse();
        }

        [Test]
        public void PARSER_SHOULD_NOT_ADD_ANY_LOCATION_INFO_WHEN_IS_NULL()
        {
            //Arrange
            var loggingEvent = new LoggingEvent(new LoggingEventData { LocationInfo = null });
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.None, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseLocationInfo(loggingEvent, resultDictionary);

            //Assert
            resultDictionary.ContainsKey("LocationInformation").Should().BeFalse();
        }

        [Test]
        public void PARSER_SHOULD_ADD_CLASS_NAME_WHEN_LOCATION_INFO_IS_REQUIRED()
        {
            //Arrange
            var locationInfo = new LocationInfo("class", "method", "file", "line");
            var loggingEvent = new LoggingEvent(new LoggingEventData { LocationInfo = locationInfo });
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.LocationInfo, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseLocationInfo(loggingEvent, resultDictionary);

            //Assert
            var resultLocationInfo = resultDictionary["LocationInformation"] as Dictionary<string, object>;
            if (resultLocationInfo != null)
                resultLocationInfo["ClassName"].Should().BeEquivalentTo(locationInfo.ClassName);
        }

        [Test]
        public void PARSER_SHOULD_ADD_FILE_NAME_WHEN_LOCATION_INFO_IS_REQUIRED()
        {
            //Arrange
            var locationInfo = new LocationInfo("class", "method", "file", "line");
            var loggingEvent = new LoggingEvent(new LoggingEventData { LocationInfo = locationInfo });
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.LocationInfo, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseLocationInfo(loggingEvent, resultDictionary);

            //Assert
            var resultLocationInfo = resultDictionary["LocationInformation"] as Dictionary<string, object>;
            if (resultLocationInfo != null)
                resultLocationInfo["FileName"].Should().BeEquivalentTo(locationInfo.FileName);
        }

        [Test]
        public void PARSER_SHOULD_ADD_LINE_NUMBER_WHEN_LOCATION_INFO_IS_REQUIRED()
        {
            //Arrange
            var locationInfo = new LocationInfo("class", "method", "file", "line");
            var loggingEvent = new LoggingEvent(new LoggingEventData { LocationInfo = locationInfo });
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.LocationInfo, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseLocationInfo(loggingEvent, resultDictionary);

            //Assert
            var resultLocationInfo = resultDictionary["LocationInformation"] as Dictionary<string, object>;
            if (resultLocationInfo != null)
                resultLocationInfo["LineNumber"].Should().BeEquivalentTo(locationInfo.LineNumber);
        }

        [Test]
        public void PARSER_SHOULD_ADD_FULL_INFO_WHEN_LOCATION_INFO_IS_REQUIRED()
        {
            //Arrange
            var locationInfo = new LocationInfo("class", "method", "file", "line");
            var loggingEvent = new LoggingEvent(new LoggingEventData { LocationInfo = locationInfo });
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.LocationInfo, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseLocationInfo(loggingEvent, resultDictionary);

            //Assert
            var resultLocationInfo = resultDictionary["LocationInformation"] as Dictionary<string, object>;
            if (resultLocationInfo != null)
                resultLocationInfo["FullInfo"].Should().BeEquivalentTo(locationInfo.FullInfo);
        }

        [Test]
        public void PARSER_SHOULD_ADD_METHOD_NAME_WHEN_LOCATION_INFO_IS_REQUIRED()
        {
            //Arrange
            var locationInfo = new LocationInfo("class", "method", "file", "line");
            var loggingEvent = new LoggingEvent(new LoggingEventData { LocationInfo = locationInfo });
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.LocationInfo, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseLocationInfo(loggingEvent, resultDictionary);

            //Assert
            var resultLocationInfo = resultDictionary["LocationInformation"] as Dictionary<string, object>;
            if (resultLocationInfo != null)
                resultLocationInfo["MethodName"].Should().BeEquivalentTo(locationInfo.MethodName);
        }

        [Test]
        public void PARSER_SHOULD_NOT_ADD_MESSAGE_FIELDS_WHEN_NOT_REQUIRED()
        {
            //Arrange
            var loggingEvent = new LoggingEvent(new LoggingEventData {Message = "hello"});
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.None, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseMessage(loggingEvent, resultDictionary);

            //Assert
            resultDictionary.ContainsKey("Message").Should().BeFalse();
            resultDictionary.ContainsKey("MessageObject").Should().BeFalse();
        }

        [Test]
        public void PARSER_SHOULD_ADD_RENDERED_MESSAGE_WHEN_MESSAGE_IS_REQUIRED()
        {
            //Arrange
            var loggingEvent = new LoggingEvent(new LoggingEventData { Message = "hello" });
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.Message, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseMessage(loggingEvent, resultDictionary);

            //Assert
            resultDictionary["Message"].Should().BeEquivalentTo(loggingEvent.RenderedMessage);
        }

        [Test]
        public void PARSER_SHOULD_NOT_ADD_MESSAGE_OBJECT_WHEN_REQUIRED_AND_SERIALIZE_OBJECTS_IS_DISABLED()
        {
            //Arrange
            var loggingEvent = new LoggingEvent(new LoggingEventData { Message = "hello" });
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.Message, false, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseMessage(loggingEvent, resultDictionary);

            //Assert
            resultDictionary.ContainsKey("MessageObject").Should().BeFalse();
        }

        [Test]
        public void PARSER_SHOULD_NOT_ADD_MESSAGE_OBJECT_WHEN_REQUIRED_AND_MESSAGE_OBJECT_IS_STRING()
        {
            //Arrange
            var messageObject = "hello";
            var loggingEvent = new LoggingEvent(GetType(), null, null, Level.Info, messageObject, null);
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.Message, false, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseMessage(loggingEvent, resultDictionary);

            //Assert
            resultDictionary.ContainsKey("MessageObject").Should().BeFalse();
        }

        [Test]
        public void PARSER_SHOULD_ADD_MESSAGE_OBJECT_WHEN_REQUIRED_AND_SERIALIZE_OBJECTS_IS_ENABLED_AND_IS_NOT_EXCEPTION()
        {
            //Arrange
            var messageObject = new object();
            var loggingEvent = new LoggingEvent(GetType(), null, null, Level.Info, messageObject, null);
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.Message, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseMessage(loggingEvent, resultDictionary);

            //Assert
            resultDictionary["MessageObject"].Should().Be(messageObject);
        }

        [Test]
        public void PARSER_SHOULD_ADD_EXCEPTION_AS_MESSAGE_OBJECT_WHEN_REQUIRED_AND_SERIALIZE_OBJECTS_IS_ENABLED()
        {
            //Arrange
            var exception = new Exception();
            var jsonException = new JsonSerializableException();
            _exceptionFactory.Create(exception).Returns(jsonException);
            var loggingEvent = new LoggingEvent(GetType(), null, null, Level.Info, exception, null);
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.Message, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseMessage(loggingEvent, resultDictionary);

            //Assert
            resultDictionary["MessageObject"].Should().Be(jsonException);
        }

        [Test]
        public void PARSER_SHOULD_NOT_ADD_EXCEPTION_WHEN_NOT_REQUIRED()
        {
            //Arrange
            var exception = new Exception();
            var loggingEvent = new LoggingEvent(GetType(), null, null, Level.Info, null, exception);
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.None, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();
            //Act
            parser.ParseException(loggingEvent, resultDictionary);

            //Assert
            resultDictionary.ContainsKey("Exception").Should().BeFalse();
            resultDictionary.ContainsKey("ExceptionObject").Should().BeFalse();
        }

        [Test]
        public void PARSER_SHOULD_NOT_ADD_EXCEPTION_WHEN_REQUIRED_AND_EXCEPTION_IS_NULL()
        {
            //Arrange
            var loggingEvent = new LoggingEvent(GetType(), null, null, Level.Info, null, null);
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.None, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseException(loggingEvent, resultDictionary);

            //Assert
            resultDictionary.ContainsKey("Exception").Should().BeFalse();
            resultDictionary.ContainsKey("ExceptionObject").Should().BeFalse();
        }

        [Test]
        public void PARSER_SHOULD_ADD_EXCEPTION_STRING_WHEN_EXCEPTION_IS_REQUIRED()
        {
            //Arrange
            var exception = new Exception();
            var loggingEvent = new LoggingEvent(GetType(), null, null, Level.Info, null, exception);
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.Exception, false, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseException(loggingEvent, resultDictionary);

            //Assert
            resultDictionary["Exception"].Should().BeEquivalentTo(loggingEvent.GetExceptionString());
        }

        [Test]
        public void PARSER_SHOULD_ADD_EXCEPTION_OBJECT_WHEN_EXCEPTION_IS_REQUIRED_AND_SERIALIZE_OBJECTS_IS_ENABLED()
        {
            //Arrange
            var exception = new Exception();
            var loggingEvent = new LoggingEvent(GetType(), null, null, Level.Info, null, exception);
            var jsonException = new JsonSerializableException();
            _exceptionFactory.Create(exception).Returns(jsonException);
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.Exception, true, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseException(loggingEvent, resultDictionary);

            //Assert
            resultDictionary["ExceptionObject"].Should().Be(jsonException);
        }

        [Test]
        public void PARSER_SHOULD_NOT_ADD_PROPERTIES_WHEN_NOT_REQUIRED()
        {
            //Arrange
            var loggingEvent = new LoggingEvent(new LoggingEventData());
            loggingEvent.Properties["prop"] = "prop";
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.None, false, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseProperties(loggingEvent, resultDictionary);

            //Assert
            resultDictionary.ContainsKey("prop").Should().BeFalse();
        }

        [Test]
        [TestCase(1)]
        [TestCase(10)]
        public void PARSER_SHOULD_ADD_ALL_PROPERTIES_WHEN_REQUIRED(int numOfProperties)
        {
            //Arrange
            var loggingEvent = new LoggingEvent(new LoggingEventData());
            for (var i = 0; i < numOfProperties; i++)
            {
                loggingEvent.Properties["property" + i] = i;
            }
            var parser = new BasicLoggingEventParser(string.Empty, FixFlags.Properties, false, _exceptionFactory);
            var resultDictionary = new Dictionary<string, object>();

            //Act
            parser.ParseProperties(loggingEvent, resultDictionary);

            //Assert
            var loggingEventProperties = loggingEvent.Properties;
            foreach (var key in loggingEventProperties.GetKeys())
            {
                resultDictionary[key].Should().Be(loggingEventProperties[key]);
            }
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
    }
}