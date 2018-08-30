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

            return file.Substring(0, file.Length - extension.Length);
        }
    }
}