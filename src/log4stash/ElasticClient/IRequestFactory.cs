using System.Collections.Generic;

namespace log4stash.ElasticClient
{
    public interface IRequestFactory
    {
        RequestDetails PrepareRequest(IEnumerable<InnerBulkOperation> bulk);
    }
}