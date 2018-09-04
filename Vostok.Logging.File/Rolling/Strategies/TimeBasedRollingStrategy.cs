using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Rolling.Strategies
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

        public IEnumerable<FilePath> DiscoverExistingFiles(FilePath basePath) =>
            RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter).Select(file => file.path);

        public FilePath GetCurrentFile(FilePath basePath) => basePath + suffixFormatter.FormatSuffix(timeProvider());
    }
}