using System;
using System.Collections.Generic;
using System.Threading;
using log4stash.Authentication;
using log4stash.Configuration;

namespace log4stash
{
    // TODO: need more work here
    public class CompositeElasticClient : IElasticsearchClient
    {
        private int _current;
        private readonly List<IElasticsearchClient> _clients;
        
        public ServerDataCollection Servers { get; private set; }
        public bool Ssl { get; private set; }
        public bool AllowSelfSignedServerCert { get; private set; }
        public AuthenticationMethodChooser AuthenticationMethod { get; set; }

        public CompositeElasticClient(int clients, Func<IElasticsearchClient> factory)
        {
            _current = 0;
            _clients = new List<IElasticsearchClient>(clients);
            for (int i = 0; i < clients; i++)
            {
                _clients.Add(factory());
            }
        }

        public void Dispose()
        {
            foreach (var client in _clients)
            {
                client.Dispose();
            }
        }

        public void PutTemplateRaw(string templateName, string rawBody)
        {
            int i = Interlocked.Increment(ref _current);
            _clients[i].PutTemplateRaw(templateName, rawBody);
        }

        public void IndexBulk(IEnumerable<InnerBulkOperation> bulk)
        {
            int i = Interlocked.Increment(ref _current);
            _clients[i].IndexBulk(bulk);
        }

        public IAsyncResult IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk)
        {
            int i = Interlocked.Increment(ref _current);
            return _clients[i].IndexBulkAsync(bulk);
        }
    }
}