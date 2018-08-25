using Vostok.Commons.Primitives;
using Vostok.Logging.File.EventsWriting;

namespace Vostok.Logging.File
{
    internal interface IFileSystem
    {
        string[] GetFilesByPrefix(string prefix);

        DataSize GetFileSize(string file);

        bool TryRemoveFile(string file);

        IEventsWriter OpenFile(string file);
    }
}