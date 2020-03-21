using System;
using System.Collections.Concurrent;

namespace log4stash.Extensions
{
    public class TolerateCallsBase
    {
        public virtual void Call(Action action, Type type, int hint)
        {
            action();
        }
    }

    /// <summary>
    /// Tolerating calls to function in given timespan.
    /// First call to <see cref="Call"/> with given type and hint key will call the action.
    /// After that, all calls (with same type and key) will be dropped till the timespan is over.
    /// 
    /// The function <see cref="Call"/> is thread safe.
    /// </summary>
    public class TolerateCalls : TolerateCallsBase
    {
        private readonly TimeSpan _tolerance;
        private readonly ConcurrentDictionary<Tuple<Type, int>, DateTime> 
            _errorsHistory = new ConcurrentDictionary<Tuple<Type, int>, DateTime>();

        public TolerateCalls(TimeSpan tolerance)
        {
            _tolerance = tolerance;
        }

        public override void Call(Action action, Type type, int hint)
        {
            var tup = new Tuple<Type, int>(type, hint);
            var now = DateTime.Now;
            if (_errorsHistory.TryAdd(tup, now))
            {
                action();
                return;
            }

            var lastErr = _errorsHistory[tup];
            if (now - lastErr > _tolerance)
            {
                if (_errorsHistory.TryUpdate(tup, now, lastErr))
                {
                    action();
                }
            }
        }
    }
}
