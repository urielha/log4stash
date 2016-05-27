namespace log4stash
{
    public class InnerBulkOperation 
    {
        public string IndexName { get; set; }
        public string IndexType { get; set; }
        public object Document { get; set; }
    }
}