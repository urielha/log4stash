using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4stash.Authentication.Aws;
using RestSharp;
using RestSharp.Authenticators;

namespace log4stash.Authentication
{
    public class AwsAuthenticationMethod : IAuthenticator
    {
        public string Aws4SignerSecretKey { get; set; }

        public string Aws4SignerAccessKey { get; set; }

        public string Aws4SignerRegion { get; set; }

        public void Authenticate(IRestClient client, IRestRequest request)
        {
            var body = request.Parameters.First(p => p.Type == ParameterType.RequestBody).Value.ToString();
            var contentHash = Aws4SignerBase.CanonicalRequestHashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(body));
            var contentHashString = Aws4SignerBase.ToHexString(contentHash, true);

            var headers = new Dictionary<string, string>
            {
                {Aws4SignerBase.X_Amz_Content_SHA256, contentHashString},
                {"content-type", "application/json"}
            };

            var signer = new Aws4SignerForAuthorizationHeader
            {
                EndpointUri = new Uri(client.BaseUrl + request.Resource),
                HttpMethod = request.Method.ToString(),
                Service = "es",
                Region = Aws4SignerRegion
            };

            var authorizationHeaderValue = signer.ComputeSignature(headers,
                "",  // no query parameters
                contentHashString,
                Aws4SignerAccessKey,
                Aws4SignerSecretKey);

            foreach (var header in headers.Keys)
            {
                if (header.Equals("host", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (header.Equals("content-length", StringComparison.OrdinalIgnoreCase))
                    request.AddHeader("content-length", long.Parse(headers[header]).ToString());
                else if (header.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                    request.AddHeader("content-type", headers[header]);
                else
                    request.AddHeader(header, headers[header]);
            }
            request.AddHeader("Authorization", authorizationHeaderValue);
        }
    }
}