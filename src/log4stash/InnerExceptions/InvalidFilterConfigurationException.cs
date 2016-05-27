using System;

namespace log4stash.InnerExceptions
{
    public class InvalidFilterConfigurationException : Exception
    {
        public InvalidFilterConfigurationException()
        {
        }

        public InvalidFilterConfigurationException(string message)
            : base(message)
        {
        }
    }
}
