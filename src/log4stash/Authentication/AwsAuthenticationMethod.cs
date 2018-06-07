using System;
using System.Collections.Generic;
using System.Text;
using log4stash.Authentication.Aws;

namespace log4stash.Authentication
{
    public class AwsAuthenticationMethod : IAuthenticationMethod
    {
        public string Aws4SignerSecretKey { get; set; }

        public string Aws4SignerAccessKey { get; set; }

        public string Aws4SignerRegion { get; set; }

        public string CreateAuthenticationHeader(RequestData requestData)
        {
            var webRequest = requestData.WebRequest;
            var contentHash = Aws4SignerBase.CanonicalRequestHashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(requestData.RequestString));
            var contentHashString = Aws4SignerBase.ToHexString(contentHash, true);

            var headers = new Dictionary<string, string>
                    {
                        {Aws4SignerBase.X_Amz_Content_SHA256, contentHashString},
                        {"content-type", "application/json"}
                    };

            var signer = new Aws4SignerForAuthorizationHeader
            {
                EndpointUri = new Uri(requestData.Url),
                HttpMethod = webRequest.Method,
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
                    webRequest.ContentLength = long.Parse(headers[header]);
                else if (header.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                    webRequest.ContentType = headers[header];
                else
                    webRequest.Headers.Add(header, headers[header]);
            }
            return authorizationHeaderValue;
        }
    }
}