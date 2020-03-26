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
using log4stash.ElasticClient;
using log4stash.ElasticClient.RestSharp;
using log4stash.ErrorHandling;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace log4stash
{
    public class WebElasticClient : AbstractWebElasticClient
    {
        private IRestClient RestClient
        {
            get { return _restClientByHost[GetServerUrl()]; }
        }

        private readonly IDictionary<string, IRestClient> _restClientByHost;
        private readonly IRequestFactory _requestFactory;
        private readonly IExternalEventWriter _eventWriter;

        public WebElasticClient(IServerDataCollection servers, int timeout)
            : this(servers, timeout, false, false, new AuthenticationMethodChooser())
        {
        }

        public WebElasticClient(IServerDataCollection servers,
            int timeout,
            bool ssl,
            bool allowSelfSignedServerCert,
            IAuthenticator authenticationMethod)
            : this(servers, timeout, ssl, allowSelfSignedServerCert, authenticationMethod, new RestSharpClientFactory(),
                new RequestFactory(), new LogLogEventWriter())
        {
        }

        public WebElasticClient(IServerDataCollection servers, int timeout,
            bool ssl, bool allowSelfSignedServerCert, IAuthenticator authenticationMethod,
            IRestClientFactory restClientFactory, IRequestFactory requestFactory,
            IExternalEventWriter eventWriter)
            : base(servers, timeout, ssl, allowSelfSignedServerCert, authenticationMethod)
        {
            _requestFactory = requestFactory;
            _eventWriter = eventWriter;
            if (Ssl && AllowSelfSignedServerCert)
            {
                ServicePointManager.ServerCertificateValidationCallback += AcceptSelfSignedServerCertCallback;
            }

            _restClientByHost = servers.ToDictionary(GetServerUrl,
                serverData => restClientFactory.Create(GetServerUrl(serverData), timeout, authenticationMethod));
        }

        public override void PutTemplateRaw(string templateName, string rawBody)
        {
            var request = _requestFactory.CreatePutTemplateRequest(templateName, rawBody);
            RestClient.ExecuteAsync(request, response => { });
        }

        public override void IndexBulk(IEnumerable<InnerBulkOperation> bulk)
        {
            var request = _requestFactory.PrepareRequest(bulk);
            SafeSendRequest(request);
        }

        public override void IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk)
        {
            var request = _requestFactory.PrepareRequest(bulk);
            Task.Run(() => SafeSendRequestAsync(request));
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
                _eventWriter.Error(GetType(), "Invalid request to ElasticSearch", ex);
                return;
            }

            try
            {
                CheckResponse(response);
            }
            catch (Exception ex)
            {
                _eventWriter.Error(GetType(), "Got error while reading response from ElasticSearch", ex);
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
                _eventWriter.Error(GetType(), "Invalid request to ElasticSearch", ex);
                return;
            }

            try
            {
                CheckResponse(response);
            }
            catch (Exception ex)
            {
                _eventWriter.Error(GetType(), "Got error while reading response from ElasticSearch", ex);
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
                string.Format("Some error occurred while sending request to ElasticSearch.{0}{1}",
                    Environment.NewLine, errString));
        }

        public override void Dispose()
        {
            ServicePointManager.ServerCertificateValidationCallback -= AcceptSelfSignedServerCertCallback;
        }
    }
}