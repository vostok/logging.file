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
        private readonly IRollingStrategy sizeRollingStrategy;
        private readonly Func<DateTime> timeProvider;
        private readonly IFileSuffixFormatter<DateTime> timeSuffixFormatter;
        private readonly IFileSuffixFormatter<(DateTime, int)> hybridSuffixFormatter;

        public HybridRollingStrategy(
            IFileSystem fileSystem, 
            IRollingStrategy sizeRollingStrategy, 
            Func<DateTime> timeProvider, 
            IFileSuffixFormatter<DateTime> timeSuffixFormatter, 
            IFileSuffixFormatter<(DateTime, int)> hybridSuffixFormatter)
        {
            this.fileSystem = fileSystem;
            this.sizeRollingStrategy = sizeRollingStrategy;
            this.timeProvider = timeProvider;
            this.timeSuffixFormatter = timeSuffixFormatter;
            this.hybridSuffixFormatter = hybridSuffixFormatter;
        }

        public IEnumerable<FilePath> DiscoverExistingFiles(FilePath basePath) =>
            RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, hybridSuffixFormatter).Select(file => file.path);

        public FilePath GetCurrentFile(FilePath basePath)
        {
            var timeBasedPrefix = RollingStrategyHelper.AddSuffix(basePath, timeSuffixFormatter.FormatSuffix(timeProvider()), true);

            return sizeRollingStrategy.GetCurrentFile(timeBasedPrefix);
        }
    }
}