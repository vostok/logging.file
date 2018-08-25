using System;

namespace Vostok.Logging.File.Rolling
{
    internal class TimeBasedRollingStrategy : IRollingStrategy
    {
        private readonly string basePath;
        private readonly IFileSuffixFormatter<DateTime> suffixFormatter;
        private readonly Func<DateTime> timeProvider;
        private readonly IFileSystem fileSystem;

        public TimeBasedRollingStrategy(string basePath, IFileSystem fileSystem, IFileSuffixFormatter<DateTime> suffixFormatter, Func<DateTime> timeProvider)
        {
            this.basePath = basePath;
            this.suffixFormatter = suffixFormatter;
            this.timeProvider = timeProvider;
            this.fileSystem = fileSystem;
        }

        public string[] DiscoverExistingFiles() => RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter.TryParseSuffix);

        public string GetCurrentFile() => basePath + suffixFormatter.FormatSuffix(timeProvider());
    }
}