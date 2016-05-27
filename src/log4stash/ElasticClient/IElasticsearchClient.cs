using System;
using System.Collections.Generic;

namespace log4stash
{
    public interface IElasticsearchClient : IDisposable
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
}