using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace log4stash.FileAccess
{
    public class BasicFileAccessor : IFileAccessor
    {
        public string ReadAllText(string fileName)
        {
            return File.ReadAllText(fileName);
        }

        public bool Exists(string fileName)
        {
            return File.Exists(fileName);
        }
    }
}
