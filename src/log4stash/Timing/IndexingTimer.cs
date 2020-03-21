using System;
using System.Threading;

namespace log4stash.Timing
{
    public class IndexingTimer : IIndexingTimer
    {
        private readonly Timer _timer;

        public event EventHandler<object> Elapsed;
        public int WaitTimeout { get; set; }

        public IndexingTimer(int timeout)
        {
            _timer = new Timer((state => ElapsedAction()), null, timeout, timeout);
        }

        public void ElapsedAction()
        {
            if (Elapsed != null)
            {
                Elapsed.Invoke(this, null);
            }
        }

        public void Restart(int timeout)
        {
            var timespan = TimeSpan.FromMilliseconds(timeout);
            _timer.Change(timespan, timespan);
        }

        public void Dispose()
        {
            WaitHandle notifyObj = new AutoResetEvent(false);
            _timer.Dispose(notifyObj);
            notifyObj.WaitOne(WaitTimeout);
        }
    }
}
