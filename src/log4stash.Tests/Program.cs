using System;
using log4stash.Tests.Integration;

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
