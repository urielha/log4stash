using log4stash.Authentication;
using log4stash.Configuration;
using log4stash.ErrorHandling;
using NUnit.Framework;

namespace log4stash.UnitTests
{
    [TestFixture]
    public class Ssl
    {
        [Test]
        public void Ssl_should_create_https()
        {
            const string expectedUrl = "https://server:8080/";
            var servers = new ServerDataCollection() {new ServerData() {Address = "server", Port = 8080, Path = ""} };
            var eventWriter = new LogLogEventWriter();
            var credentials = new AuthenticationMethodChooser(null);
            credentials.AddBasic(new BasicAuthenticationMethod() {Username = "username", Password = "password"});
            var client = new WebElasticClient(servers, 10000, true, true, eventWriter, credentials);
            Assert.AreEqual(expectedUrl, client.Url);
        }
    }
}