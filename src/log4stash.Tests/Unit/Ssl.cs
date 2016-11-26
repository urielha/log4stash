using log4stash.Authentication;
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
            var credentials = new AuthenticationMethodChooser();
            credentials.AddBasic(new BasicAuthenticationMethod() {Username = "username", Password = "password"});
            var client = new WebElasticClient("server", 8080, true, true, credentials);
            Assert.AreEqual(expectedUrl, client.Url);
        }
    }
}