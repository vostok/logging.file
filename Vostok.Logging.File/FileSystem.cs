using System;
using System.IO;
using Vostok.Commons.Primitives;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;

namespace Vostok.Logging.File
{
    internal class FileSystem : IFileSystem
    {
        private readonly Func<FileLogSettings> settingsProvider;

        public FileSystem(Func<FileLogSettings> settingsProvider) =>
            this.settingsProvider = settingsProvider;

        public string[] GetFilesByPrefix(string prefix)
        {
            var directory = Path.GetDirectoryName(prefix);
            var baseName = Path.GetFileName(prefix);

            if (directory == null || !Directory.Exists(directory))
                return Array.Empty<string>();

            return Directory.GetFiles(directory, baseName + "*");
        }

        public DataSize GetFileSize(string file) => new DataSize(new FileInfo(file).Length);

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

        public IEventsWriter OpenFile(string file)
        {
            var settings = settingsProvider();

            var fileMode = settings.FileOpenMode == FileOpenMode.Append ? FileMode.Append : FileMode.Create;
            var stream = new FileStream(file, fileMode, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete, settings.OutputBufferSize); // TODO(krait): is it ok to have double buffering?
            var writer = new StreamWriter(stream, settings.Encoding, settings.OutputBufferSize, false);

            return new EventsWriter(writer);
        }
    }
}