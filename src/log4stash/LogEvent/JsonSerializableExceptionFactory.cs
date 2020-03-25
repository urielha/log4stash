using System;

namespace log4stash.LogEvent
{
    public class BasicJsonSerializableExceptionFactory : IJsonSerializableExceptionFactory
    {
        public JsonSerializableException Create(Exception exception)
        {
            if (exception == null)
                return null;

            var serializable = new JsonSerializableException
            {
#if NET45
                HResult = exception.HResult,
#endif
                Type = exception.GetType().FullName,
                Message = exception.Message,
                HelpLink = exception.HelpLink,
                Source = exception.Source,
                StackTrace = exception.StackTrace,
                TargetSite = exception.TargetSite != null ? exception.TargetSite.ToString() : null,
                Data = exception.Data
            };

            if (exception.InnerException != null)
            {
                serializable.InnerException = Create(exception.InnerException);
            }
            return serializable;
        }
    }
}
