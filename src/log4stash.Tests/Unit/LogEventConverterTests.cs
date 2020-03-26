using System;
using System.Collections.Generic;
using FluentAssertions;
using log4net.Core;
using log4stash.LogEvent;
using NSubstitute;
using NUnit.Framework;

namespace log4stash.Tests.Unit
{
    class LogEventConverterTests
    {
        private ILoggingEventParser _logParser;

        [SetUp]
        public void Setup()
        {
            _logParser = Substitute.For<ILoggingEventParser>();
        }

        [Test]
        public void CONVERTER_SHOULD_THROW_ARGUMENT_NULL_EXCEPTION_WHEN_EVENT_IS_NULL()
        {
            //Arrange
            var converter = new BasicLogEventConverter(_logParser);

            //Act
            try
            {
                converter.ConvertLogEventToDictionary(null);
            }

            //Assert
            catch (Exception e)
            {
                e.Should().BeOfType<ArgumentNullException>();
            }
        }

        [Test]
        public void CONVERTER_SHOULD_USE_ALL_METHODS_FROM_PARSER()
        {
            //Arrange
            var converter = new BasicLogEventConverter(_logParser);
            var log = new LoggingEvent(new LoggingEventData());

            //Act
            converter.ConvertLogEventToDictionary(log);

            //Assert
            _logParser.Received().ParseBasicFields(log, Arg.Any<Dictionary<string, object>>());
            _logParser.Received().ParseLocationInfo(log, Arg.Any<Dictionary<string, object>>());
            _logParser.Received().ParseMessage(log, Arg.Any<Dictionary<string, object>>());
            _logParser.Received().ParseException(log, Arg.Any<Dictionary<string, object>>());
            _logParser.Received().ParseProperties(log, Arg.Any<Dictionary<string, object>>());
        }
    }
}
