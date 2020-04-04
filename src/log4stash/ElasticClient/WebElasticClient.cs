using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using log4stash.Authentication;
using log4stash.Configuration;
using log4stash.ElasticClient;
using log4stash.ElasticClient.RestSharp;
using log4stash.ErrorHandling;
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
        private readonly IResponseValidator _responseValidator;
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
                new RequestFactory(), new ResponseValidator(), new LogLogEventWriter())
        {
        }

        public WebElasticClient(IServerDataCollection servers, int timeout,
            bool ssl, bool allowSelfSignedServerCert, IAuthenticator authenticationMethod,
            IRestClientFactory restClientFactory, IRequestFactory requestFactory,
            IResponseValidator responseValidator, IExternalEventWriter eventWriter)
            : base(servers, timeout, ssl, allowSelfSignedServerCert, authenticationMethod)
        {
            _requestFactory = requestFactory;
            _eventWriter = eventWriter;
            _responseValidator = responseValidator;
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

        public override async Task IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk)
        {
            var request = _requestFactory.PrepareRequest(bulk);
            await SafeSendRequestAsync(request);
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
                ReportRequestError(ex);
                return;
            }

            try
            {
                _responseValidator.ValidateResponse(response);
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
                ReportRequestError(ex);
                return;
            }

            try
            {
                _responseValidator.ValidateResponse(response);
            }
            catch (Exception ex)
            {
                _eventWriter.Error(GetType(), "Got error while reading response from ElasticSearch", ex);
            }
        }

        private void ReportRequestError(Exception ex)
        {
            _eventWriter.Error(GetType(), "Invalid request to ElasticSearch", ex);
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

        

        public override void Dispose()
        {
            ServicePointManager.ServerCertificateValidationCallback -= AcceptSelfSignedServerCertCallback;
        }
    }
}