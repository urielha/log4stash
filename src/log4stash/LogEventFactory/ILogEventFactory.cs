using System.Collections.Generic;
using log4net.Core;

namespace log4stash.LogEventFactory
{
    public interface ILogEventFactoryParams
    {
        FixFlags FixedFields { get; set; }
        bool SerializeObjects { get; set; }
    }

    /// <summary>
    /// Represents class which can create LogEvent <see cref="Dictionary{TKey,TValue}"/>
    /// out of given <see cref="LoggingEvent"/>
    /// </summary>
    public interface ILogEventFactory
    {
        /// <summary>
        /// Configure the Factory before use.
        /// </summary>
        /// <param name="factoryParams">ILogEventFactoryParams</param>
        void Configure(ILogEventFactoryParams factoryParams);

        /// <summary>
        /// Create the log event out of given <paramref name="loggingEvent"/>
        /// </summary>
        /// <param name="loggingEvent">log4net <see cref="LoggingEvent"/> input</param>
        /// <returns>LogEvent Dictionary</returns>
        Dictionary<string, object> CreateLogEvent(LoggingEvent loggingEvent);
    }
}