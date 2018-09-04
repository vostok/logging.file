using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal class HybridRollingStrategy : IRollingStrategy
    {
        private readonly IFileSystem fileSystem;
        private readonly IRollingStrategy timeRollingStrategy;
        private readonly IRollingStrategy sizeRollingStrategy;
        private readonly IFileSuffixFormatter<(DateTime, int)> suffixFormatter;

        public HybridRollingStrategy(IFileSystem fileSystem, IRollingStrategy timeRollingStrategy, IRollingStrategy sizeRollingStrategy, IFileSuffixFormatter<(DateTime, int)> suffixFormatter)
        {
            this.fileSystem = fileSystem;
            this.timeRollingStrategy = timeRollingStrategy;
            this.sizeRollingStrategy = sizeRollingStrategy;
            this.suffixFormatter = suffixFormatter;
        }

        public IEnumerable<FilePath> DiscoverExistingFiles(FilePath basePath) =>
            RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter).Select(file => file.path);

        public FilePath GetCurrentFile(FilePath basePath)
        {
            var timeBasedPrefix = timeRollingStrategy.GetCurrentFile(basePath);

            return sizeRollingStrategy.GetCurrentFile(timeBasedPrefix);
        }
    }
}