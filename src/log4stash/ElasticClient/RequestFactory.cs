using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RestSharp;

namespace log4stash.ElasticClient
{
    public class RequestFactory : IRequestFactory
    {
        public RequestDetails PrepareRequest(IEnumerable<InnerBulkOperation> bulk)
        {
            var requestString = PrepareBulk(bulk);
            var restRequest = new RestRequest("_bulk", Method.POST);
            restRequest.AddParameter("application/json", requestString, ParameterType.RequestBody);

            return new RequestDetails(restRequest, requestString);
        }

        public IRestRequest CreatePutTemplateRequest(string templateName, string rawBody)
        {
            var url = string.Concat("_template/", templateName);
            var restRequest = new RestRequest(url, Method.PUT) { RequestFormat = DataFormat.Json };
            restRequest.AddParameter("application/json", rawBody, ParameterType.RequestBody);
            return restRequest;
        }

        private static string PrepareBulk(IEnumerable<InnerBulkOperation> bulk)
        {
            var sb = new StringBuilder();
            foreach (var operation in bulk)
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
            var json = JsonConvert.SerializeObject(operation.Document);
            sb.Append(json);
            sb.Append("\n");
        }


    }
}
