using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace log4stash.Extensions
{
    public static class DictionaryExtensions
    {
        public static void ApplyFilter(this Dictionary<string, object> logEvent, IElasticAppenderFilter filter)
        {
            filter.PrepareEvent(logEvent);
        }
    }
}
