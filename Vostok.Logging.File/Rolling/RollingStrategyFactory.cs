using System;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Rolling
{
    internal class RollingStrategyFactory
    {
        public IRollingStrategy CreateStrategy(string basePath, RollingStrategyType type, Func<FileLogSettings> settingsProvider)
        {
            var fileSystem = new FileSystem(settingsProvider);

            switch (type)
            {
                case RollingStrategyType.None:
                    return new DisabledRollingStrategy(basePath, fileSystem);
                case RollingStrategyType.ByTime:
                    return CreateTimeBasedStrategy(basePath, settingsProvider, fileSystem);
                case RollingStrategyType.BySize:
                    return CreateSizeBasedStrategy(basePath, settingsProvider, fileSystem);
                case RollingStrategyType.Hybrid:
                    return CreateHybridStrategy(basePath, settingsProvider, fileSystem);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static IRollingStrategy CreateTimeBasedStrategy(string basePath, Func<FileLogSettings> settingsProvider, IFileSystem fileSystem)
        {
            var suffixFormatter = new TimeBasedSuffixFormatter(() => settingsProvider().RollingStrategy.Period);

            return new TimeBasedRollingStrategy(basePath, fileSystem, suffixFormatter, () => DateTime.Now);
        }

        private static IRollingStrategy CreateSizeBasedStrategy(string basePath, Func<FileLogSettings> settingsProvider, IFileSystem fileSystem)
        {
            var suffixFormatter = new SizeBasedSuffixFormatter();

            return new SizeBasedRollingStrategy(basePath, fileSystem, suffixFormatter, new SizeBasedRoller(fileSystem, () => settingsProvider().RollingStrategy.MaxSize));
        }

        private static IRollingStrategy CreateHybridStrategy(string basePath, Func<FileLogSettings> settingsProvider, IFileSystem fileSystem)
        {
            var sizeSuffixFormatter = new SizeBasedSuffixFormatter();
            var timeSuffixFormatter = new TimeBasedSuffixFormatter(() => settingsProvider().RollingStrategy.Period);

            return new HybridRollingStrategy(basePath, fileSystem, timeSuffixFormatter, sizeSuffixFormatter, () => DateTime.Now, new SizeBasedRoller(fileSystem, () => settingsProvider().RollingStrategy.MaxSize));
        }
    }
}