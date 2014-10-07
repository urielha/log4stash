using System;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using Nest;
using NUnit.Framework;

namespace log4net.ElasticSearch.Tests
{
    public class TestsSetup
    {
        public ElasticClient Client;
        public readonly string TestIndex = "log_test_" + DateTime.Now.ToString("yyyy-MM-dd");

        [TestFixtureSetUp]
        public void FixtureSetup()
        {
            string host = null;
            int port = 0;
            QueryConfiguration(appender =>
            {
                appender.IndexName = TestIndex;

                host = appender.Server;
                port = appender.Port;
            });

            ConnectionSettings elasticSettings =
                new ConnectionSettings(new Uri(string.Format("http://{0}:{1}", host, port)))
                    .SetDefaultIndex(TestIndex);

            Client = new ElasticClient(elasticSettings);
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            try
            {
                Client.DeleteIndex(descriptor => descriptor.Index(TestIndex));
            }
            catch
            {
                // we don't care if the index does not exists
            }
        }

        [SetUp]
        public void TestSetup()
        {
            FixtureTearDown();
            QueryConfiguration(appender =>
            {
                appender.BulkSize = 1;
                appender.BulkIdleTimeout = -1;
            });
        }

        protected static void QueryConfiguration(Action<ElasticSearchAppender> action)
        {
            var hierarchy = LogManager.GetRepository() as Hierarchy;
            if (hierarchy != null)
            {
                IAppender[] appenders = hierarchy.GetAppenders();
                foreach (IAppender appender in appenders)
                {
                    var elsAppender = appender as ElasticSearchAppender;
                    if (elsAppender != null && action != null)
                    {
                        action(elsAppender);
                    }
                }
            }
        }
    }
}