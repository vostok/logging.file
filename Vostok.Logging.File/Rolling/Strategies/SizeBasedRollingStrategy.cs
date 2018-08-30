using System.Collections.Generic;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal class SizeBasedRollingStrategy : IRollingStrategy
    {
        private readonly ISizeBasedRoller sizeBasedRoller;
        private readonly IFileSystem fileSystem;
        private readonly IFileSuffixFormatter<int> suffixFormatter;

        public SizeBasedRollingStrategy(IFileSystem fileSystem, IFileSuffixFormatter<int> suffixFormatter, ISizeBasedRoller sizeBasedRoller)
        {
            this.sizeBasedRoller = sizeBasedRoller;
            this.fileSystem = fileSystem;
            this.suffixFormatter = suffixFormatter;
        }

        public IEnumerable<string> DiscoverExistingFiles(string basePath) => RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter.TryParseSuffix);

        public string GetCurrentFile(string basePath) => RollingStrategyHelper.GetCurrentFileBySize(basePath, fileSystem, suffixFormatter, sizeBasedRoller);
    }
}