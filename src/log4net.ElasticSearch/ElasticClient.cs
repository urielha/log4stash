using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using log4net.ElasticSearch.InnerExceptions;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace log4net.ElasticSearch
{
    public interface IElasticClientProxy
    {
        string Server { get; }
        int Port { get; }
        void PutTemplateRaw(string templateName, string rawBody);
        void IndexBulk(IEnumerable<IInnerBulkOperation> bulk);
        Task IndexBulkAsync(IEnumerable<IInnerBulkOperation> bulk);
    }

    public interface IInnerBulkOperation
    {
        string IndexName { get; }
        string IndexType { get; }
        object Document { get; }
    }

    public class InnerBulkOperation : IInnerBulkOperation
    {
        public string IndexName { get; set; }
        public string IndexType { get; set; }
        public object Document { get; set; }
    }

    class NestElasticClient : IElasticClientProxy
    {
        private readonly ElasticClient _client;

        public string Server { get; private set; }
        public int Port { get; private set; }

        public NestElasticClient(string server, int port, int maxAsyncConnections)
        {
            Server = server;
            Port = port;
            var connectionSettings = new ConnectionSettings(new Uri(string.Format("http://{0}:{1}", Server, Port)));
            connectionSettings.SetMaximumAsyncConnections(maxAsyncConnections);
            _client = new ElasticClient(connectionSettings);
        }

        public void PutTemplateRaw(string templateName, string rawBody)
        {
            var res = _client.Raw.IndicesPutTemplateForAll(templateName, rawBody);
            if (!res.Success)
            {
                throw new ErrorSettingTemplateException(res);
            }
        }

        public void IndexBulk(IEnumerable<IInnerBulkOperation> bulk) 
        {
            var bulkRequest = PrepareBulk(bulk);
            _client.Bulk(bulkRequest);
        }

        public Task IndexBulkAsync(IEnumerable<IInnerBulkOperation> bulk) 
        {
            var bulkRequest = PrepareBulk(bulk);
            return _client.BulkAsync(bulkRequest);
        }

        private static BulkRequest PrepareBulk(IEnumerable<IInnerBulkOperation> bulk) 
        {
            var bulkRequest = new BulkRequest();
            bulkRequest.Operations = new List<IBulkOperation>();

            foreach (var operation in bulk)
            {
                var nestOperation = new BulkIndexOperation<object>(operation.Document)
                {
                    Index = operation.IndexName,
                    Type = operation.IndexType
                };

                bulkRequest.Operations.Add(nestOperation);
            }
            return bulkRequest;
        }
    }

    public class WebElasticClient : IElasticClientProxy
    {
        public string Server { get; private set; }
        public int Port { get; private set; }

        private readonly string _url;

        public WebElasticClient(string server, int port)
        {
            Server = server;
            Port = port;
            _url = string.Format("http://{0}:{1}/", Server, Port);
            ServicePointManager.Expect100Continue = false;
        }

        public void PutTemplateRaw(string templateName, string rawBody)
        {
            var webRequest = WebRequest.Create(string.Concat(_url, "_template/", templateName));
            webRequest.ContentType = "text/json";
            webRequest.Method = "PUT";
            SendRequest(webRequest, rawBody);
        }

        public void IndexBulk(IEnumerable<IInnerBulkOperation> bulk)
        {
            var requestString = PrepareBulk(bulk);

            var webRequest = WebRequest.Create(string.Concat(_url, "_bulk"));
            webRequest.ContentType = "text/plain";
            webRequest.Method = "POST";

            SendRequest(webRequest, requestString);
        }

        public Task IndexBulkAsync(IEnumerable<IInnerBulkOperation> bulk)
        {
            throw new NotImplementedException();
        }

        private static string PrepareBulk(IEnumerable<IInnerBulkOperation> bulk)
        {
            var sb = new StringBuilder();
            foreach (var operation in bulk)
            {
                sb.AppendFormat(
                    @"{{ ""index"" : {{ ""_index"" : ""{0}"", ""_type"" : ""{1}""}} }}",
                    operation.IndexName, operation.IndexType);
                sb.Append("\n");

                //string json = new JavaScriptSerializer().Serialize(logEvent);
                JObject jo = operation.Document as JObject ?? JObject.FromObject(operation.Document);

                sb.Append(jo.ToString(Formatting.None));

                sb.Append("\n");
            }
            return sb.ToString();
        }

        private static void SendRequest(WebRequest webRequest, string requestString)
        {
            using (var stream = new StreamWriter(webRequest.GetRequestStream()))
            {
                stream.Write(requestString);
            }

            using (var httpResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                if (httpResponse.StatusCode != HttpStatusCode.OK)
                {
                    var buff = new byte[httpResponse.ContentLength];
                    using (var response = httpResponse.GetResponseStream())
                    {
                        if (response != null)
                        {
                            response.Read(buff, 0, (int)httpResponse.ContentLength);
                        }
                    }

                    throw new InvalidOperationException(
                        string.Format("Some error occurred while sending request to Elasticsearch.{0}{1}",
                            Environment.NewLine, Encoding.UTF8.GetString(buff)));
                }
            }
        }
    }
}
