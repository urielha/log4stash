using NUnit.Framework;

namespace log4net.ElasticSearch.Tests
{
    [TestFixture]
    public class CoreTests
    {
        [Test]
        public void Ssl_should_create_https()
        {
            const string expectedUrl = "https://server:8080/";
            var client = new WebElasticClient("server", 8080, true, true, "username", "password");
            Assert.AreEqual(expectedUrl, client.Url);
        }
    }
}