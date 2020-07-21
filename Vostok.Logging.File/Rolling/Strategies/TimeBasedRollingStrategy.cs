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
        private readonly char suffixEliminator;

        public TimeBasedRollingStrategy(IFileSystem fileSystem, IFileSuffixFormatter<DateTime> suffixFormatter, Func<DateTime> timeProvider, char suffixEliminator = '-')
        {
            this.suffixFormatter = suffixFormatter;
            this.timeProvider = timeProvider;
            this.fileSystem = fileSystem;
            this.suffixEliminator = suffixEliminator;
        }

        public IEnumerable<FilePath> DiscoverExistingFiles(FilePath basePath) =>
            RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter, suffixEliminator).Select(file => file.path);

        public FilePath GetCurrentFile(FilePath basePath) => RollingStrategyHelper.AddSuffix(basePath, suffixFormatter.FormatSuffix(timeProvider()), false, suffixEliminator);
    }
}