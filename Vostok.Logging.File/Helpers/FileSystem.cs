using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Helpers
{
    internal class FileSystem : IFileSystem
    {
        public IEnumerable<FilePath> GetFilesByPrefix(FilePath path) =>
            GetFilesByPrefix(path.PathWithoutExtension).Select(f => (FilePath)f);

        public long GetFileSize(FilePath file)
        {
            var fileInfo = new FileInfo(file.NormalizedPath);

            return fileInfo.Exists ? fileInfo.Length : 0;
        }

        public bool Exists(FilePath file) => System.IO.File.Exists(file.NormalizedPath);

        public bool TryRemoveFile(FilePath file)
        {
            for (var i = 0; i < 5; i++)
            {
                if (!System.IO.File.Exists(file.NormalizedPath))
                    return true;

                try
                {
                    System.IO.File.Delete(file.NormalizedPath);
                    return true;
                }
                catch
                {
                    // ignored
                }
            }

            return false;
        }

        public TextWriter OpenFile(FilePath file, FileOpenMode fileOpenMode, Encoding encoding, int bufferSize)
        {
            try
            {
                var fileMode = fileOpenMode == FileOpenMode.Append ? FileMode.Append : FileMode.Create;
                var stream = new FileStream(file.NormalizedPath, fileMode, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete, 1);

                return new StreamWriter(stream, encoding, bufferSize, false);
            }
            catch (Exception error)
            {
                SafeConsole.ReportError($"Failed to open log file '{file}':", error);
                return null;
            }
        }

        private static IEnumerable<string> GetFilesByPrefix(string prefix)
        {
            var directory = Path.GetDirectoryName(prefix);
            var baseName = Path.GetFileName(prefix);

            if (directory == null || !Directory.Exists(directory))
                return Array.Empty<string>();

            return Directory.EnumerateFiles(directory, baseName + "*");
        }
    }
}