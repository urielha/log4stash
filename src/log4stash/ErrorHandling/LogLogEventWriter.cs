using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Util;

namespace log4stash.ErrorHandling
{
    public class LogLogEventWriter : IExternalEventWriter
    {
        public void Error(Type type, string message)
        {
            LogLog.Error(type, message);
        }

        public void Error(Type type, string message, Exception ex)
        {
            LogLog.Error(type, message, ex);
        }

        public void Warn(Type type, string message)
        {
            LogLog.Warn(type, message);
        }

        public void Warn(Type type, string message, Exception ex)
        {
            LogLog.Warn(type, message, ex);
        }
    }
}
