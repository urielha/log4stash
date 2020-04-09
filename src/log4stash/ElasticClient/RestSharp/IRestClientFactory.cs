using RestSharp;
using RestSharp.Authenticators;

namespace log4stash.ElasticClient.RestSharp
{
    public interface IRestClientFactory
    {
        IRestClient Create(string baseUrl, int timeout, IAuthenticator authenticator);
    }
}