using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using log4net;
using log4net.Core;
using log4stash.Configuration;
using log4stash.Filters;
using Nest;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace log4stash.Tests.Integration
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
        public void Log_With_Custom_Id()
        {
            string oldDocId = null;
            QueryConfiguration(appender =>
            {
                if(!appender.IndexOperationParams.TryGetValue("_id", out oldDocId)) 
                {
                    oldDocId = null;
                }
                appender.DocumentIdSource = "IdSource";
            });
            ThreadContext.Properties["IdSource"] = "TEST_ID";
            _log.Info("loggingtest");

            Client.Refresh(TestIndex);

            var searchResults = Client.Search<JObject>(s => s.AllTypes().Query(q => q.Ids(descriptor => descriptor.Values("TEST_ID"))));

            QueryConfiguration(appender =>
            {
                appender.DocumentIdSource = oldDocId;
            });

            Assert.AreEqual(1, searchResults.Total);
        }

        [Test]
        public void Log_With_Custom_Routing()
        {
            string oldRoutingSource = null;
            QueryConfiguration(appender =>
            {
                if (!appender.IndexOperationParams.TryGetValue("_routing", out oldRoutingSource))
                {
                    oldRoutingSource = null;
                }
                appender.IndexOperationParams.AddParameter(new IndexOperationParam("_routing", "%{RoutingSource}"));
            });
            ThreadContext.Properties["RoutingSource"] = "ROUTING";
            _log.Info("loggingtest");

            Client.Refresh(TestIndex);
            var query = new TermsQuery
            {
                Field = "_routing",
                Terms = new[] { "ROUTING" }
            };
            var searchResults = Client.Search<JObject>(s => s.AllTypes().Query(descriptor => query));

            QueryConfiguration(appender =>
            {
                if (oldRoutingSource == null)
                {
                    appender.IndexOperationParams.Remove("_routing");
                }
                else
                {
                    appender.IndexOperationParams.AddParameter(new IndexOperationParam("_routing", oldRoutingSource));
                }
            });

            Assert.AreEqual(1, searchResults.Total);
        }

        [Test]
        public void log_async_message()
        {
            bool originIndexAsync = false;
            QueryConfiguration(appender =>
            {
                originIndexAsync = appender.IndexAsync;
                appender.IndexAsync = true;
            });

            _log.Info("dummy async");

            int tries = 5;
            ISearchResponse<JObject> searchResults = null;
            while (--tries >= 0)
            {
                Client.Refresh(TestIndex);

                searchResults = Client.Search<JObject>(s => s.AllTypes().AllIndices());

                if (searchResults.Total > 0)
                {
                    break;
                }
                Thread.Sleep(100);
            }
            Assert.AreEqual(1, searchResults.Total);

            QueryConfiguration(appender =>
            {
                appender.IndexAsync = originIndexAsync;
            });

        }

        [Test]
        public void Log_exception_string_without_object()
        {
            const string exceptionString = "Exception string";
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
        [TestCase(new[] {",", " "}, new[] {":", "="}, "", false, TestName = "Can_read_KvFilter_properties: Regular1")]
        [TestCase(new[] {";", " "}, new[] {"~"}, "", false, TestName = "Can_read_KvFilter_properties: Regular2")]
        [TestCase(new[] {";"}, new[] {"="}, "", true,
            TestName =
                "Can_read_KvFilter_properties: No whiteSpace on fieldSplit causes the 'another ' key and raise spaces issue"
            )]
        [TestCase(new[] {";"}, new[] {"="}, " ", false,
            TestName = "Can_read_KvFilter_properties: No whiteSpace but with trimming, fix the 'another' key")]
        [TestCase(new[] {"\\|", " "}, new[] {"\\>"}, "", false,
            TestName = "Can_read_KvFilter_properties: Regex chars need to be escaped with backslash")]
        [TestCase(new[] {"\n"}, new[] {":"}, " ", false, TestName = "Can_read_KvFilter_properties: NewLine")]
        public void Can_read_KvFilter_properties(string[] fieldSplit, string[] valueSplit, string trim,
            bool expectAnotherToBeNull)
        {
            IElasticAppenderFilter oldFilters = null;
            QueryConfiguration(appender =>
            {
                oldFilters = appender.ElasticFilters;
                var newFilters = new ElasticAppenderFilters();
                
                newFilters.AddFilter(new KvFilter()
                {
                    FieldSplit = string.Join("", fieldSplit),
                    ValueSplit = string.Join("", valueSplit),
                    TrimKey = trim,
                    TrimValue = trim
                });
                appender.ElasticFilters = newFilters;
            });

            _log.InfoFormat(
                "this is message{1}key{0}value{1}another {0} 'another'{1}object{0}[this is object :)]",
                valueSplit[0].TrimStart('\\'), fieldSplit[0].TrimStart('\\'));

            Client.Refresh(TestIndex);
            var searchResults = Client.Search<dynamic>(s => s.AllIndices().Type("LogEvent").Take(1));

            var entry = searchResults.Documents.First();

            QueryConfiguration(appender => appender.ElasticFilters = oldFilters);

            Assert.IsNotNull(entry.key);
            Assert.AreEqual("value", entry.key.ToString());

            Assert.IsNotNull(entry["object"]);
            Assert.AreEqual("this is object :)", entry["object"].ToString());

            if (entry.another == null)
            {
                Assert.IsTrue(expectAnotherToBeNull, "only on the spaces issues test this 'another' should be null");
            }
            else
            {
                Assert.AreEqual("another", entry.another.ToString());
            }
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
            Assert.That(doc["0"], Is.Null.Or.Empty);
        }

        [Test]
        public void Can_convert_to_Array_filter()
        {
            _log.Info("someIds=[123, 124 ,125 , 007] anotherIds=[33]");

            Client.Refresh(TestIndex);

            var res = Client.Search<JObject>(s => s.AllIndices().Type("LogEvent").Take(1));
            var doc = res.Documents.First();
            Assert.AreEqual(true, doc["someIds"].HasValues);
            Assert.Contains("123", doc["someIds"].Values<string>().ToArray());
            Assert.AreEqual(true, doc["anotherIds"].HasValues);
            Assert.AreEqual("33", doc["anotherIds"].Values<string>().First());
        }

        [Test]
        public void convert_filter_to_string()
        {
            var sp = new StringProducer();
            log4net.GlobalContext.Properties["shouldBeString"] = sp;
            _log.Debug("dummy");

            Client.Refresh(TestIndex);

            var res = Client.Search<JObject>(s => s.AllIndices().Type("LogEvent").Take(1));
            var doc = res.Documents.First();

            Assert.AreEqual(doc["shouldBeString"].Value<string>(), sp.GetInnerGuid());
        }

        [Test]
        public void can_parse_log4net_context_stacks()
        {
            const string sourceKey = "UserName";

            IElasticAppenderFilter oldFilters = null;
            QueryConfiguration(appender =>
            {
                oldFilters = appender.ElasticFilters;
                var newFilters = new ElasticAppenderFilters();
                var convert = new ConvertFilter();
                convert.AddToString(sourceKey);

                var toArray = new ConvertToArrayFilter { SourceKey = sourceKey };
                convert.AddToArray(toArray);
                newFilters.AddConvert(convert);
                appender.ElasticFilters = newFilters;
            });

            LogicalThreadContext.Stacks[sourceKey].Push("name1");
            LogicalThreadContext.Stacks[sourceKey].Push("name2");
            _log.Info("hi");

            Client.Refresh(TestIndex);

            var res = Client.Search<JObject>(s => s.AllIndices().Type("LogEvent").Take(1));
            var doc = res.Documents.First();
            var usrName = doc[sourceKey];
            Assert.NotNull(usrName);
            Assert.AreEqual("name1", usrName[0].Value<string>());
            Assert.AreEqual("name2", usrName[1].Value<string>());

            QueryConfiguration(appender =>
            {
                appender.ElasticFilters = oldFilters;
            });
        }

        [Test]
        [TestCase(false, "_", TestName = "parse_json_string_as_object2: Should preserve json structure")]
        [TestCase(true, "_", TestName = "parse_json_string_as_object2: Should flatten the json")]
        [TestCase(true, "S", TestName = "parse_json_string_as_object2: Should flatten the json with 'S' separator")]
        public void parse_json_string_as_object(bool flatten, string separator = "_")
        {
            const string sourceKey = "jsonObject";
            IElasticAppenderFilter oldFilters = null;
            QueryConfiguration(appender =>
            {
                oldFilters = appender.ElasticFilters;
                var newFilters = new ElasticAppenderFilters();
                newFilters.AddFilter(new JsonFilter() { FlattenJson = flatten, Separator = separator, SourceKey = sourceKey });
                appender.ElasticFilters = newFilters;
            });
            var jObject = new JObject
            {
                { "key", "value\r\nnewline" },
                { "Data", new JObject{{"Type","Url"}, {"Host","localhost"}, { "Array", new JArray(Enumerable.Range(0, 5)) }} }
            };
            log4net.LogicalThreadContext.Properties[sourceKey] = jObject.ToString();
            _log.Info("logging jsonObject");

            Client.Refresh(TestIndex);
            var res = Client.Search<JObject>(s => s.AllIndices().Type("LogEvent").Take(1));
            var doc = res.Documents.First();

            QueryConfiguration(appender =>
            {
                appender.ElasticFilters = oldFilters;
            });

            JToken actualObj;
            string key;
            string dataType;
            string dataHost;
            string dataArrayFirst;
            string dataArrayLast;
            if (flatten)
            {
                actualObj = doc;
                key = actualObj["key"].ToString();
                dataType = actualObj["Data" + separator + "Type"].ToString();
                dataHost = actualObj["Data" + separator + "Host"].ToString();
                dataArrayFirst = actualObj["Data" + separator + "Array" + separator + "0"].ToString();
                dataArrayLast = actualObj["Data" + separator + "Array" + separator + "4"].ToString();
            }
            else
            {
                actualObj = doc[sourceKey];
                key = actualObj["key"].ToString();
                dataType = actualObj["Data"]["Type"].ToString();
                dataHost = actualObj["Data"]["Host"].ToString();
                dataArrayFirst = actualObj["Data"]["Array"][0].ToString();
                dataArrayLast = actualObj["Data"]["Array"][4].ToString();
            }
            Assert.IsNotNull(actualObj);
            Assert.AreEqual("value\r\nnewline", key);
            Assert.AreEqual("Url", dataType);
            Assert.AreEqual("localhost", dataHost);
            Assert.AreEqual("0", dataArrayFirst);
            Assert.AreEqual("4", dataArrayLast);
        }

        [Test]
        [TestCase(false, TestName = "parse_xml_string_as_object2: Should preserve xml structure in json format")]
        [TestCase(true, TestName = "parse_xml_string_as_object2: Should flatten the xml")]
        public void parse_xml_string_as_object(bool flatten)
        {
            const string separator = "_";
            const string sourceKey = "xmlObject";
            IElasticAppenderFilter oldFilters = null;

            QueryConfiguration(appender =>
            {
                oldFilters = appender.ElasticFilters;
                var newFilters = new ElasticAppenderFilters();
                newFilters.AddFilter(new XmlFilter { SourceKey = sourceKey, FlattenXml = flatten, Separator = separator });
                appender.ElasticFilters = newFilters;
            });

            var xmlDoc = new XmlDocument();
            var parentNode = xmlDoc.CreateElement("Parent");
            var parentAttribute = xmlDoc.CreateAttribute("key");
            parentAttribute.Value = "value\r\nnewline";
            parentNode.Attributes.Append(parentAttribute);
            xmlDoc.AppendChild(parentNode);
            foreach (var i in Enumerable.Range(0, 5))
            {
                var childNode = xmlDoc.CreateElement("Child");
                var childAttribute = xmlDoc.CreateAttribute("id");
                childAttribute.Value = i.ToString();
                childNode.Attributes.Append(childAttribute);
                parentNode.AppendChild(childNode);
            }

            var xmlString = ConvertXmlToString(xmlDoc);
            LogicalThreadContext.Properties[sourceKey] = xmlString;
            _log.Info("logging xmlObject");

            Client.Refresh(TestIndex);

            var res = Client.Search<JObject>(s => s.AllIndices().Type("LogEvent").Take(1));
            var doc = res.Documents.First();

            QueryConfiguration(appender =>
            {
                appender.ElasticFilters = oldFilters;
            });

            if (flatten)
            {
                Assert.NotNull(doc);
                Assert.AreEqual(doc["Parent" + separator + "@key"].ToString(), "value\r\nnewline");
                Assert.AreEqual(doc["Parent" + separator + "Child" + separator + "0" + separator + "@id"].ToString(), "0");
                Assert.AreEqual(doc["Parent" + separator + "Child" + separator + "1" + separator + "@id"].ToString(), "1");
            }
            else
            {
                var jsonObject = doc[sourceKey];
                Assert.NotNull(jsonObject);
                Assert.AreEqual(jsonObject["Parent"]["@key"].Value<string>(), "value\r\nnewline");
                var arr = jsonObject["Parent"]["Child"].ToArray();
                foreach (var i in Enumerable.Range(0, 5))
                {
                    Assert.AreEqual(arr[i]["@id"].Value<int>(), i);
                }
            }
        }

        [Test]
        public void DropEventsOverBulkLimit()
        {
            const int timeout = 1000;
            int oldBulkSize = 0;
            int oldTimeout = 0;
            QueryConfiguration(appender =>
            {
                appender.DropEventsOverBulkLimit = true;
                oldBulkSize = appender.BulkSize;
                oldTimeout = appender.BulkIdleTimeout;
                appender.BulkSize = 1;
                appender.BulkIdleTimeout = timeout;
            });

            for (int i = 0; i < 10; i++)
            {
                _log.Info("info...");
            }

            Thread.Sleep(timeout + 1000);

            Client.Refresh(TestIndex);

            var res = Client.Search<JObject>(s => s.AllIndices().Type("LogEvent"));
            Assert.AreEqual(1, res.Total);

            QueryConfiguration(appender =>
            {
                appender.DropEventsOverBulkLimit = false;
                appender.BulkSize = oldBulkSize;
                appender.BulkIdleTimeout = oldTimeout;
            });
        }

        [Test]
        [Ignore("the build agent have problems on running performance")]
        public static void Performance()
        {
            IElasticAppenderFilter oldFilters = null;
            QueryConfiguration(appender =>
            {
                appender.BulkSize = 4000;
                appender.BulkIdleTimeout = -1;
                oldFilters = appender.ElasticFilters;
                var newFilters = new ElasticAppenderFilters();
                newFilters.AddFilter(new GrokFilter() { Pattern = "testNum: {INT:testNum}, name is {WORD:name} and guid {UUID:guid}" });
                appender.ElasticFilters = newFilters;
            });

            PerformanceTests.Test(1, 32000);

            QueryConfiguration(appender => appender.ElasticFilters = oldFilters);
        }

        private static string ConvertXmlToString(XmlDocument xmlDoc)
        {
            using (var stringWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter))
                {
                    xmlDoc.WriteContentTo(xmlWriter);
                    xmlWriter.Flush();
                    return stringWriter.GetStringBuilder().ToString();
                }
            }
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

    public class StringProducer
    {
        private readonly string _guid;

        public string GetInnerGuid()
        {
            return _guid;
        }

        public StringProducer()
        {
            _guid = Guid.NewGuid().ToString();
        }

        public override string ToString()
        {
            return _guid;
        }
    }
}
