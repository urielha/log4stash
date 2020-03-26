using System.Collections.Generic;
using FluentAssertions;
using log4stash.Filters;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace log4stash.Tests.Unit
{
    class FiltersTests
    {
        [Test]
        public void ADD_VALUE_FILTER_SHOULD_ADD_VALUE_TO_EMPTY_EVENT()
        {
            //Arrange
            const string key = "key";
            const string value = "value";
            var filter = new AddValueFilter {Key = key, Value = value, Overwrite = true};
            var eventProperties = new Dictionary<string, object>();

            //Act
            filter.PrepareEvent(eventProperties);

            //Assert
            eventProperties[key].Should().Be(value);
        }

        [Test]
        public void ADD_VALUE_FILTER_SHOULD_NOT_OVERRIDE_VALUE_WHEN_NOT_ALLOWED()
        {
            //Arrange
            const string key = "key";
            const string value = "value";
            const string value2 = "value2";
            var filter = new AddValueFilter {Key = key, Value = value, Overwrite = false};
            var eventProperties = new Dictionary<string, object> {{key, value2}};

            //Act
            filter.PrepareEvent(eventProperties);

            //Assert
            ((IEnumerable<object>) eventProperties[key]).Should().BeEquivalentTo(new object[] {value2, value});
        }

        [Test]
        public void ADD_VALUE_FILTER_SHOULD_OVERRIDE_VALUE_WHEN_ALLOWED()
        {
            //Arrange
            const string key = "key";
            const string value = "value";
            const string value2 = "value2";
            var filter = new AddValueFilter {Key = key, Value = value, Overwrite = true};
            var eventProperties = new Dictionary<string, object> {{key, value2}};

            //Act
            filter.PrepareEvent(eventProperties);

            //Assert
            eventProperties[key].Should().Be(value);
        }

        [Test]
        public void CONVERT_FILTER_SHOULD_USE_TO_STRING_ON_CORRECT_KEY()
        {
            //Arrange
            var filter = new ConvertFilter();
            const string key = "key";
            const string stringValue = "hello";
            filter.AddToString(key);
            var obj = new MockEventObject(stringValue);
            var eventProperties = new Dictionary<string, object> {{key, obj}};

            //Act
            filter.PrepareEvent(eventProperties);

            //Assert
            eventProperties[key].Should().Be(stringValue);
        }

        [Test]
        public void CONVERT_FILTER_SHOULD_USE_TO_LOWER_ON_CORRECT_KEY()
        {
            //Arrange
            var filter = new ConvertFilter();
            const string key = "key";
            const string value = "HELLO";
            filter.AddToLower(key);
            var eventProperties = new Dictionary<string, object> {{key, value}};

            //Act
            filter.PrepareEvent(eventProperties);

            //value
            eventProperties[key].Should().Be("hello");
        }

        [Test]
        public void CONVERT_FILTER_SHOULD_USE_TO_UPPER_ON_CORRECT_KEY()
        {
            //Arrange
            var filter = new ConvertFilter();
            const string key = "key";
            const string value = "hello";
            filter.AddToUpper(key);
            var eventProperties = new Dictionary<string, object> {{key, value}};

            //Act
            filter.PrepareEvent(eventProperties);

            //value
            eventProperties[key].Should().Be("HELLO");
        }

        [Test]
        public void CONVERT_FILTER_SHOULD_USE_TO_INT_ON_CORRECT_KEY()
        {
            //Arrange
            var filter = new ConvertFilter();
            const string key = "key";
            const string value = "111";
            filter.AddToInt(key);
            var eventProperties = new Dictionary<string, object> {{key, value}};

            //Act
            filter.PrepareEvent(eventProperties);

            //value
            eventProperties[key].Should().Be(111);
        }

        [Test]
        public void CONVERT_FILTER_SHOULD_RETURN_VALUE_ITSELF_WHEN_VALUE_IS_INT()
        {
            //Arrange
            var filter = new ConvertFilter();
            const string key = "key";
            const int value = 111;
            filter.AddToInt(key);
            var eventProperties = new Dictionary<string, object> {{key, value}};

            //Act
            filter.PrepareEvent(eventProperties);

            //value
            eventProperties[key].Should().Be(value);
        }

        [Test]
        public void CONVERT_FILTER_SHOULD_RETURN_ZERO_ON_TO_INT_WHEN_VALUE_IS_NOT_A_NUMBER()
        {
            //Arrange
            var filter = new ConvertFilter();
            const string key = "key";
            const string value = "hello";
            filter.AddToInt(key);
            var eventProperties = new Dictionary<string, object> {{key, value}};

            //Act
            filter.PrepareEvent(eventProperties);

            //value
            eventProperties[key].Should().Be(0);
        }

        [Test]
        [TestCase(",.", "1,2.3", new[] {"1", "2", "3"})]
        [TestCase(",", "1,2.3", new[] {"1", "2.3"})]
        public void CONVERT_TO_ARRAY_FILTER_SHOULD_RETURN_SPLIT_ARRAY(string separators, string value,
            string[] expected)
        {
            //Arrange
            const string key = "key";
            var filter = new ConvertToArrayFilter {SourceKey = key, Seperators = separators};
            var eventProperties = new Dictionary<string, object> {{key, value}};

            //Act
            filter.PrepareEvent(eventProperties);

            //value
            eventProperties[key].Should().BeEquivalentTo(expected);
        }

        [Test]
        public void CONVERT_TO_ARRAY_FILTER_SHOULD_NOT_CONVERT_WHEN_VALUE_IS_NOT_STRING()
        {
            //Arrange
            const string key = "key";
            var value = new object();
            var filter = new ConvertToArrayFilter {SourceKey = key, Seperators = ","};
            var eventProperties = new Dictionary<string, object> {{key, value}};

            //Act
            filter.PrepareEvent(eventProperties);

            //value
            eventProperties[key].Should().Be(value);
        }

        [Test]
        public void RENAME_FILTER_SHOULD_RENAME_CORRECT_KEY()
        {
            //Arrange
            const string sourceKey = "source";
            const string destinationKey = "destination";
            var value = new object();
            var filter = new RenameKeyFilter {Key = sourceKey, RenameTo = destinationKey, Overwrite = false};
            var eventProperties = new Dictionary<string, object> {{sourceKey, value}};

            //Act
            filter.PrepareEvent(eventProperties);

            //value
            eventProperties[destinationKey].Should().Be(value);
            eventProperties.ContainsKey(sourceKey).Should().BeFalse();
        }

        [Test]
        public void RENAME_FILTER_SHOULD_RENAME_CORRECT_KEY_AND_OVERWRITE()
        {
            //Arrange
            const string sourceKey = "source";
            const string destinationKey = "destination";
            var value = new object();
            var value2 = new object();
            var filter = new RenameKeyFilter {Key = sourceKey, RenameTo = destinationKey, Overwrite = true};
            var eventProperties = new Dictionary<string, object> {{sourceKey, value}, {destinationKey, value2}};

            //Act
            filter.PrepareEvent(eventProperties);

            //value
            eventProperties[destinationKey].Should().Be(value);
            eventProperties.ContainsKey(sourceKey).Should().BeFalse();
        }

        [Test]
        public void REMOVE_KEY_FILTER_SHOULD_REMOVE_FIELD_FROM_EVENT()
        {
            //Arrange
            const string key = "key";
            var filter = new RemoveKeyFilter {Key = key};
            var eventProperties = new Dictionary<string, object> {{key, null}};

            //Act
            filter.PrepareEvent(eventProperties);

            //Assert
            eventProperties.ContainsKey(key).Should().BeFalse();
        }

        [Test]
        public void JSON_FILTER_SHOULD_SET_KEY_WITH_COMPLETE_TOKEN_WHEN_FLATTEN_IS_DISABLED()
        {
            //Arrange
            const string key = "key";
            var filter = new JsonFilter() {SourceKey = key, FlattenJson = false};
            var eventProperties = new Dictionary<string, object> {{key, "{ \"prop\": \"value\"}"}};

            //Act
            filter.PrepareEvent(eventProperties);

            //Assert
            ((JToken)eventProperties[key])["prop"].Value<string>().Should().Be("value");
        }

        [Test]
        public void JSON_FILTER_SHOULD_FLATTEN_JSON()
        {
            //Arrange
            const string key = "key";
            var filter = new JsonFilter() { SourceKey = key, FlattenJson = true };
            var eventProperties = new Dictionary<string, object> { { key, "{ \"prop\": \"value\"}" } };

            //Act
            filter.PrepareEvent(eventProperties);

            //Assert
            eventProperties["prop"].Should().Be("value");
        }

        private class MockEventObject
        {
            private readonly string _value;

            public MockEventObject(string value)
            {
                _value = value;
            }

            public override string ToString()
            {
                return _value;
            }
        }
    }
}