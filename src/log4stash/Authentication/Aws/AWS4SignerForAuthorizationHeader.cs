using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace log4stash.Authentication.Aws
{
    /// <summary>
    /// Sample AWS4 signer demonstrating how to sign requests to Amazon S3
    /// using an 'Authorization' header.
    /// Original code from: http://docs.aws.amazon.com/general/latest/gr/sigv4_signing.html
    /// </summary>
    public class Aws4SignerForAuthorizationHeader : Aws4SignerBase
    {
        /// <summary>
        /// Computes an AWS4 signature for a request, ready for inclusion as an 
        /// 'Authorization' header.
        /// </summary>
        /// <param name="headers">
        /// The request headers; 'Host' and 'X-Amz-Date' will be added to this set.
        /// </param>
        /// <param name="queryParameters">
        /// Any query parameters that will be added to the endpoint. The parameters 
        /// should be specified in canonical format.
        /// </param>
        /// <param name="bodyHash">
        /// Precomputed SHA256 hash of the request body content; this value should also
        /// be set as the header 'X-Amz-Content-SHA256' for non-streaming uploads.
        /// </param>
        /// <param name="awsAccessKey">
        /// The user's AWS Access Key.
        /// </param>
        /// <param name="awsSecretKey">
        /// The user's AWS Secret Key.
        /// </param>
        /// <returns>
        /// The computed authorization string for the request. This value needs to be set as the 
        /// header 'Authorization' on the subsequent HTTP request.
        /// </returns>
        public string ComputeSignature(IDictionary<string, string> headers,
                      string queryParameters,
                      string bodyHash,
                      string awsAccessKey,
                      string awsSecretKey)
        {
            // first get the date and time for the subsequent request, and convert to ISO 8601 format
            // for use in signature generation
            var requestDateTime = DateTime.UtcNow;
            var dateTimeStamp = requestDateTime.ToString(Iso8601BasicFormat, CultureInfo.InvariantCulture);

            // update the headers with required 'x-amz-date' and 'host' values
            headers.Add(X_Amz_Date, dateTimeStamp);

            var hostHeader = EndpointUri.Host;
            if (!EndpointUri.IsDefaultPort)
                hostHeader += ":" + EndpointUri.Port;
            headers.Add("Host", hostHeader);

            // canonicalize the headers; we need the set of header names as well as the
            // names and values to go into the signature process
            var canonicalizedHeaderNames = CanonicalizeHeaderNames(headers);
            var canonicalizedHeaders = CanonicalizeHeaders(headers);

            // if any query string parameters have been supplied, canonicalize them
            // (note this sample assumes any required url encoding has been done already)
            var canonicalizedQueryParameters = string.Empty;
            if (!string.IsNullOrEmpty(queryParameters))
            {
                var paramDictionary = queryParameters.Split('&').Select(p => p.Split('='))
                    .ToDictionary(nameval => nameval[0],
                        nameval => nameval.Length > 1
                            ? nameval[1] : "");

                var sb = new StringBuilder();
                var paramKeys = new List<string>(paramDictionary.Keys);
                paramKeys.Sort(StringComparer.Ordinal);
                foreach (var p in paramKeys)
                {
                    if (sb.Length > 0)
                        sb.Append("&");
                    sb.AppendFormat("{0}={1}", p, paramDictionary[p]);
                }

                canonicalizedQueryParameters = sb.ToString();
            }

            // canonicalize the various components of the request
            var canonicalRequest = CanonicalizeRequest(EndpointUri,
                HttpMethod,
                canonicalizedQueryParameters,
                canonicalizedHeaderNames,
                canonicalizedHeaders,
                bodyHash);
            Console.WriteLine("\nCanonicalRequest:\n{0}", canonicalRequest);

            // generate a hash of the canonical request, to go into signature computation
            var canonicalRequestHashBytes
                = CanonicalRequestHashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(canonicalRequest));

            // construct the string to be signed
            var stringToSign = new StringBuilder();

            var dateStamp = requestDateTime.ToString(DateStringFormat, CultureInfo.InvariantCulture);
            var scope = string.Format("{0}/{1}/{2}/{3}",
                dateStamp,
                Region,
                Service,
                Terminator);

            stringToSign.AppendFormat("{0}-{1}\n{2}\n{3}\n", Scheme, Algorithm, dateTimeStamp, scope);
            stringToSign.Append(ToHexString(canonicalRequestHashBytes, true));

            Console.WriteLine("\nStringToSign:\n{0}", stringToSign);

            // compute the signing key
            var hashAlgorithm = KeyedHashAlgorithm.Create(HMACSHA256);
            hashAlgorithm.Key = DeriveSigningKey(HMACSHA256, awsSecretKey, Region, dateStamp, Service);

            // compute the AWS4 signature and return it
            var signature = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(stringToSign.ToString()));
            var signatureString = ToHexString(signature, true);
            Console.WriteLine("\nSignature:\n{0}", signatureString);

            var authString = new StringBuilder();
            authString.AppendFormat("{0}-{1} ", Scheme, Algorithm);
            authString.AppendFormat("Credential={0}/{1}, ", awsAccessKey, scope);
            authString.AppendFormat("SignedHeaders={0}, ", canonicalizedHeaderNames);
            authString.AppendFormat("Signature={0}", signatureString);

            var authorization = authString.ToString();
            Console.WriteLine("\nAuthorization:\n{0}", authorization);

            return authorization;
        }
    }
}