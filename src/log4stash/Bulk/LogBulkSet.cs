using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4stash.Configuration;
using log4stash.SmartFormatters;

namespace log4stash.Bulk
{
    public class LogBulkSet : ILogBulkSet
    {
        private List<InnerBulkOperation> _bulk = new List<InnerBulkOperation>();

        public int Count
        {
            get
            {
                lock (_bulk)
                {
                    return _bulk.Count;
                }
            }
        }

        public void AddEventToBulk(Dictionary<string, object> logEvent, LogEventSmartFormatter indexNameFormat,
            LogEventSmartFormatter indexTypeFormat, IndexOperationParamsDictionary indexOperationParams)
        {
            var indexName = indexNameFormat.Format(logEvent).ToLower();
            var indexType = indexTypeFormat.Format(logEvent);
            var indexOperationParamValues = indexOperationParams.ToDictionary(logEvent);

            var operation = new InnerBulkOperation
            {
                Document = logEvent,
                IndexName = indexName,
                IndexType = indexType,
                IndexOperationParams = indexOperationParamValues
            };

            lock (_bulk)
            {
                _bulk.Add(operation);
            }
        }

        public List<InnerBulkOperation> ResetBulk()
        {
            var currentBulk = Interlocked.Exchange(ref _bulk, new List<InnerBulkOperation>());
            return currentBulk;
        }



    }
}
