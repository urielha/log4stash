using System.Collections.Generic;

namespace log4stash
{
    public interface IElasticAppenderFilter 
    {
        void PrepareConfiguration(IElasticsearchClient client);
        void PrepareEvent(Dictionary<string, object> logEvent);
    }
}