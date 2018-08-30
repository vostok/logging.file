using System;
using System.Collections.Generic;
using Vostok.Logging.File.Rolling.Strategies;

namespace Vostok.Logging.File.Rolling
{
    internal class TimeBasedRollingStrategy : IRollingStrategy
    {
        private readonly IFileSuffixFormatter<DateTime> suffixFormatter;
        private readonly Func<DateTime> timeProvider;
        private readonly IFileSystem fileSystem;

        public TimeBasedRollingStrategy(IFileSystem fileSystem, IFileSuffixFormatter<DateTime> suffixFormatter, Func<DateTime> timeProvider)
        {
            this.suffixFormatter = suffixFormatter;
            this.timeProvider = timeProvider;
            this.fileSystem = fileSystem;
        }

        public IEnumerable<string> DiscoverExistingFiles(string basePath) => RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter.TryParseSuffix);

        public string GetCurrentFile(string basePath) => basePath + suffixFormatter.FormatSuffix(timeProvider());
    }
}