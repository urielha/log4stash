using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using log4net.Util;
using Newtonsoft.Json;

namespace log4stash
{
    public abstract class AbstractWebElasticClient : IElasticsearchClient
    {
        public string Server { get; private set; }
        public int Port { get; private set; }
        public bool Ssl { get; private set; }
        public int Timeout { get; private set; }
        public bool AllowSelfSignedServerCert { get; private set; }
        public string BasicAuthUsername { get; private set; }
        public string BasicAuthPassword { get; private set; }
        public bool UseAws4Signer { get; private set; }
        public string Aws4SignerRegion { get; private set; }
        public string Aws4SignerAccessKey { get; private set; }
        public string Aws4SignerSecretKey { get; private set; }
        public string Url { get; private set; }

        protected AbstractWebElasticClient(string server, int port,
                                bool ssl, bool allowSelfSignedServerCert,
                                string basicAuthUsername, string basicAuthPassword, bool useAWS4Signer,
                string aws4SignerRegion, string aws4SignerAccessKey, string aws4SignerSecretKey, int timeout)
        {
            Server = server;
            Port = port;
            ServicePointManager.Expect100Continue = false;

            // SSL related properties
            Ssl = ssl;
            AllowSelfSignedServerCert = allowSelfSignedServerCert;
            BasicAuthPassword = basicAuthPassword;
            UseAws4Signer = useAWS4Signer;
            Aws4SignerRegion = aws4SignerRegion;
            Aws4SignerAccessKey = aws4SignerAccessKey;
            Aws4SignerSecretKey = aws4SignerSecretKey;
            BasicAuthUsername = basicAuthUsername;
            Timeout = timeout;
            Url = string.Format("{0}://{1}:{2}/", Ssl ? "https" : "http", Server, Port);
        }

        public abstract void PutTemplateRaw(string templateName, string rawBody);
        public abstract void IndexBulk(IEnumerable<InnerBulkOperation> bulk);
        public abstract IAsyncResult IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk);
        public abstract void Dispose();
    }

    public class WebElasticClient : AbstractWebElasticClient
    {
        private class RequestDetails
        {
            public RequestDetails(WebRequest webRequest, string content)
            {
                WebRequest = webRequest;
                Content = content;
            }

            public WebRequest WebRequest { get; private set; }
            public string Content { get; private set;  }
        }

        public WebElasticClient(string server, int port, bool ssl, bool allowSelfSignedServerCert,
                                string basicAuthUsername, string basicAuthPassword, int timeout)
            : this(server, port, ssl, allowSelfSignedServerCert, basicAuthUsername, basicAuthPassword, false, string.Empty, string.Empty, string.Empty, timeout)
        {
        }

        public WebElasticClient(string server, int port, int timeout)
            : this(server, port, false, false, string.Empty, string.Empty, false, string.Empty, string.Empty, string.Empty, timeout)
        {
        }

        public WebElasticClient(string server, int port,
                                bool ssl, bool allowSelfSignedServerCert,
                                string basicAuthUsername, string basicAuthPassword, bool useAWS4Signer, 
                                string aws4SignerRegion, string aws4SignerAccessKey, string aws4SignerSecretKey, int timeout)
            : base(server, port, ssl, allowSelfSignedServerCert, basicAuthUsername, basicAuthPassword, useAWS4Signer,
                aws4SignerRegion, aws4SignerAccessKey, aws4SignerSecretKey, timeout)
        {
            if (Ssl && AllowSelfSignedServerCert)
            {
                ServicePointManager.ServerCertificateValidationCallback += AcceptSelfSignedServerCertCallback;
            }
        }

        public override void PutTemplateRaw(string templateName, string rawBody)
        {
            var url = string.Concat(Url, "_template/", templateName);
            var webRequest = WebRequest.Create(url);
            webRequest.ContentType = "text/json";
            webRequest.Method = "PUT";
            SetHeaders((HttpWebRequest)webRequest, url, rawBody);
            if (SafeSendRequest(new RequestDetails(webRequest, rawBody), webRequest.GetRequestStream))
            {
                SafeGetAndCheckResponse(webRequest.GetResponse);
            }
        }

        public override void IndexBulk(IEnumerable<InnerBulkOperation> bulk)
        {
            var request = PrepareRequest(bulk);
            if (SafeSendRequest(request, request.WebRequest.GetRequestStream))
            {
                SafeGetAndCheckResponse(request.WebRequest.GetResponse);
            }
        }

        public override IAsyncResult IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk)
        {
            var request = PrepareRequest(bulk);
            return request.WebRequest.BeginGetRequestStream(FinishGetRequest, request);
        }

        private void FinishGetRequest(IAsyncResult result)
        {
            var request = (RequestDetails)result.AsyncState;
            if (SafeSendRequest(request, () => request.WebRequest.EndGetRequestStream(result)))
            {
                request.WebRequest.BeginGetResponse(FinishGetResponse, request.WebRequest);
            }
        }

        private void FinishGetResponse(IAsyncResult result)
        {
            var webRequest = (WebRequest)result.AsyncState;
            SafeGetAndCheckResponse(() => webRequest.EndGetResponse(result));
        }

        private RequestDetails PrepareRequest(IEnumerable<InnerBulkOperation> bulk)
        {
            var url = string.Concat(Url, "_bulk");
            var requestString = PrepareBulk(bulk);

            var webRequest = WebRequest.Create(url);
            webRequest.ContentType = "text/plain";
            webRequest.Method = "POST";
            webRequest.Timeout = Timeout;
            SetHeaders((HttpWebRequest)webRequest, url, requestString);
            return new RequestDetails(webRequest, requestString);
        }

        private static string PrepareBulk(IEnumerable<InnerBulkOperation> bulk)
        {
            var sb = new StringBuilder();
            foreach (InnerBulkOperation operation in bulk)
            {
                sb.AppendFormat(
                    @"{{ ""index"" : {{ ""_index"" : ""{0}"", ""_type"" : ""{1}""}} }}",
                    operation.IndexName, operation.IndexType);
                sb.Append("\n");
                
                string json = JsonConvert.SerializeObject(operation.Document);
                sb.Append(json);

                sb.Append("\n");
            }
            return sb.ToString();
        }

        private bool SafeSendRequest(RequestDetails request, Func<Stream> getRequestStream)
        {
            try
            {
                using (var stream = new StreamWriter(getRequestStream()))
                {
                    stream.Write(request.Content);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogLog.Error(GetType(), "Invalid request to ElasticSearch", ex);
            }
            return false;
        }

        private void SafeGetAndCheckResponse(Func<WebResponse> getResponse)
        {
            try
            {
                using (var httpResponse = (HttpWebResponse)getResponse())
                {
                    CheckResponse(httpResponse);
                }
            }
            catch (Exception ex)
            {
                LogLog.Error(GetType(), "Got error while reading response from ElasticSearch", ex);
            }
        }

        private void SetHeaders(HttpWebRequest webRequest, string url, string requestString)
        {
            var authorizationHeaderValue = string.Empty;
            var authIsBasicAuth = !string.IsNullOrEmpty(BasicAuthUsername) && !string.IsNullOrEmpty(BasicAuthPassword);
            var authIsAws4Signer = UseAws4Signer &&
                !string.IsNullOrEmpty(Aws4SignerRegion) &&
                !string.IsNullOrEmpty(Aws4SignerAccessKey) &&
                !string.IsNullOrEmpty(Aws4SignerSecretKey);

            if (authIsBasicAuth)
            {
                var authInfo = string.Format("{0}:{1}", BasicAuthUsername, BasicAuthPassword);
                var encodedAuthInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes(authInfo));
                authorizationHeaderValue = string.Format("{0} {1}", "Basic", encodedAuthInfo);
            }
            else if (authIsAws4Signer)
            {
                var contentHash = AWS4Signer.AWS4SignerBase.CanonicalRequestHashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(requestString));
                var contentHashString = AWS4Signer.AWS4SignerBase.ToHexString(contentHash, true);

                var headers = new Dictionary<string, string>
                    {
                        {AWS4Signer.AWS4SignerBase.X_Amz_Content_SHA256, contentHashString},
                        {"content-type", "text/plain"}
                    };

                var signer = new AWS4Signer.AWS4SignerForAuthorizationHeader
                {
                    EndpointUri = new Uri(url),
                    HttpMethod = webRequest.Method,
                    Service = "es",
                    Region = Aws4SignerRegion
                };

                authorizationHeaderValue = signer.ComputeSignature(headers, 
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
            }

            if (!string.IsNullOrEmpty(authorizationHeaderValue)) 
                webRequest.Headers[HttpRequestHeader.Authorization] = authorizationHeaderValue;
        }

        private bool AcceptSelfSignedServerCertCallback(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            var certificate2 = (certificate as X509Certificate2);

            string subjectCn = certificate2.GetNameInfo(X509NameType.DnsName, false);
            string issuerCn = certificate2.GetNameInfo(X509NameType.DnsName, true);
            if (sslPolicyErrors == SslPolicyErrors.None
                || (Server.Equals(subjectCn) && subjectCn.Equals(issuerCn)))
            {
                return true;
            }

            return false;
        }

        private static void CheckResponse(HttpWebResponse httpResponse)
        {
            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                var buff = new byte[httpResponse.ContentLength];
                using (var response = httpResponse.GetResponseStream())
                {
                    if (response != null)
                    {
                        response.Read(buff, 0, (int) httpResponse.ContentLength);
                    }
                }

                throw new InvalidOperationException(
                    string.Format("Some error occurred while sending request to Elasticsearch.{0}{1}",
                        Environment.NewLine, Encoding.UTF8.GetString(buff)));
            }
        }

        public override void Dispose()
        {
            ServicePointManager.ServerCertificateValidationCallback -= AcceptSelfSignedServerCertCallback;
        }
    }
}
