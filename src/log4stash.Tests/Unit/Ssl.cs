using log4stash.Authentication;
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
            var credentials = new AuthenticationMethodChooser();
            credentials.AddBasic(new BasicAuthenticationMethod() {Username = "username", Password = "password"});
            var client = new WebElasticClient(servers, 10000, true, true, credentials);
            Assert.AreEqual(expectedUrl, client.Url);
        }
    }
}