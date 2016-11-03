using log4stash.Configuration;
using NUnit.Framework;

namespace log4stash.Tests.Unit
{
    [TestFixture]
    public class Ssl
    {
        [Test]
        public void Ssl_should_create_https()
        {
            const string expectedUrl = "https://server:8080/";
            var servers = new ServerDataCollection() {new ServerData() {Address = "server", Port = 8080} };
            var client = new WebElasticClient(servers, true, true, "username", "password");
            Assert.AreEqual(expectedUrl, client.Url);
        }
    }
}