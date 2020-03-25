using System;

namespace log4stash.LogEvent
{
    public interface IJsonSerializableExceptionFactory
    {
        JsonSerializableException Create(Exception exception);
    }
}