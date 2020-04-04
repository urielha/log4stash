using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4stash.Configuration;
using RestSharp.Authenticators;

namespace log4stash
{
    public interface IElasticsearchClient : IDisposable
    {
        IServerDataCollection Servers { get; }
        bool Ssl { get; }
        bool AllowSelfSignedServerCert { get; }
        IAuthenticator AuthenticationMethod { get; set; }
        void PutTemplateRaw(string templateName, string rawBody);
        Task PutTemplateRawAsync(string templateName, string rawBody);
        void IndexBulk(IEnumerable<InnerBulkOperation> bulk);
        Task IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk);
    }
}