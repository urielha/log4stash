using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using log4net.Util;
using log4stash.Authentication;
using log4stash.Configuration;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace log4stash
{

    public class WebElasticClient : AbstractWebElasticClient
    {
        private class RequestDetails
        {
            public RequestDetails(RestRequest restRequest, string content)
            {
                RestRequest = restRequest;
                Content = content;
            }

            public RestRequest RestRequest { get; private set; }
            public string Content { get; private set; }
        }

        public IRestClient RestClient
        {
            get { return _restClientByHost[GetServerUrl()]; }
        }

        private readonly IDictionary<string, RestClient> _restClientByHost;

        public WebElasticClient(ServerDataCollection servers, int timeout)
            : this(servers, timeout, false, false, new AuthenticationMethodChooser())
        {
        }

        public WebElasticClient(IServerDataCollection servers,
                                int timeout,
                                bool ssl,
                                bool allowSelfSignedServerCert,
                                IAuthenticator authenticationMethod)
            : base(servers, timeout, ssl, allowSelfSignedServerCert, authenticationMethod)
        {
            if (Ssl && AllowSelfSignedServerCert)
            {
                ServicePointManager.ServerCertificateValidationCallback += AcceptSelfSignedServerCertCallback;
            }

            _restClientByHost = servers.ToDictionary(GetServerUrl,
                serverData => new RestClient(GetServerUrl(serverData))
                {
                    Timeout = timeout,
                    Authenticator = authenticationMethod
                });
        }

        public override void PutTemplateRaw(string templateName, string rawBody)
        {
            var url = string.Concat("_template/", templateName);
            var restRequest = new RestRequest(url, Method.PUT) {RequestFormat = DataFormat.Json};
            restRequest.AddParameter("application/json", rawBody, ParameterType.RequestBody);
            RestClient.ExecuteAsync(restRequest, response => { });
        }

        public override void IndexBulk(IEnumerable<InnerBulkOperation> bulk)
        {
            var request = PrepareRequest(bulk);
            SafeSendRequest(request);
        }

        public override void IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk)
        {
            var request = PrepareRequest(bulk);

            SafeSendRequestAsync(request);
        }


        private RequestDetails PrepareRequest(IEnumerable<InnerBulkOperation> bulk)
        {
            var requestString = PrepareBulk(bulk);
            var restRequest = new RestRequest("_bulk", Method.POST);
            restRequest.AddParameter("application/json", requestString, ParameterType.RequestBody);

            return new RequestDetails(restRequest, requestString);
        }

        private static string PrepareBulk(IEnumerable<InnerBulkOperation> bulk)
        {
            var sb = new StringBuilder();
            foreach (InnerBulkOperation operation in bulk)
            {
                AddOperationMetadata(operation, sb);
                AddOperationDocument(operation, sb);
            }
            return sb.ToString();
        }

        private static void AddOperationMetadata(InnerBulkOperation operation, StringBuilder sb)
        {
            var indexParams = new Dictionary<string, string>(operation.IndexOperationParams)
            {
                { "_index", operation.IndexName },
                { "_type", operation.IndexType },
            };
            var paramStrings = indexParams.Where(kv => kv.Value != null)
                .Select(kv => string.Format(@"""{0}"" : ""{1}""", kv.Key, kv.Value));
            var documentMetadata = string.Join(",", paramStrings.ToArray());
            sb.AppendFormat(@"{{ ""index"" : {{ {0} }} }}", documentMetadata);
            sb.Append("\n");
        }

        private static void AddOperationDocument(InnerBulkOperation operation, StringBuilder sb)
        {
            string json = JsonConvert.SerializeObject(operation.Document);
            sb.Append(json);
            sb.Append("\n");
        }

        private void SafeSendRequest(RequestDetails request)
        {
            IRestResponse response;
            try
            {
                response = RestClient.Execute(request.RestRequest);
            }
            catch (Exception ex)
            {
                LogLog.Error(GetType(), "Invalid request to ElasticSearch", ex);
                return;
            }

            try
            {
                CheckResponse(response);
            }
            catch (Exception ex)
            {
                LogLog.Error(GetType(), "Got error while reading response from ElasticSearch", ex);
            }
        }

        private async Task SafeSendRequestAsync(RequestDetails request)
        {
            IRestResponse response;
            try
            {
                response = await RestClient.ExecuteTaskAsync(request.RestRequest);
            }
            catch (Exception ex)
            {
                LogLog.Error(GetType(), "Invalid request to ElasticSearch", ex);
                return;
            }

            try
            {
                CheckResponse(response);
            }
            catch (Exception ex)
            {
                LogLog.Error(GetType(), "Got error while reading response from ElasticSearch", ex);
            }

        }

        private bool AcceptSelfSignedServerCertCallback(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            var certificate2 = certificate as X509Certificate2;
            if (certificate2 == null)
                return false;

            string subjectCn = certificate2.GetNameInfo(X509NameType.DnsName, false);
            string issuerCn = certificate2.GetNameInfo(X509NameType.DnsName, true);
            var serverAddresses = Servers.Select(s => s.Address);
            if (sslPolicyErrors == SslPolicyErrors.None
                || (serverAddresses.Contains(subjectCn) && subjectCn != null && subjectCn.Equals(issuerCn)))
            {
                return true;
            }

            return false;
        }

        private static string GetResponseErrorIfAny(IRestResponse response)
        {
            if (response == null)
            {
                return "Got null response";
            }

            // Handle network transport or framework exception
            if (response.ErrorException != null)
            {
                return response.ErrorException.ToString();
            }

            // Handle request errors
            if (!response.StatusCode.HasFlag(HttpStatusCode.OK))
            {
                var err = new StringBuilder();
                err.AppendFormat("Got non ok status code: {0}.", response.StatusCode);
                err.AppendLine(response.Content);
                return err.ToString();
            }

            // Handle index error
            try
            {
                var jsonResponse = JsonConvert.DeserializeObject<PartialElasticResponse>(response.Content);
                if (jsonResponse != null && jsonResponse.Errors)
                {
                    return response.Content;
                }
            }
            catch (JsonReaderException)
            {
                return string.Format("Can't parse Elastic response: {0}", response.Content);
            }

            return null;
        }

        private static void CheckResponse(IRestResponse response)
        {
            var errString = GetResponseErrorIfAny(response);
            if (string.IsNullOrEmpty(errString))
            {
                return;
            }

            throw new InvalidOperationException(
                string.Format("Some error occurred while sending request to Elasticsearch.{0}{1}",
                    Environment.NewLine, errString));
            
        }

        public override void Dispose()
        {
            ServicePointManager.ServerCertificateValidationCallback -= AcceptSelfSignedServerCertCallback;
        }
    }
}
