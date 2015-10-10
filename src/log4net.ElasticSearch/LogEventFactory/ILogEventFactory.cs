using System.Collections.Generic;
using log4net.Core;

namespace log4net.ElasticSearch.LogEventFactory
{
    /// <summary>
    /// Represents class which can create LogEvent <see cref="Dictionary{TKey,TValue}"/>
    /// out of given <see cref="LoggingEvent"/>
    /// </summary>
    public interface ILogEventFactory
    {
        /// <summary>
        /// Configure the Factory before use.
        /// </summary>
        /// <param name="appenderProperties">ElasticSearchAppender</param>
        void Configure(ElasticSearchAppender appenderProperties);

        /// <summary>
        /// Create the log event out of given <paramref name="sourceLoggingEvent"/>
        /// </summary>
        /// <param name="sourceLoggingEvent">log4net <see cref="LoggingEvent"/> input</param>
        /// <returns>LogEvent Dictionary</returns>
        Dictionary<string, object> CreateLogEvent(LoggingEvent sourceLoggingEvent);
    }
}