using RestSharp;

namespace log4stash.ElasticClient
{
    public class RequestDetails
    {
        public RequestDetails(RestRequest restRequest, string content)
        {
            RestRequest = restRequest;
            Content = content;
        }

        public RestRequest RestRequest { get; private set; }
        public string Content { get; private set; }
    }
}