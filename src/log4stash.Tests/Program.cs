using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using log4net;
using log4stash.Tests.Integration;

namespace log4stash.Tests
{
    public static class Program
    {
        public static void Main()
        {
            LogManager.GetRepository().ResetConfiguration();
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo("other.config"));
            ILog mLog = LogManager.GetLogger("MyLog");
            mLog.Info("bla");


            var strBldr = new StringBuilder();
            for (int i = 0; i < 50; i++) strBldr.Append("hello world my friend. ");
            var temp = strBldr.ToString();
            for (int i = 0; i < 500; i++) strBldr.AppendLine(temp);

            Console.WriteLine(strBldr.Length/1024.0/1024.0);
            Console.WriteLine("Press any key to start");
            Console.ReadLine();

            for (int i = 0; i < 8000; i++)
            {
                Console.Write(".");
                mLog.Info(strBldr.ToString());
            }
            Console.WriteLine("Press any key to end");
            Console.ReadLine();
            return;

            ElasticsearchAppenderTests.Performance();
            Console.ReadLine();
        }
    }
}
