using System;

namespace log4stash.Extensions
{
    public class TolerateCallsFactory : ITolerateCallsFactory
    {
        public TolerateCallsBase Create(int toleranceSec)
        {
            if (toleranceSec <= 0)
            {
                return new TolerateCallsBase();
            }

            return new TolerateCalls(TimeSpan.FromSeconds(toleranceSec));
        }
    }
}