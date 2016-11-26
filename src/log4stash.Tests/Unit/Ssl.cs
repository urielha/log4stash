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
            var client = new WebElasticClient("server", 8080, true, true, "username", "password", 1000);
            Assert.AreEqual(expectedUrl, client.Url);
        }
    }
}