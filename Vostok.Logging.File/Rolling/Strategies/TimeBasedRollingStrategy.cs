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
        private readonly Func<char> suffixSeparatorProvider;

        public TimeBasedRollingStrategy(IFileSystem fileSystem, IFileSuffixFormatter<DateTime> suffixFormatter, Func<DateTime> timeProvider, Func<char> suffixSeparatorProvider)
        {
            this.suffixFormatter = suffixFormatter;
            this.timeProvider = timeProvider;
            this.fileSystem = fileSystem;
            this.suffixSeparatorProvider = suffixSeparatorProvider;
        }

        public IEnumerable<FilePath> DiscoverExistingFiles(FilePath basePath) =>
            RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter, suffixSeparatorProvider()).Select(file => file.path);

        public FilePath GetCurrentFile(FilePath basePath) => RollingStrategyHelper.AddSuffix(basePath, suffixFormatter.FormatSuffix(timeProvider()), false, suffixSeparatorProvider());
    }
}