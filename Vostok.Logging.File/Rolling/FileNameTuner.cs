using System;
using System.IO;

namespace Vostok.Logging.File.Rolling
{
    internal class FileNameTuner : IFileNameTuner
    {
        private readonly string extension;

        public FileNameTuner(string basePath)
        {
            extension = Path.GetExtension(basePath);
        }

        public string RestoreExtension(string file)
        {
            if (string.IsNullOrEmpty(extension))
                return file;

            return file + extension;
        }

        public string RemoveExtension(string file)
        {
            if (string.IsNullOrEmpty(extension) || file.Length < extension.Length)
                return file;

            var extensionStart = file.Length - extension.Length;
            if (string.Compare(file, extensionStart, extension, 0, extension.Length, FilePath.PathComparison) == 0)
                return file.Substring(0, extensionStart);

            return file;
        }
    }
}