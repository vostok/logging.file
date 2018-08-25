using System;

namespace Vostok.Logging.File.Rolling
{
    internal class HybridRollingStrategy : IRollingStrategy
    {
        private readonly string basePath;
        private readonly ISizeBasedRoller sizeBasedRoller;
        private readonly IFileSystem fileSystem;
        private readonly IFileSuffixFormatter<DateTime> timeSuffixFormatter;
        private readonly IFileSuffixFormatter<int> sizeSuffixFormatter;
        private readonly Func<DateTime> timeProvider;

        public HybridRollingStrategy(string basePath, IFileSystem fileSystem, IFileSuffixFormatter<DateTime> timeSuffixFormatter, IFileSuffixFormatter<int> sizeSuffixFormatter, Func<DateTime> timeProvider, ISizeBasedRoller sizeBasedRoller)
        {
            this.basePath = basePath;
            this.sizeBasedRoller = sizeBasedRoller;
            this.fileSystem = fileSystem;
            this.timeSuffixFormatter = timeSuffixFormatter;
            this.sizeSuffixFormatter = sizeSuffixFormatter;
            this.timeProvider = timeProvider;
        }

        public string[] DiscoverExistingFiles() => RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, TryParseSuffix);

        public string GetCurrentFile()
        {
            var timeBasedPrefix = basePath + timeSuffixFormatter.FormatSuffix(timeProvider());

            return RollingStrategyHelper.GetCurrentFileBySize(timeBasedPrefix, fileSystem, sizeSuffixFormatter, sizeBasedRoller);
        }

        private (DateTime, int)? TryParseSuffix(string suffix)
        {
            var lastDotIndex = suffix.LastIndexOf('.');

            if (lastDotIndex < 0)
                return null;

            var leftPart = suffix.Substring(0, lastDotIndex);
            var rightPart = suffix.Substring(lastDotIndex + 1);

            var date = timeSuffixFormatter.TryParseSuffix(leftPart);
            var part = sizeSuffixFormatter.TryParseSuffix(rightPart);

            return date == null || part == null ? null as (DateTime, int)? : (date.Value, part.Value);
        }
    }
}