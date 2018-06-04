using System;
using System.Collections.Generic;
using System.IO;
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
using RestSharp.Deserializers;

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

        public override bool IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk)
        {
            var request = PrepareRequest(bulk);

            return SafeSendRequestAsync(request).Result;
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

        private bool SafeSendRequest(RequestDetails request)
        {
            IRestResponse response;
            try
            {
                response = RestClient.Execute(request.RestRequest);
            }
            catch (Exception ex)
            {
                LogLog.Error(GetType(), "Invalid request to ElasticSearch", ex);
                return false;
            }

            try
            {
                CheckResponse(response);
                return true;
            }
            catch (Exception ex)
            {
                LogLog.Error(GetType(), "Got error while reading response from ElasticSearch", ex);
            }

            return false;
        }

        private async Task<bool> SafeSendRequestAsync(RequestDetails request)
        {
            IRestResponse response;
            try
            {
                response = await RestClient.ExecuteTaskAsync(request.RestRequest);
            }
            catch (Exception ex)
            {
                LogLog.Error(GetType(), "Invalid request to ElasticSearch", ex);
                return false;
            }

            try
            {
                CheckResponse(response);
                return true;
            }
            catch (Exception ex)
            {
                LogLog.Error(GetType(), "Got error while reading response from ElasticSearch", ex);
            }

            return false;
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

        private static void CheckResponse(IRestResponse response)
        {
            if (response == null)
            {
                return;
            }

            var stringResponse = response.Content;
            var jsonResponse = JsonConvert.DeserializeObject<PartialElasticResponse>(stringResponse);

            bool responseHasError = jsonResponse.Errors || response.StatusCode != HttpStatusCode.OK;
            if (responseHasError)
            {
                throw new InvalidOperationException(
                    string.Format("Some error occurred while sending request to Elasticsearch.{0}{1}",
                        Environment.NewLine, stringResponse));
            }
        }

        public override void Dispose()
        {
            ServicePointManager.ServerCertificateValidationCallback -= AcceptSelfSignedServerCertCallback;
        }
    }
}
