using System;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Helpers;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal class RollingStrategyFactory : IRollingStrategyFactory
    {
        public IRollingStrategy CreateStrategy(FilePath basePath, RollingStrategyType type, Func<FileLogSettings> settingsProvider)
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

        private static IRollingStrategy CreateTimeBasedStrategy(Func<FileLogSettings> settingsProvider, IFileSystem fileSystem, TimeBasedSuffixFormatter suffixFormatter = null)
        {
            suffixFormatter = suffixFormatter ?? new TimeBasedSuffixFormatter(() => settingsProvider().RollingStrategy.Period);

            return new TimeBasedRollingStrategy(fileSystem, suffixFormatter, () => DateTime.Now);
        }

        private static IRollingStrategy CreateSizeBasedStrategy(Func<FileLogSettings> settingsProvider, IFileSystem fileSystem, SizeBasedSuffixFormatter suffixFormatter = null)
        {
            suffixFormatter = suffixFormatter ?? new SizeBasedSuffixFormatter();

            return new SizeBasedRollingStrategy(fileSystem, suffixFormatter, new SizeBasedRoller(fileSystem, () => settingsProvider().RollingStrategy.MaxSize));
        }

        private static IRollingStrategy CreateHybridStrategy(Func<FileLogSettings> settingsProvider, IFileSystem fileSystem)
        {
            var timeSuffixFormatter = new TimeBasedSuffixFormatter(() => settingsProvider().RollingStrategy.Period);
            var sizeSuffixFormatter = new SizeBasedSuffixFormatter();
            var timeStrategy = CreateTimeBasedStrategy(settingsProvider, fileSystem, timeSuffixFormatter);
            var sizeStrategy = CreateSizeBasedStrategy(settingsProvider, fileSystem, sizeSuffixFormatter);
            var suffixFormatter = new HybridSuffixFormatter(timeSuffixFormatter, sizeSuffixFormatter);

            return new HybridRollingStrategy(fileSystem, timeStrategy, sizeStrategy, suffixFormatter);
        }
    }
}