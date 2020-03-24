using log4stash.Configuration;
using RestSharp.Authenticators;

namespace log4stash.ElasticClient
{
    public class WebElasticClientFactory : IElasticClientFactory
    {
        public IElasticsearchClient CreateClient(IServerDataCollection servers,
            int timeout,
            bool ssl,
            bool allowSelfSignedServerCert,
            IAuthenticator authenticationMethod)
        {
            return new WebElasticClient(servers, timeout, ssl, allowSelfSignedServerCert, authenticationMethod);
        }
    }
}
