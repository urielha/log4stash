using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using FluentAssertions;
using log4stash.SmartFormatters;
using NUnit.Framework;

namespace log4stash.UnitTests
{
    [TestFixture]
    public class LogEventSmartFormatterTests
    {
        [Test]
        [TestCaseSource(nameof(TimeZoneCases))]
        public void FORMATTER_SHOULD_USE_CORRECT_DATE_TIMEZONE_ACCORDING_TO_LEADING_SYMBOL(string format,
            string expected)
        {
            //Arrange
            var formatter = new LogEventSmartFormatter(format);
            //Act
            var result = formatter.Format();

            //Assert
            result.Should().Be(expected);
        }

        private const string DateFormat = "HH:mm";

        private static readonly string UtcHour = DateTime.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture);
        private static readonly string LocalHour = DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture);

        private static readonly TestCaseData[] TimeZoneCases =
        {
            new TestCaseData($"%{{+{DateFormat}}}", LocalHour),
            new TestCaseData($"%{{~{DateFormat}}}", UtcHour),
        };
    }
}
