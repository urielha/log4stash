using System.Collections.Generic;
using RestSharp;

namespace log4stash.ElasticClient
{
    public interface IRequestFactory
    {
        RequestDetails PrepareRequest(IEnumerable<InnerBulkOperation> bulk);
        IRestRequest CreatePutTemplateRequest(string templateName, string rawBody);
    }
}