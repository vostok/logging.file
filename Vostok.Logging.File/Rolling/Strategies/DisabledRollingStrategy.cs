using System.Collections.Generic;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal class DisabledRollingStrategy : IRollingStrategy
    {
        private readonly IFileSystem fileSystem;

        public DisabledRollingStrategy(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public IEnumerable<FilePath> DiscoverExistingFiles(FilePath basePath)
        {
            if (fileSystem.Exists(basePath.NormalizedPath))
                yield return basePath;
        }

        public FilePath GetCurrentFile(FilePath basePath) => basePath;
    }
}