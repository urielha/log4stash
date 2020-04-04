using System.Collections.Generic;
using RestSharp;

namespace log4stash.ElasticClient
{
    public interface IRequestFactory
    {
        IRestRequest PrepareRequest(IEnumerable<InnerBulkOperation> bulk);
        IRestRequest CreatePutTemplateRequest(string templateName, string rawBody);
    }
}