using System;
using Vostok.Commons.Primitives;

namespace Vostok.Logging.File.Rolling
{
    internal class SizeBasedRoller : ISizeBasedRoller
    {
        private readonly IFileSystem fileSystem;
        private readonly Func<DataSize> maxFileSizeProvider;

        public SizeBasedRoller(IFileSystem fileSystem, Func<DataSize> maxFileSizeProvider)
        {
            this.fileSystem = fileSystem;
            this.maxFileSizeProvider = maxFileSizeProvider;
        }

        public bool ShouldRollOver(string currentFilePath)
        {
            if (currentFilePath != null)
            {
                var maxFileSize = maxFileSizeProvider();

                if (fileSystem.GetFileSize(currentFilePath) >= maxFileSize)
                    return true;
            }

            return false;
        }
    }
}