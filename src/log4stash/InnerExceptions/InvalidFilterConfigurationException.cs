using System;

namespace log4net.ElasticSearch.InnerExceptions
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
