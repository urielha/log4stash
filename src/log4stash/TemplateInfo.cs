using log4net.Core;
using log4stash.ErrorHandling;
using log4stash.FileAccess;

namespace log4stash
{
    public class TemplateInfo : IOptionHandler
    {
        private readonly IFileAccessor _fileAccessor;
        private readonly IExternalEventWriter _eventWriter;

        public string Name { get; set; }
        public string FileName { get; set; }
        public bool IsValid { get; private set; }

        public TemplateInfo() : this(new BasicFileAccessor(), new LogLogEventWriter())
        {
        }

        public TemplateInfo(IFileAccessor fileAccessor, IExternalEventWriter eventWriter)
        {
            IsValid = false;
            _fileAccessor = fileAccessor;
            _eventWriter = eventWriter;
        }

        public void ActivateOptions()
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(FileName))
            {
                _eventWriter.Error(GetType(), "Template name or fileName is empty!");
                return;
            }

            if (!_fileAccessor.Exists(FileName))
            {
                _eventWriter.Error(GetType(), string.Format("Could not find template file: {0}", FileName));
                return;
            }

            IsValid = true;
        }
    }
}