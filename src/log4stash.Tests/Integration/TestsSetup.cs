using System;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using Nest;
using NUnit.Framework;

namespace log4stash.Tests.Integration
{
    public class TestsSetup
    {
        public IElasticClient Client;
        public readonly string TestIndex = "log_test_" + DateTime.Now.ToString("yyyy-MM-dd");

        public void FixtureSetup()
        {
            string host = null;
            int port = 0;
            string path = null;
            QueryConfiguration(appender =>
            {
                appender.IndexName = TestIndex;
                var serverData = appender.Servers.GetRandomServerData();
                host = serverData.Address;
                port = serverData.Port;
                path = serverData.Path;
           });

            ConnectionSettings elasticSettings =
                new ConnectionSettings(new Uri(string.Format("http://{0}:{1}{2}", host, port, path)))
                    .DefaultIndex(TestIndex);

            Client = new Nest.ElasticClient(elasticSettings);
        }

        public void FixtureTearDown()
        {
            if (Client == null) return;

            var response = Client.IndexExists(new IndexExistsRequest(TestIndex));
            if (response.Exists)
            {
                Client.DeleteIndex(new DeleteIndexRequest(TestIndex));
            }
        }

        [SetUp]
        public void TestSetup()
        {
            FixtureTearDown();
            FixtureSetup();
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
                        elsAppender.ActivateOptions();
                    }
                }
            }
        }
    }
}