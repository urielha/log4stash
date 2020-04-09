using log4net.Core;

namespace log4stash.LogEvent
{
    public interface ILogEventConverterFactory
    {
        ILogEventConverter Create(FixFlags fixedFields, bool serializeObjects);
    }
}