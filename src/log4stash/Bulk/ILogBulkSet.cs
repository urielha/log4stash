using System.Collections.Generic;
using log4stash.Configuration;
using log4stash.SmartFormatters;

namespace log4stash.Bulk
{
    public interface ILogBulkSet
    {
        int Count { get; }

        void AddEventToBulk(Dictionary<string, object> logEvent, LogEventSmartFormatter indexNameFormat,
            LogEventSmartFormatter indexTypeFormat, IndexOperationParamsDictionary indexOperationParams);

        List<InnerBulkOperation> ResetBulk();
    }
}