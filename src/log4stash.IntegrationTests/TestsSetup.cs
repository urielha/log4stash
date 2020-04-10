using System;
using System.IO;
using System.Reflection;
using Elasticsearch.Net;
using log4net;
using log4net.Config;
using log4net.Repository;
using Nest;
using Nest.JsonNetSerializer;
using NUnit.Framework;

namespace log4stash.IntegrationTests
{
    public class TestsSetup
    {
        public IElasticClient Client;
        public readonly string TestIndex = "log_test_" + DateTime.Now.ToString("yyyy.MM.dd");
        private ILoggerRepository _repository;

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            var configFile = TestContext.Parameters["configFile"];
            _repository = LogManager.GetRepository(Assembly.GetCallingAssembly());
            XmlConfigurator.Configure(_repository, new FileInfo(configFile));
            string host = null;
            var port = 0;
            string path = null;
            QueryConfiguration(appender =>
            {
                appender.IndexName = TestIndex;
                var serverData = appender.Servers.GetRandomServerData();
                host = serverData.Address;
                port = serverData.Port;
                path = serverData.Path;
           });
            
            var pool = new SingleNodeConnectionPool(new Uri(string.Format("http://{0}:{1}{2}", host, port, path)));

            var elasticSettings =
                new ConnectionSettings(pool, SourceSerializer)
                    .DefaultIndex(TestIndex);

            Client = new Nest.ElasticClient(elasticSettings);
        }

        private IElasticsearchSerializer SourceSerializer(IElasticsearchSerializer builtin, IConnectionSettingsValues values)
        {
            return new JsonNetSerializer(builtin, values);
        }
        
        [OneTimeTearDown]
        public void FixtureTearDown()
        {
            if (Client == null) return;

            var response = Client.Indices.Exists(new IndexExistsRequest(TestIndex));
            if (response.Exists)
            {
                Client.Indices.Delete(new DeleteIndexRequest(TestIndex));
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

        protected void QueryConfiguration(Action<ElasticSearchAppender> action)
        {
            
            if (_repository == null) return;
            var appenders = _repository.GetAppenders();
            foreach (var appender in appenders)
            {
                var elsAppender = appender as ElasticSearchAppender;
                if (elsAppender == null || action == null) continue;
                action(elsAppender);
                elsAppender.ActivateOptions();
            }
        }
    }
}