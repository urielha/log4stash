using System;
using System.Text;
using RestSharp;
using RestSharp.Authenticators;

namespace log4stash.Authentication
{
    public class ApiKeyAuthenticationMethod : IAuthenticator
    {
        public string ApiKey { get; set; }

        public void Authenticate(IRestClient client, IRestRequest request)
        {
            var authorizationHeaderValue = string.Format("{0} {1}", "ApiKey", ApiKey);
            request.AddHeader("Authorization", authorizationHeaderValue);
        }
    }
}