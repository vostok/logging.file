using System.Text;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;

namespace Vostok.Logging.File
{
    internal interface IFileSystem
    {
        string[] GetFilesByPrefix(string prefix);

        long GetFileSize(string file);

        bool Exists(string file);

        bool TryRemoveFile(string file);

        IEventsWriter OpenFile(string file, FileOpenMode fileOpenMode, Encoding encoding, int bufferSize);
    }
}