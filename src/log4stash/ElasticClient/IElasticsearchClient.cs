using System;
using System.Collections.Generic;
using log4stash.Authentication;

namespace log4stash
{
    public interface IElasticsearchClient : IDisposable
    {
        string Server { get; }
        int Port { get; }
        bool Ssl { get; }
        bool AllowSelfSignedServerCert { get; }
        AuthenticationMethodChooser AuthenticationMethod { get; set; }
        void PutTemplateRaw(string templateName, string rawBody);
        void IndexBulk(IEnumerable<InnerBulkOperation> bulk);
        IAsyncResult IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk);
    }
}