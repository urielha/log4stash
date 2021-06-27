using log4stash.Configuration;
using log4stash.ErrorHandling;
using RestSharp.Authenticators;

namespace log4stash.ElasticClient
{
    public class WebElasticClientFactory : IElasticClientFactory
    {
        public IElasticsearchClient CreateClient(IServerDataCollection servers,
            int timeout,
            bool ssl,
            bool allowSelfSignedServerCert,
            IExternalEventWriter eventWriter,
            IAuthenticator authenticationMethod)
        {
            return new WebElasticClient(servers, timeout, ssl, allowSelfSignedServerCert, eventWriter, authenticationMethod);
        }
    }
}
