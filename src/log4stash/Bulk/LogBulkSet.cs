using System.Collections.Generic;
using System.Linq;
using log4stash.Configuration;
using log4stash.SmartFormatters;

namespace log4stash.Bulk
{
    public class LogBulkSet : ILogBulkSet
    {
        private List<List<InnerBulkOperation>> _allBulks = new List<List<InnerBulkOperation>>();
        private List<InnerBulkOperation> _currentBulk = new List<InnerBulkOperation>();
        private object _lock = new object();

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _currentBulk.Count;
                }
            }
        }

        public int TotalCount
        {
            get
            {
                lock (_lock)
                {
                    return _allBulks.Sum(b => b.Count);
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

            lock (_lock)
            {
                _currentBulk.Add(operation);
            }
        }

        public List<InnerBulkOperation> ResetBulk()
        {
            List<InnerBulkOperation> result;
            lock (_lock)
            {
                result = _currentBulk;
                _currentBulk = new List<InnerBulkOperation>();
                _allBulks.Add(_currentBulk);
            }

            return result;
        }

        public void CommitBulk(List<InnerBulkOperation> bulkToSend)
        {
            _allBulks.Remove(_currentBulk);
        }
    }
}