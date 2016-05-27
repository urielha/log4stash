using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net.Core;
using log4net.ElasticSearch.Filters;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace log4net.ElasticSearch.Tests.Integration
{
    [TestFixture]
    public class ElasticsearchAppenderTests : TestsSetup
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ElasticsearchAppenderTests));

        [Test]
        public void Can_create_an_event_from_log4net()
        {
            _log.Info("loggingtest");

            Client.Refresh(TestIndex);

            var searchResults = Client.Search<JObject>(s => s.AllTypes().Query(q => q.Term("Message", "loggingtest")));

            Assert.AreEqual(1, searchResults.Total);
        }

        [Test]
        public void Log_exception_string_without_object()
        {
            var exceptionString = "Exception string";
            var eventData = new LoggingEventData
            {
                LoggerName = _log.Logger.Name,
                ExceptionString = exceptionString,
                Level = Level.Error,
                Message = "loggingtest",
                TimeStamp = DateTime.Now,
                Domain = "Domain",
            };
            var loggingEvent = new LoggingEvent(eventData);

            _log.Logger.Repository.Log(loggingEvent);

            Client.Refresh(TestIndex);

            var searchResults = Client.Search<JObject>(s => s.AllTypes().Query(q => q.Term("Message", "loggingtest")));

            Assert.AreEqual(1, searchResults.Total);
            var doc = searchResults.Documents.First();
            Assert.AreEqual(exceptionString, doc["Exception"].ToString());
        }

        [Test]
        public void Can_add_and_remove_smart_values()
        {
            _log.Info("loggingtest");

            Client.Refresh(TestIndex);

            var searchResults = Client.Search<JObject>(s => s.AllTypes().Query(q => q.Term("Message", "loggingtest")));

            Assert.AreEqual(1, searchResults.Total);
            var doc = searchResults.Documents.First();
            Assert.IsNull(doc["@type"]);
            Assert.IsNotNull(doc["SmartValue2"]);
            Assert.AreEqual("the type is Special", doc["SmartValue2"].ToString());
        }

        [Test]
        public void Can_read_properties()
        {
            GlobalContext.Properties["globalDynamicProperty"] = "global";
            ThreadContext.Properties["threadDynamicProperty"] = "thread";
            LogicalThreadContext.Properties["logicalThreadDynamicProperty"] = "local thread";
            _log.Info("loggingtest");

            Client.Refresh(TestIndex);
            var searchResults = Client.Search<dynamic>(s => s.AllTypes().Query(q => q.Term("Message", "loggingtest")));

            Assert.AreEqual(1, searchResults.Total);
            var firstEntry = searchResults.Documents.First();
            Assert.AreEqual("global", firstEntry.globalDynamicProperty.ToString());
            Assert.AreEqual("thread", firstEntry.threadDynamicProperty.ToString());
            Assert.AreEqual("local thread", firstEntry.logicalThreadDynamicProperty.ToString());
        }

        [Test]
        public void Properties_with_same_key()
        {
            var value = "level-value";
            log4net.LogicalThreadContext.Properties["Level"] = value;
            _log.Debug("debug kuku");

            Client.Refresh(TestIndex);

            var searchResults = Client.Search<JObject>(s => s.AllIndices().Type("LogEvent").Take(1));

            Assert.AreEqual(1, searchResults.Total);
            var doc = searchResults.Documents.First();

            Assert.AreEqual(doc["Level"].ToString(), value);
        }

        [Test]
        public void Property_with_null_value()
        {
            log4net.LogicalThreadContext.Properties["NullProperty"] = null;
            _log.Debug("debug kuku");

            Client.Refresh(TestIndex);

            var searchResults = Client.Search<JObject>(s => s.AllIndices().Type("LogEvent").Take(1));

            Assert.AreEqual(1, searchResults.Total);
            var doc = searchResults.Documents.First();

            Assert.AreEqual(doc["NullProperty"].ToString(), string.Empty);
        }

        [Test]
        [TestCase(new[] { ",", " " },   new[] { ":", "=" }, "",     TestName = "Can_read_KvFilter_properties: Regular1")]
        [TestCase(new[] { ";", " " },   new[] { "~" },      "",     TestName = "Can_read_KvFilter_properties: Regular2")]
        [TestCase(new[] { ";" },        new[] { "=" },      "",     TestName = "Can_read_KvFilter_properties: No whiteSpace on fieldSplit causes the 'another ' key and raise spaces issue", ExpectedException = typeof(Exception), ExpectedMessage = "spaces issue")]
        [TestCase(new[] { ";" },        new[] { "=" },      " ",    TestName = "Can_read_KvFilter_properties: No whiteSpace but with trimming, fix the 'another' key")]
        [TestCase(new[] { "\\|", " " }, new[] { "\\>" },    "",     TestName = "Can_read_KvFilter_properties: Regex chars need to be escaped with backslash")]
        [TestCase(new[] { "\n" },       new[] { ":" },      " ",    TestName = "Can_read_KvFilter_properties: NewLine")]
        public void Can_read_KvFilter_properties(string[] fieldSplit, string[] valueSplit, string trim)
        {
            ElasticAppenderFilters oldFilters = null;
            QueryConfiguration(appender =>
            {
                oldFilters = appender.ElasticFilters;
                appender.ElasticFilters = new ElasticAppenderFilters();
                appender.ElasticFilters.AddFilter(new KvFilter()
                {
                    FieldSplit = string.Join("", fieldSplit),
                    ValueSplit = string.Join("", valueSplit),
                    TrimKey = trim,
                    TrimValue = trim
                });
                appender.ActivateOptions();
            });

            _log.InfoFormat(
                "this is message{1}key{0}value{1}another {0} 'another'{1}object{0}[this is object :)]",
                valueSplit[0].TrimStart('\\'), fieldSplit[0].TrimStart('\\'));

            Client.Refresh(TestIndex);
            var searchResults = Client.Search<dynamic>(s => s.AllIndices().Type("LogEvent").Take(1));

            var entry = searchResults.Documents.First();

            QueryConfiguration(appender =>
            {
                appender.ElasticFilters = oldFilters;
                appender.ActivateOptions();
            });

            Assert.IsNotNull(entry.key);
            Assert.AreEqual("value", entry.key.ToString());
            
            Assert.IsNotNull(entry["object"]);
            Assert.AreEqual("this is object :)", entry["object"].ToString());

            if (entry.another == null)
            {
                throw new Exception("spaces issue");
            }
            Assert.AreEqual("another", entry.another.ToString());
        }

        [Test]
        public void Can_read_grok_propertis()
        {
            var newGuid = Guid.NewGuid();
            _log.Error("error! name is UnknownError and guid " + newGuid);

            Client.Refresh(TestIndex);
            var res = Client.Search<dynamic>(s => s.AllIndices().Type("LogEvent").Take((1)));
            var doc = res.Documents.First();
            Assert.AreEqual("UnknownError", doc.name.ToString());
            Assert.AreEqual(newGuid.ToString(), doc.the_guid.ToString());
            Assert.IsNullOrEmpty(doc["0"]);
        }

        [Test]
        public void Can_convert_to_Array_filter()
        {
            _log.Info("someIds=[123, 124 ,125 , 007] anotherIds=[33]");

            Client.Refresh(TestIndex);

            var res = Client.Search<JObject>(s => s.AllIndices().Type("LogEvent").Take((1)));
            var doc = res.Documents.First();
            Assert.AreEqual(true, doc["someIds"].HasValues);
            Assert.Contains("123", doc["someIds"].Values<string>().ToArray());
            Assert.AreEqual(true, doc["anotherIds"].HasValues);
            Assert.AreEqual("33", doc["anotherIds"].Values<string>().First());
        }

        [Test]
        public void can_parse_log4net_context_stacks()
        {
            LogicalThreadContext.Stacks["UserName"].Push("name1");
            LogicalThreadContext.Stacks["UserName"].Push("name2");
            _log.Info("hi");

            Client.Refresh(TestIndex);

            var res = Client.Search<JObject>(s => s.AllIndices().Type("LogEvent").Take(1));
            var doc = res.Documents.First();
            var usrName = doc["UserName"];
            Assert.NotNull(usrName);
            Assert.AreEqual("name1", usrName[0].Value<string>());
            Assert.AreEqual("name2", usrName[1].Value<string>());
        }

        [Test]
        public void parse_json_string_as_object()
        {
            const string sourceKey = "jsonObject";
            ElasticAppenderFilters oldFilters = null;

            QueryConfiguration(appender =>
            {
                oldFilters = appender.ElasticFilters;
                appender.ElasticFilters = new ElasticAppenderFilters();
                appender.ElasticFilters.AddFilter(new JsonFilter() { SourceKey = sourceKey });
            });

            var jObject = new JObject { { "key", "value\r\nnewline" }, { "arr", new JArray(Enumerable.Range(0, 5)) } };
            LogicalThreadContext.Properties[sourceKey] = jObject.ToString();
            _log.Info("logging jsonObject");

            Client.Refresh(TestIndex);

            var res = Client.Search<JObject>(s => s.AllIndices().Type("LogEvent").Take(1));
            var doc = res.Documents.First();

            var jsonObject = doc[sourceKey];
            QueryConfiguration(appender => appender.ElasticFilters = oldFilters);

            Assert.NotNull(jsonObject);
            Assert.AreEqual(jsonObject["key"].Value<string>(), "value\r\nnewline");
            var arr = jsonObject["arr"].ToArray();
            foreach (var i in Enumerable.Range(0, 5))
            {
                Assert.AreEqual(arr[i].Value<int>(), i);
            }

        }

        [Test]
        [TestCase(false, TestName = "parse_json_string_as_object2: Should preserve json structure")]
        [TestCase(true, TestName = "parse_json_string_as_object2: Should flatten the json")]
        public void parse_json_string_as_object2(bool flatten)
        {
            ElasticAppenderFilters oldFilters = null;
            QueryConfiguration(appender =>
            {
                oldFilters = appender.ElasticFilters;
                appender.ElasticFilters = new ElasticAppenderFilters();
                appender.ElasticFilters.AddFilter(new JsonFilter() {FlattenJson = flatten});
                appender.ActivateOptions();
            });
            string json =
                "{\"InnerMessage\":\"Starting.\", \"Data\":{\"Type\":\"Server\", \"Host\":\"localhost\", \"Array\":[\"One\", \"Two\"]}}";
            log4net.LogicalThreadContext.Properties["JsonRaw"] = json;
            _log.Info("Info");

            Client.Refresh(TestIndex);
            var res = Client.Search<JObject>(s => s.AllIndices().Type("LogEvent").Take(1));
            var doc = res.Documents.First();

            QueryConfiguration(appender =>
            {
                appender.ElasticFilters = oldFilters;
                appender.ActivateOptions();
            });

            JToken actualObj;
            string innerMessage;
            string dataType;
            string dataHost;
            string dataArrayFirst;
            if (flatten)
            {
                actualObj = doc;
                innerMessage = actualObj["InnerMessage"].ToString();
                dataType = actualObj["Data.Type"].ToString();
                dataHost = actualObj["Data.Host"].ToString();
                dataArrayFirst = actualObj["Data.Array.0"].ToString();
            }
            else
            {
                actualObj = doc["JsonRaw"];
                innerMessage = actualObj["InnerMessage"].ToString();
                dataType = actualObj["Data"]["Type"].ToString();
                dataHost = actualObj["Data"]["Host"].ToString();
                dataArrayFirst = actualObj["Data"]["Array"][0].ToString();
            }
            Assert.IsNotNull(actualObj);
            Assert.AreEqual("Starting.", innerMessage);
            Assert.AreEqual("Server", dataType);
            Assert.AreEqual("localhost", dataHost);
            Assert.AreEqual("One", dataArrayFirst);
        }

        [Test]
        [TestCase("1s", 0, TestName = "ttl elapsed")]
        [TestCase("20m", 1, TestName = "ttl didn't elapsed")]
        public void test_ttl(string ttlValue, int expectation)
        {
            const string ttlTemplateName = "ttltemplate";
            const int toWaitMillisec = 3000;

            Client.PutIndexTemplate(ttlTemplateName,
                descriptor =>
                    descriptor.Template("*")
                        .Settings(settings => settings.Setting("indices.ttl.interval", "1s").Setting("index.ttl.interval", "1s"))
                        .Mappings(mapping => mapping.Map("_default_", desc => desc.TtlField(ttlField => ttlField.Enable().Default("1d")))));

            ElasticAppenderFilters oldFilters = null;
            QueryConfiguration(
                appender =>
                {
                    oldFilters = appender.ElasticFilters;
                    appender.ElasticFilters = new ElasticAppenderFilters();
                    appender.ElasticFilters.AddFilter(new AddValueFilter() { Key = "_ttl", Value = ttlValue });
                });

            _log.Info("test");
            Client.Refresh(TestIndex);
            var res = Client.Search<dynamic>(s => s.AllIndices().Type("LogEvent"));
            Assert.AreEqual(1, res.Total);

            // "Magic". The time of deletion isn't consistent :/
            int numOfTries = 20;
            while (--numOfTries > 0)
            {
                Client.Refresh(TestIndex);
                Client.Optimize(TestIndex);
                res = Client.Search<dynamic>(s => s.AllTypes().AllIndices());
                numOfTries = res.Total == expectation ? 0 : numOfTries;
                Thread.Sleep(toWaitMillisec);
            }

            res = Client.Search<dynamic>(s => s.AllIndices().Type("LogEvent"));
            Client.DeleteIndexTemplate(ttlTemplateName);
            QueryConfiguration(appender => appender.ElasticFilters = oldFilters);

            Assert.AreEqual(expectation, res.Total);
        }

        [Test]
        [Ignore("the build agent have problems on running performance")]
        public static void Performance()
        {
            ElasticAppenderFilters oldFilters = null;
            QueryConfiguration(appender =>
            {
                appender.BulkSize = 4000;
                appender.BulkIdleTimeout = -1;
                oldFilters = appender.ElasticFilters;
                //appender.ElasticFilters = new ElasticAppenderFilters();
                appender.ElasticFilters.AddFilter(new GrokFilter() { Pattern = "testNum: {INT:testNum}, name is {WORD:name} and guid {UUID:guid}" });
            });

            PerformanceTests.Test(1, 32000);

            QueryConfiguration(appender => appender.ElasticFilters = oldFilters);
        }
    }

    static class PerformanceTests
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ElasticSearchAppender));

        public static void Test(int numberOfTasks, int numberOfCycles)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < numberOfTasks; i++)
            {
                int i1 = i;
                tasks.Add(Task.Run(() => Runner(i1, numberOfCycles)));
            }
            Task.WaitAll(tasks.ToArray());
        }

        private static void Runner(int t, int numberOfCycles)
        {
            log4net.ThreadContext.Properties["taskNumber"] = t;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < numberOfCycles; i++)
            {
                Logger.InfoFormat("testNum: {0}, name is someName and guid {1}", i, Guid.NewGuid());
            }
            sw.Stop();

            Console.WriteLine("Ellapsed: {0}, numPerSec: {1}",
                sw.ElapsedMilliseconds, numberOfCycles / (sw.ElapsedMilliseconds / (double)1000));
        }
    }
}
