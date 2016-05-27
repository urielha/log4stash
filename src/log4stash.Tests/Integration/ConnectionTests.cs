using System;
using System.Linq;
using Nest;
using NUnit.Framework;

namespace log4net.ElasticSearch.Tests.Integration
{
    [TestFixture]
    public class ElasticsearchTests : TestsSetup
    {
        [Test]
        public void Can_insert_record()
        {
            var logEvent = new
            {
                ClassName = "IntegrationTestClass",
                Domain = "TestDomain",
                Exception = "This is a test exception",
                FileName = "c:\\test\\file.txt",
                Fix = "none",
                FullInfo = "A whole bunch of error info dump",
                Identity = "localhost\\user",
                Level = "9",
                LineNumber = "99",
                TimeStamp = DateTime.Now
            };

            var results = Client.Index(logEvent, descriptor => descriptor.Type("anonymous"));

            Assert.IsNotNullOrEmpty(results.Id);
        }

        [Test]
        public void Can_read_inserted_record()
        {
            var logEvent = new
            {
                ClassName = "IntegrationTestClass",
                Exception = "ReadingTest"
            };

            Client.Index(logEvent, descriptor => descriptor.Type("anonymous"));
            Client.Refresh(Indices.AllIndices);

            var searchResults = Client.Search<dynamic>(s => s.AllTypes().MatchAll());
            Assert.AreEqual(1, searchResults.Total);
            Assert.AreEqual("ReadingTest", searchResults.Documents.First().exception.ToString());
        }
    }
}