using Nest;
using Newtonsoft.Json.Linq;

namespace log4net.ElasticSearch
{
    public interface IElasticAppenderFilter 
    {
        void PrepareConfiguration(IElasticClientProxy client);
        void PrepareEvent(JObject logEvent, IElasticClientProxy client);
    }
}