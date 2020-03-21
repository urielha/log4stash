using System;

namespace log4stash.Timing
{
    public interface IIndexingTimer : IDisposable
    {
        event EventHandler<object> Elapsed;
        void ElapsedAction();
        void Restart(int timeout);
    }
}