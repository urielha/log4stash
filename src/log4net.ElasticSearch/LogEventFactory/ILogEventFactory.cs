using System.Collections.Generic;
using log4net.Core;

namespace log4net.ElasticSearch.LogEventFactory
{
    public interface ILogEventFactory
    {
        void Configure(ElasticSearchAppender appenderProperties);
        Dictionary<string, object> CreateLogEvent(LoggingEvent sourceLoggingEvent);
    }
}