using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Core;

namespace log4stash.LogEvent
{
    public class BasicLogEventConverterFactory : ILogEventConverterFactory
    {
        public ILogEventConverter Create(FixFlags fixedFields, bool serializeObjects)
        {
            return new BasicLogEventConverter(fixedFields, serializeObjects);
        }
    }
}
