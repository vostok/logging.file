using System.Collections.Generic;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal class DisabledRollingStrategy : IRollingStrategy
    {
        private readonly IFileSystem fileSystem;

        public DisabledRollingStrategy(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public IEnumerable<string> DiscoverExistingFiles(string basePath)
        {
            if (fileSystem.Exists(basePath))
                yield return basePath;
        }

        public string GetCurrentFile(string basePath) => basePath;
    }
}