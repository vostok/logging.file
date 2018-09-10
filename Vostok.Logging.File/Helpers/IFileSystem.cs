using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Helpers
{
    internal interface IFileSystem
    {
        IEnumerable<FilePath> GetFilesByPrefix(FilePath file);

        long GetFileSize(FilePath file);

        bool Exists(FilePath file);

        bool TryRemoveFile(FilePath file);

        [CanBeNull]
        TextWriter OpenFile(FilePath file, FileOpenMode fileOpenMode, Encoding encoding, int bufferSize);
    }
}