﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
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

        public TextWriter TryOpenFile(FilePath file, FileLogSettings settings)
        {
            if (!settings.UseSeparateFileOnConflict)
                return TryOpenFileOnce(file, settings);

            for (var i = 0; i < 5; i++)
            {
                var currentFile = i == 0 ? file : file + $"{settings.RollingStrategy.SuffixSeparator}{i}";
                var writer = TryOpenFileOnce(currentFile, settings);
                if (writer != null)
                    return writer;
            }

            return null;
        }

        private static TextWriter TryOpenFileOnce(FilePath file, FileLogSettings settings)
        {
            string ReplaceSlashes(string value) => value.Replace('\\', '/').Replace('/', '_');

            FileStream CreateFileStream(FileMode fileMode) => new FileStream(file.NormalizedPath, fileMode, FileAccess.Write, settings.FileShare, 1);

            FileStream CreateFileStreamOnUnix(FileMode fileMode)
            {
                using (var m = new Mutex(false, $"Global\\{ReplaceSlashes(file.NormalizedPath)}-FileLogMutex"))
                {
                    var hasMutex = false;
                    try
                    {
                        if (m.WaitOne(2))
                        {
                            hasMutex = true;
                            // NOTE: See https://github.com/dotnet/runtime/issues/34126
                            // In order to avoid multiple writers, we want to fall in case file is opened either for reading or writing.
                            
                            using (new FileStream(file.NormalizedPath, fileMode, FileAccess.Write, FileShare.None, 1)){}

                            return CreateFileStream(fileMode);
                        }
                    }
                    finally
                    {
                        if (hasMutex)
                        {
                            m.ReleaseMutex();
                        }
                    }
                }

                throw new Exception("blabla");
            }
            
            
            try
            {
                var directory = Path.GetDirectoryName(file.NormalizedPath);
                if (directory != null && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var fileMode = settings.FileOpenMode == FileOpenMode.Append ? FileMode.Append : FileMode.Create;

                var stream = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                    ? CreateFileStream(fileMode)
                    : CreateFileStreamOnUnix(fileMode);
                

                return new StreamWriter(stream, settings.Encoding, settings.OutputBufferSize, false);
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