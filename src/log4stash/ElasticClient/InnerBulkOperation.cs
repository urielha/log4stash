namespace log4stash
{
    public class InnerBulkOperation 
    {
        public string IndexName { get; set; }
        public string IndexType { get; set; }
        public object Document { get; set; }
        public object DocumentId { get; set; }

        public InnerBulkOperation()
        {
            DocumentId = null;
        }
    }
}