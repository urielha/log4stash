using log4stash.Configuration;
using RestSharp.Authenticators;

namespace log4stash.ElasticClient
{
    public interface IElasticClientFactory
    {
        IElasticsearchClient CreateClient(IServerDataCollection servers,
            int timeout,
            bool ssl,
            bool allowSelfSignedServerCert,
            IAuthenticator authenticationMethod);
    }
}