using System;
using System.Threading.Tasks;
using log4stash.Authentication;
using log4stash.Configuration;
using log4stash.Extensions;
using Nest;
using NUnit.Framework;

namespace log4stash.Tests.Unit
{
    class FuncMock
    {
        public int Times { get; set; }

        public void Inc()
        {
            Times++;
        }
    }

    [TestFixture]
    public class ToleratedLogLogTests
    {
        [Test]
        public void CheckTolerance()
        {
            var t = GetType();
            var mock = new FuncMock();
            var tolerator = new ToleratedCalls(TimeSpan.FromSeconds(2));

            Parallel.For(0, 100, i => tolerator.Call(mock.Inc, t, 0));
            Assert.AreEqual(1, mock.Times, "Should be called only once");

            Parallel.For(0, 100, i => tolerator.Call(mock.Inc, t, 1));
            Assert.AreEqual(2, mock.Times, "Two types of calls");

            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));

            Parallel.For(0, 100, i => tolerator.Call(mock.Inc, t, 0));
            Parallel.For(0, 100, i => tolerator.Call(mock.Inc, t, 1));

            Assert.AreEqual(4, mock.Times, "After time passed - log again");
        }
    }
}