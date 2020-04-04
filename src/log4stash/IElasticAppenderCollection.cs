using System.Collections.Generic;
using log4stash.Filters;

namespace log4stash
{
    public interface IElasticAppenderCollection : IElasticAppenderFilter
    {
        void PrepareConfiguration(IElasticsearchClient client);
        void PrepareEvent(Dictionary<string, object> logEvent);
        void AddFilter(IElasticAppenderFilter filter);
        void AddAdd(AddValueFilter filter);
        void AddRemove(RemoveKeyFilter filter);
        void AddRename(RenameKeyFilter filter);
        void AddKv(KvFilter filter);
        void AddGrok(GrokFilter filter);
        void AddConvertToArray(ConvertToArrayFilter filter);
        void AddJson(JsonFilter filter);
        void AddXml(XmlFilter filter);
        void AddConvert(ConvertFilter filter);
    }
}