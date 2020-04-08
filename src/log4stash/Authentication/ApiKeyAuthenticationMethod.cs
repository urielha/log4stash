using System;
using System.Text;
using RestSharp;
using RestSharp.Authenticators;

namespace log4stash.Authentication
{
    public class ApiKeyAuthenticationMethod : IAuthenticator
    {
        public string ApiKeyBase64 { get; set; }
        public string Id { get; set; }
        public string ApiKey { get; set; }

        public void Authenticate(IRestClient client, IRestRequest request)
        {
            string authorizationHeaderValue;
            if (!string.IsNullOrWhiteSpace(ApiKeyBase64))
            {
                authorizationHeaderValue = ApiKeyBase64;
            }
            else
            {
                var rawHeaderValue = string.Format("{0}:{1}", Id, ApiKey);
                authorizationHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawHeaderValue));
            }

            var authorizationApiKeyHeaderValue = string.Format("{0} {1}", "ApiKey", authorizationHeaderValue);
            request.AddHeader("Authorization", authorizationApiKeyHeaderValue);
        }
    }
}