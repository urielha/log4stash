using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;

namespace log4stash.ElasticClient.RestSharp
{
    public class RestSharpClientFactory : IRestClientFactory
    {
        public IRestClient Create(string baseUrl, int timeout, IAuthenticator authenticator)
        {
            return new RestClient(baseUrl){Authenticator = authenticator, Timeout = timeout};
        }
    }
}
