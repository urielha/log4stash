using Newtonsoft.Json.Linq;

namespace log4net.ElasticSearch
{
    public interface IElasticAppenderFilter 
    {
        void PrepareConfiguration(IElasticsearchClient client);
        void PrepareEvent(JObject logEvent);
    }
}