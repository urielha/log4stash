namespace log4stash.FileAccess
{
    public interface IFileAccessor
    {
        string ReadAllText(string fileName);
        bool Exists(string fileName);
    }
}