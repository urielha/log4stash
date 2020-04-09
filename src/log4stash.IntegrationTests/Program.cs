using System;

namespace log4stash.Tests
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
