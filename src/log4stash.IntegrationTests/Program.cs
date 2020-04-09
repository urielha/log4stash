using System;

namespace log4stash.IntegrationTests
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
