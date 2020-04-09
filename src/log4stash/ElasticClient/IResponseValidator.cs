using RestSharp;

namespace log4stash.ElasticClient
{
    public interface IResponseValidator
    {
        void ValidateResponse(IRestResponse response);
    }
}