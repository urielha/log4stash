using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using log4net.Util;
using log4stash.Authentication;
using log4stash.Configuration;
using Newtonsoft.Json;

namespace log4stash
{
    public abstract class AbstractWebElasticClient : IElasticsearchClient
    {
        public ServerDataCollection Servers { get; private set; }
        public int Timeout { get; private set; }
        public bool Ssl { get; private set; }
        public bool AllowSelfSignedServerCert { get; private set; }
        public AuthenticationMethodChooser AuthenticationMethod { get; set; }
        public string Url { get { return GetServerUrl(); } }

        protected AbstractWebElasticClient(ServerDataCollection servers,
                                           int timeout,
                                           bool ssl, 
                                           bool allowSelfSignedServerCert,
                                           AuthenticationMethodChooser authenticationMethod)
        {
            Servers = servers;
            Timeout = timeout;
            ServicePointManager.Expect100Continue = false;

            // SSL related properties
            Ssl = ssl;
            AllowSelfSignedServerCert = allowSelfSignedServerCert;
            AuthenticationMethod = authenticationMethod;
        }

        public abstract void PutTemplateRaw(string templateName, string rawBody);
        public abstract void IndexBulk(IEnumerable<InnerBulkOperation> bulk);
        public abstract IAsyncResult IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk);
        public abstract void Dispose();

        protected string GetServerUrl()
        {
            var serverData = Servers.GetRandomServerData();
            var url = string.Format("{0}://{1}:{2}/", Ssl ? "https" : "http", serverData.Address, serverData.Port);
            return url;
        }

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

        public WebElasticClient(ServerDataCollection servers, int timeout)
            : this(servers, timeout, false, false, new AuthenticationMethodChooser())
        {
        }

        public WebElasticClient(ServerDataCollection servers,
                                int timeout,
                                bool ssl,
                                bool allowSelfSignedServerCert,
                                AuthenticationMethodChooser authenticationMethod)
            : base(servers, timeout, ssl, allowSelfSignedServerCert, authenticationMethod)
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
            webRequest.Timeout = Timeout;
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
            var requestString = PrepareBulk(bulk);
            var url = string.Concat(Url, "_bulk");
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
            var requestData = new RequestData {WebRequest = webRequest, Url = url, RequestString = requestString};

            var authorizationHeaderValue = AuthenticationMethod.CreateAuthenticationHeader(requestData);

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
            var serverAddresses = Servers.Select(s => s.Address);
            if (sslPolicyErrors == SslPolicyErrors.None
                || (serverAddresses.Contains(subjectCn) && subjectCn.Equals(issuerCn)))
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
