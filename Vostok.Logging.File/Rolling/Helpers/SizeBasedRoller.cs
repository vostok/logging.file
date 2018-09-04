using System;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Rolling.Helpers
{
    internal class SizeBasedRoller : ISizeBasedRoller
    {
        private readonly IFileSystem fileSystem;
        private readonly Func<long> maxFileSizeProvider;

        public SizeBasedRoller(IFileSystem fileSystem, Func<long> maxFileSizeProvider)
        {
            this.fileSystem = fileSystem;
            this.maxFileSizeProvider = maxFileSizeProvider;
        }

        public bool ShouldRollOver(FilePath currentFilePath)
        {
            if (currentFilePath != null)
            {
                var maxFileSize = maxFileSizeProvider();

                if (fileSystem.GetFileSize(currentFilePath.NormalizedPath) >= maxFileSize)
                    return true;
            }

            return false;
        }
    }
}