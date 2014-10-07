using System;

namespace log4net.ElasticSearch.Tests
{
    public static class Program
    {
        public static void Main()
        {
            ElasticsearchAppenderTests.Performance();
            Console.ReadLine();
        }
    }
}
