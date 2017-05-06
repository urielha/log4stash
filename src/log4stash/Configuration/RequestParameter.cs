namespace log4stash.Configuration
{
    public class RequestParameter
    {
        public string Key { get; set; }

        public string Value { get; set; }

        public RequestParameter()
        {
        }

        public RequestParameter(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
