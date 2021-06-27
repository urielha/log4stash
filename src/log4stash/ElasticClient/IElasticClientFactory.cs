using log4stash.Configuration;
using log4stash.ErrorHandling;
using RestSharp.Authenticators;

namespace log4stash.ElasticClient
{
    public interface IElasticClientFactory
    {
        IElasticsearchClient CreateClient(IServerDataCollection servers,
            int timeout,
            bool ssl,
            bool allowSelfSignedServerCert,
            IExternalEventWriter eventWriter,
            IAuthenticator authenticationMethod);
    }
}