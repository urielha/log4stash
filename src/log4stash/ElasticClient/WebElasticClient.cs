using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net.Util;
using log4stash.Authentication;
using log4stash.Configuration;
using Newtonsoft.Json;
using RestSharp;

namespace log4stash
{

    public class WebElasticClient : AbstractWebElasticClient
    {
        private class RequestDetails
        {
            public RequestDetails(RestRequest restRequest)
            {
                RestRequest = restRequest;
            }

            public RestRequest RestRequest { get; private set; }
        }

        public IRestClient RestClient
        {
            get { return _restClientByHost[GetServerUrl()]; }
        }

        private readonly IDictionary<string, RestClient> _restClientByHost;

        private int _requests;
        public int MaxConcurrentRequests { get; private set; }

        public WebElasticClient(ServerDataCollection servers, int timeout, int maxConcurrentRequests)
            : this(servers, timeout, false, false, new AuthenticationMethodChooser(), maxConcurrentRequests)
        {
        }

        public WebElasticClient(ServerDataCollection servers,
                                int timeout,
                                bool ssl,
                                bool allowSelfSignedServerCert,
                                AuthenticationMethodChooser authenticationMethod,
                                int maxConcurrentRequests
            )
            : base(servers, timeout, ssl, allowSelfSignedServerCert, authenticationMethod)
        {
            _requests = 0;
            MaxConcurrentRequests = maxConcurrentRequests;

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
            var restRequest = new RestRequest(url, Method.PUT) { RequestFormat = DataFormat.Json };
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
            if (MaxConcurrentRequests > 0 &&
                Interlocked.Increment(ref _requests) > MaxConcurrentRequests)
            {
                LogLog.Error(GetType(),
                    string.Format("Number of concurrent requests ({0}) exceeded the limit ({1})! Bulk lost.",
                    _requests, MaxConcurrentRequests));
                Interlocked.Decrement(ref _requests);
                return;
            }

            var request = PrepareRequest(bulk);

            SafeSendRequestAsync(request);
        }


        private RequestDetails PrepareRequest(IEnumerable<InnerBulkOperation> bulk)
        {
            var requestString = PrepareBulk(bulk);
            var restRequest = new RestRequest("_bulk", Method.POST);
            restRequest.AddParameter("application/json", requestString, ParameterType.RequestBody);

            return new RequestDetails(restRequest);
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

        private async void SafeSendRequestAsync(RequestDetails request)
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
            finally
            {
                if (MaxConcurrentRequests > 0)
                {
                    Interlocked.Decrement(ref _requests);                    
                }
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

        private static void CheckResponse(IRestResponse response)
        {
            if (response == null)
            {
                return;
            }

            var stringResponse = response.Content;
            var jsonResponse = JsonConvert.DeserializeObject<PartialElasticResponse>(stringResponse);

            bool responseHasError = jsonResponse == null || jsonResponse.Errors || response.StatusCode != HttpStatusCode.OK;
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
