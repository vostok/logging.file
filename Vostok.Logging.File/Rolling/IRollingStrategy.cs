namespace Vostok.Logging.File.Rolling
{
    internal interface IRollingStrategy
    {
        string[] DiscoverExistingFiles();

        string GetCurrentFile();
    }
}