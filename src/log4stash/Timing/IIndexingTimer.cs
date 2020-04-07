using System;

namespace log4stash.Timing
{
    public interface IIndexingTimer : IDisposable
    {
        event EventHandler Elapsed;
        void ElapsedAction();
        void Restart(int timeout);
    }
}