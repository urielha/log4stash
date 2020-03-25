using System.Collections.Generic;
using log4net.Core;

namespace log4stash.LogEvent
{
    /// <summary>
    /// Represents class which can create LogEvent <see cref="Dictionary{TKey,TValue}"/>
    /// out of given <see cref="LoggingEvent"/>
    /// </summary>
    public interface ILogEventConverter
    {

        /// <summary>
        /// Create the log event out of given <paramref name="loggingEvent"/>
        /// </summary>
        /// <param name="loggingEvent">log4net <see cref="LoggingEvent"/> input</param>
        /// <returns>LogEvent Dictionary</returns>
        Dictionary<string, object> ConvertLogEventToDictionary(LoggingEvent loggingEvent);
    }
}