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
        public bool AllowSelfSignedServerCert { get; private set; }
        public string BasicAuthUsername { get; private set; }
        public string BasicAuthPassword { get; private set; }
        public string Url { get { return _url; } }

        protected readonly string _url;
        protected readonly string _encodedAuthInfo;

        protected AbstractWebElasticClient(string server, int port,
                                bool ssl, bool allowSelfSignedServerCert, 
                                string basicAuthUsername, string basicAuthPassword)
        {
            Server = server;
            Port = port;
            ServicePointManager.Expect100Continue = false;

            // SSL related properties
            Ssl = ssl;
            AllowSelfSignedServerCert = allowSelfSignedServerCert;
            BasicAuthPassword = basicAuthPassword;
            BasicAuthUsername = basicAuthUsername;

            if(BasicAuthUsername != null && !string.IsNullOrEmpty(BasicAuthUsername.Trim()))
            {
                string authInfo = string.Format("{0}:{1}", BasicAuthUsername, BasicAuthPassword);
                _encodedAuthInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes(authInfo));
            }

            _url = string.Format("{0}://{1}:{2}/", Ssl ? "https" : "http", Server, Port);
        }

        public abstract void PutTemplateRaw(string templateName, string rawBody);
        public abstract void IndexBulk(IEnumerable<InnerBulkOperation> bulk);
        public abstract IAsyncResult IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk);
        
        public abstract void Dispose();
    }

    public class WebElasticClient : AbstractWebElasticClient
    {
        private readonly string _credentials;

        class RequestDetails
        {
            public RequestDetails(WebRequest webRequest, string content)
            {
                WebRequest = webRequest;
                Content = content;
            }

            public WebRequest WebRequest { get; private set; }
            public string Content { get; private set;  }
        }

        public WebElasticClient(string server, int port)
            : this(server, port, false, false, string.Empty, string.Empty)
        {
        }

        public WebElasticClient(string server, int port,
                                bool ssl, bool allowSelfSignedServerCert, 
                                string basicAuthUsername, string basicAuthPassword)
            : base(server, port, ssl, allowSelfSignedServerCert, basicAuthUsername, basicAuthPassword)
        {
            if (Ssl && AllowSelfSignedServerCert)
            {
                ServicePointManager.ServerCertificateValidationCallback += AcceptSelfSignedServerCertCallback;
            }

            if (!string.IsNullOrEmpty(_encodedAuthInfo))
            {
                _credentials = string.Format("{0} {1}", "Basic", _encodedAuthInfo);
            }
        }

        public override void PutTemplateRaw(string templateName, string rawBody)
        {
            var webRequest = WebRequest.Create(string.Concat(_url, "_template/", templateName));
            webRequest.ContentType = "text/json";
            webRequest.Method = "PUT";
            SetBasicAuthHeader(webRequest);
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
            var requestString = PrepareBulk(bulk);

            var webRequest = WebRequest.Create(string.Concat(_url, "_bulk"));
            webRequest.ContentType = "text/plain";
            webRequest.Method = "POST";
            webRequest.Timeout = 10000;
            SetBasicAuthHeader(webRequest);
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

        private void SetBasicAuthHeader(WebRequest request)
        {
            if (!string.IsNullOrEmpty(_credentials)) 
            {
                request.Headers[HttpRequestHeader.Authorization] = _credentials;
            }
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
