using log4net.Core;
using log4net.Util;
using log4stash.FileAccess;

namespace log4stash
{
    public class TemplateInfo : IOptionHandler
    {
        private readonly IFileAccessor _fileAccessor;

        public string Name { get; set; }
        public string FileName { get; set; }
        public bool IsValid { get; private set; }

        public TemplateInfo()
        {
            IsValid = false;
            _fileAccessor = new BasicFileAccessor();
        }

        public TemplateInfo(IFileAccessor fileAccessor)
        {
            IsValid = false;
            _fileAccessor = fileAccessor;
        }

        public void ActivateOptions()
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(FileName))
            {
                LogLog.Error(GetType(), "Template name or fileName is empty!");
                return;
            }

            if (!_fileAccessor.Exists(FileName))
            {
                LogLog.Error(GetType(), string.Format("Could not find template file: {0}", FileName));
                return;
            }

            IsValid = true;
        }
    }
}