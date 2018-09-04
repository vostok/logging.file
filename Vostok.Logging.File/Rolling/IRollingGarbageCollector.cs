using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Rolling
{
    internal interface IRollingGarbageCollector
    {
        void RemoveStaleFiles(FilePath[] allFiles);
    }
}