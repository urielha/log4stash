using System.Collections.Generic;
using log4stash.Configuration;

namespace log4stash
{
    public class InnerBulkOperation 
    {
        public string IndexName { get; set; }
        public string IndexType { get; set; }
        public object Document { get; set; }
        public Dictionary<string, object> RequestParameters { get; set; }

        public InnerBulkOperation()
        {
        }
    }
}