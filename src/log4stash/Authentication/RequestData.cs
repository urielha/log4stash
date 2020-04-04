using RestSharp;

namespace log4stash.Authentication
{
    public class RequestData
    {
        public IRestRequest RestRequest { get; set; }

        public string Url { get; set; }

        public string RequestString { get; set; }
    }
}
