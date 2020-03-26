using System;

namespace log4stash.ErrorHandling
{
    public interface IExternalEventWriter
    {
        void Error(Type type, string message);
        void Error(Type type, string message, Exception ex);
        void Warn(Type type, string message);
        void Warn(Type type, string message, Exception ex);
    }
}