using System.Diagnostics;
using System.IO;
using log4net.Core;
using log4net.Util;

namespace log4stash
{
    public class TemplateInfo : IOptionHandler
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public bool IsValid { get; private set; }

        public TemplateInfo()
        {
            IsValid = false;
        }

        public void ActivateOptions()
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(FileName))
            {
                LogLog.Error(GetType(), "Template name or fileName is empty!");
                return;
            }

            if (!File.Exists(FileName))
            {
                LogLog.Error(GetType(), string.Format("Could not find template file: {0}", FileName));
                return;
            }

            IsValid = true;
        }
    }
}