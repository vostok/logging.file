using System;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Rolling.Strategies;

namespace Vostok.Logging.File.Rolling
{
    internal class RollingStrategyFactory
    {
        public IRollingStrategy CreateStrategy(RollingStrategyType type, Func<FileLogSettings> settingsProvider)
        {
            var fileSystem = new FileSystem();

            switch (type)
            {
                case RollingStrategyType.None:
                    return new DisabledRollingStrategy(fileSystem);
                case RollingStrategyType.ByTime:
                    return CreateTimeBasedStrategy(settingsProvider, fileSystem);
                case RollingStrategyType.BySize:
                    return CreateSizeBasedStrategy(settingsProvider, fileSystem);
                case RollingStrategyType.Hybrid:
                    return CreateHybridStrategy(settingsProvider, fileSystem);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static IRollingStrategy CreateTimeBasedStrategy(Func<FileLogSettings> settingsProvider, IFileSystem fileSystem)
        {
            var suffixFormatter = new TimeBasedSuffixFormatter(() => settingsProvider().RollingStrategy.Period);

            return new TimeBasedRollingStrategy(fileSystem, suffixFormatter, () => DateTime.Now);
        }

        private static IRollingStrategy CreateSizeBasedStrategy(Func<FileLogSettings> settingsProvider, IFileSystem fileSystem)
        {
            var suffixFormatter = new SizeBasedSuffixFormatter();

            return new SizeBasedRollingStrategy(fileSystem, suffixFormatter, new SizeBasedRoller(fileSystem, () => settingsProvider().RollingStrategy.MaxSize));
        }

        private static IRollingStrategy CreateHybridStrategy(Func<FileLogSettings> settingsProvider, IFileSystem fileSystem)
        {
            var sizeSuffixFormatter = new SizeBasedSuffixFormatter();
            var timeSuffixFormatter = new TimeBasedSuffixFormatter(() => settingsProvider().RollingStrategy.Period);

            return new HybridRollingStrategy(fileSystem, timeSuffixFormatter, sizeSuffixFormatter, () => DateTime.Now, new SizeBasedRoller(fileSystem, () => settingsProvider().RollingStrategy.MaxSize));
        }
    }
}