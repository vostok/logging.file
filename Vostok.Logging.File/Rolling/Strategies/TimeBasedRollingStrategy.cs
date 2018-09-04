using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Helpers;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal class TimeBasedRollingStrategy : IRollingStrategy
    {
        private readonly IFileSuffixFormatter<DateTime> suffixFormatter;
        private readonly Func<DateTime> timeProvider;
        private readonly IFileNameTuner fileNameTuner;
        private readonly IFileSystem fileSystem;

        public TimeBasedRollingStrategy(IFileSystem fileSystem, IFileSuffixFormatter<DateTime> suffixFormatter, Func<DateTime> timeProvider, IFileNameTuner fileNameTuner)
        {
            this.suffixFormatter = suffixFormatter;
            this.timeProvider = timeProvider;
            this.fileNameTuner = fileNameTuner;
            this.fileSystem = fileSystem;
        }

        public IEnumerable<string> DiscoverExistingFiles(string basePath) =>
            RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter, fileNameTuner).Select(file => fileNameTuner.RestoreExtension(file.path));

        public string GetCurrentFile(string basePath) => fileNameTuner.RestoreExtension(fileNameTuner.RemoveExtension(basePath) + suffixFormatter.FormatSuffix(timeProvider()));
    }
}
