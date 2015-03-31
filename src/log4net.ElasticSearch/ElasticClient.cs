using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web.Script.Serialization;

namespace log4net.ElasticSearch
{
    public interface IElasticsearchClient
    {
        string Server { get; }
        int Port { get; }
        bool Ssl { get; }
        bool AllowSelfSignedServerCert { get; }
        string BasicAuthUsername { get; }
        string BasicAuthPassword { get; }
        void PutTemplateRaw(string templateName, string rawBody);
        void IndexBulk(IEnumerable<InnerBulkOperation> bulk);
        IAsyncResult IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk);
    }
    
    public class InnerBulkOperation 
    {
        public string IndexName { get; set; }
        public string IndexType { get; set; }
        public object Document { get; set; }
    }

    public class WebElasticClient : IElasticsearchClient
    {
        public string Server { get; private set; }
        public int Port { get; private set; }
        public bool Ssl { get; private set; }
        public bool AllowSelfSignedServerCert { get; private set; }
        public string BasicAuthUsername { get; private set; }
        public string BasicAuthPassword { get; private set; }

        private string _url { 
            get 
            {
                return string.Format("{0}://{1}:{2}/", Ssl ? "https" : "http", Server, Port);
            }
        }

        public WebElasticClient(string server, int port)
        {
            Server = server;
            Port = port;
            ServicePointManager.Expect100Continue = false;
        }

        public WebElasticClient(string server, int port,
                                bool ssl, bool allowSelfSignedServerCert, 
                                string basicAuthUsername, string basicAuthPassword)
            : this(server, port)
        {
            Ssl = ssl;
            AllowSelfSignedServerCert = allowSelfSignedServerCert;
            BasicAuthPassword = basicAuthPassword;
            BasicAuthUsername = basicAuthUsername;

            if (true == Ssl && true == AllowSelfSignedServerCert)
            {
                AcceptSelfSignedServerCert(server);
            }
        }

        public void PutTemplateRaw(string templateName, string rawBody)
        {
            var webRequest = WebRequest.Create(string.Concat(_url, "_template/", templateName));
            webRequest.ContentType = "text/json";
            webRequest.Method = "PUT";
            SetBasicAuthHeader(webRequest, BasicAuthUsername, BasicAuthPassword);
            SendRequest(webRequest, rawBody);
            using (var httpResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                CheckResponse(httpResponse);
            }
        }

        public void IndexBulk(IEnumerable<InnerBulkOperation> bulk)
        {
            var webRequest = PrepareBulkAndSend(bulk);
            using (var httpResponse = (HttpWebResponse) webRequest.GetResponse())
            {
                CheckResponse(httpResponse);
            }
        }
        
        public IAsyncResult IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk)
        {
            var webRequest = PrepareBulkAndSend(bulk);
            return webRequest.BeginGetResponse(FinishGetResponse, webRequest);
        }

        private void FinishGetResponse(IAsyncResult result)
        {
            var webRequest = (WebRequest)result.AsyncState;
            using (var httpResponse = (HttpWebResponse)webRequest.EndGetResponse(result))
            {
                CheckResponse(httpResponse);
            }
        }

        private WebRequest PrepareBulkAndSend(IEnumerable<InnerBulkOperation> bulk)
        {
            var requestString = PrepareBulk(bulk);

            var webRequest = WebRequest.Create(string.Concat(_url, "_bulk"));
            webRequest.ContentType = "text/plain";
            webRequest.Method = "POST";
            webRequest.Timeout = 10000;
            SetBasicAuthHeader(webRequest, BasicAuthUsername, BasicAuthPassword);
            SendRequest(webRequest, requestString);
            return webRequest;
        }

        private static string PrepareBulk(IEnumerable<InnerBulkOperation> bulk)
        {
            var sb = new StringBuilder();
            foreach (var operation in bulk)
            {
                sb.AppendFormat(
                    @"{{ ""index"" : {{ ""_index"" : ""{0}"", ""_type"" : ""{1}""}} }}",
                    operation.IndexName, operation.IndexType);
                sb.Append("\n");

                string json = new JavaScriptSerializer().Serialize(operation.Document);
                sb.Append(json);

                sb.Append("\n");
            }
            return sb.ToString();
        }

        private static void SendRequest(WebRequest webRequest, string requestString)
        {
            using (var stream = new StreamWriter(webRequest.GetRequestStream()))
            {
                stream.Write(requestString);
            }
        }

        private static void SetBasicAuthHeader(WebRequest request, string username, string password)
        {
            if (false == string.IsNullOrEmpty(username)) /* IsNullOrWhiteSpace will be better, but .Net 3.5 doesn't support IsNullOrWhiteSpace but only IsNullOrEmpty */
            {
                string authInfo = string.Format("{0}:{1}", username, password);
                string encodedAuthInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes(authInfo));
                string credentials = string.Format("{0} {1}", "Basic", encodedAuthInfo);
                request.Headers[HttpRequestHeader.Authorization] = credentials;
            }
        }

        private static void AcceptSelfSignedServerCert(string server)
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                new System.Net.Security.RemoteCertificateValidationCallback(
                delegate(Object sender,
                          X509Certificate certificate,
                          X509Chain chain,
                          SslPolicyErrors sslPolicyErrors)
                {
                    string subjectCN = (certificate as X509Certificate2).GetNameInfo(X509NameType.DnsName, false);
                    string issuerCN = (certificate as X509Certificate2).GetNameInfo(X509NameType.DnsName, true);
                    if (sslPolicyErrors == SslPolicyErrors.None
                        || (server.Equals(subjectCN) && subjectCN.Equals(issuerCN)))
                    {
                        return true;
                    }
                    else
                        return false;
                });
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
    }
}
