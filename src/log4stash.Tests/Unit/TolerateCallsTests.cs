using System;
using System.Threading;
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
        private int _times;

        public int Times
        {
            get { return _times; }
            set { _times = value; }
        }

        public void Inc()
        {
            Interlocked.Increment(ref _times);
        }
    }

    [TestFixture]
    public class TolerateCallsTests
    {
        private const int TimeSec = 1;
        private void Checker(TolerateCallsBase tolerator, bool shouldTolerate)
        {
            var t = GetType();
            var mock = new FuncMock();

            Parallel.For(0, 100, i => tolerator.Call(mock.Inc, t, 0));
            Assert.AreEqual(shouldTolerate ? 1 : 100, mock.Times);

            Parallel.For(0, 100, i => tolerator.Call(mock.Inc, t, 1));
            Assert.AreEqual(shouldTolerate ? 2 : 200, mock.Times);

            Thread.Sleep(TimeSpan.FromSeconds(TimeSec));

            Parallel.For(0, 100, i => tolerator.Call(mock.Inc, t, 0));
            Parallel.For(0, 100, i => tolerator.Call(mock.Inc, t, 1));

            Assert.AreEqual(shouldTolerate ? 4 : 400, mock.Times);
        }

        [Test]
        public void CheckTolerance()
        {
            Checker(new TolerateCalls(TimeSpan.FromSeconds(TimeSec)), true);
        }

        [Test]
        public void Factory()
        {
            var factory = new TolerateCallsFactory();
            Checker(factory.Create(0), false);
            Checker(factory.Create(-1), false);
            Checker(factory.Create(TimeSec), true);
        }
    }
}