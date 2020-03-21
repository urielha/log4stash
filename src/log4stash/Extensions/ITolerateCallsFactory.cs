namespace log4stash.Extensions
{
    public interface ITolerateCallsFactory
    {
        TolerateCallsBase Create(int toleranceSec);
    }
}