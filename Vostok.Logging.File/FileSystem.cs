using System;
using System.IO;
using System.Text;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;

namespace Vostok.Logging.File
{
    internal class FileSystem : IFileSystem
    {
        public string[] GetFilesByPrefix(string prefix)
        {
            var directory = Path.GetDirectoryName(prefix);
            var baseName = Path.GetFileName(prefix);

            if (directory == null || !Directory.Exists(directory))
                return Array.Empty<string>();

            return Directory.GetFiles(directory, baseName + "*");
        }

        public long GetFileSize(string file)
        { 
            var fileInfo = new FileInfo(file);

            return fileInfo.Exists ? fileInfo.Length : 0;
        }

        public bool Exists(string file) => System.IO.File.Exists(file);

        public bool TryRemoveFile(string file)
        {
            for (var i = 0; i < 5; i++)
            {
                if (!System.IO.File.Exists(file))
                    return true;

                try
                {
                    System.IO.File.Delete(file);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            return false;
        }

        public IEventsWriter OpenFile(string file, FileOpenMode fileOpenMode, Encoding encoding, int bufferSize)
        {
            var fileMode = fileOpenMode == FileOpenMode.Append ? FileMode.Append : FileMode.Create;
            var stream = new FileStream(file, fileMode, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete, 1);
            var writer = new StreamWriter(stream, encoding, bufferSize, false);

            return new EventsWriter(writer);
        }
    }
}