using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace log4net.ElasticSearch
{
    public interface IElasticsearchClient
    {
        string Server { get; }
        int Port { get; }
        void PutTemplateRaw(string templateName, string rawBody);
        void IndexBulk(IEnumerable<InnerBulkOperation> bulk);
        Task IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk);
    }
    
    public class InnerBulkOperation 
    {
        public string IndexName { get; set; }
        public string IndexType { get; set; }
        public object Document { get; set; }
    }

    public class WebElasticClient : IElasticsearchClient
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

        public void IndexBulk(IEnumerable<InnerBulkOperation> bulk)
        {
            var requestString = PrepareBulk(bulk);

            var webRequest = WebRequest.Create(string.Concat(_url, "_bulk"));
            webRequest.ContentType = "text/plain";
            webRequest.Method = "POST";

            SendRequest(webRequest, requestString);
        }

        private static string PrepareBulk(IEnumerable<InnerBulkOperation> bulk)
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

        public Task IndexBulkAsync(IEnumerable<InnerBulkOperation> bulk)
        {
            throw new NotImplementedException();
        }
    }
}
