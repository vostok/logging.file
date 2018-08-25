namespace Vostok.Logging.File.Rolling
{
    internal interface IRollingGarbageCollector
    {
        void RemoveStaleFiles(string[] allFiles);
    }
}