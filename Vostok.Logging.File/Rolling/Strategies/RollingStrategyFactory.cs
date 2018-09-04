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
            var fileNameTuner = new FileNameTuner(basePath.NormalizedPath);

            switch (type)
            {
                case RollingStrategyType.None:
                    return new DisabledRollingStrategy(fileSystem);
                case RollingStrategyType.ByTime:
                    return CreateTimeBasedStrategy(settingsProvider, fileSystem, fileNameTuner);
                case RollingStrategyType.BySize:
                    return CreateSizeBasedStrategy(settingsProvider, fileSystem, fileNameTuner);
                case RollingStrategyType.Hybrid:
                    return CreateHybridStrategy(settingsProvider, fileSystem, fileNameTuner);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        private static IRollingStrategy CreateTimeBasedStrategy(Func<FileLogSettings> settingsProvider, IFileSystem fileSystem, IFileNameTuner fileNameTuner, TimeBasedSuffixFormatter suffixFormatter = null)
        {
            suffixFormatter = suffixFormatter ?? new TimeBasedSuffixFormatter(() => settingsProvider().RollingStrategy.Period);

            return new TimeBasedRollingStrategy(fileSystem, suffixFormatter, () => DateTime.Now, fileNameTuner);
        }

        private static IRollingStrategy CreateSizeBasedStrategy(Func<FileLogSettings> settingsProvider, IFileSystem fileSystem, IFileNameTuner fileNameTuner, SizeBasedSuffixFormatter suffixFormatter = null)
        {
            suffixFormatter = suffixFormatter ?? new SizeBasedSuffixFormatter();

            return new SizeBasedRollingStrategy(fileSystem, suffixFormatter, new SizeBasedRoller(fileSystem, () => settingsProvider().RollingStrategy.MaxSize), fileNameTuner);
        }

        private static IRollingStrategy CreateHybridStrategy(Func<FileLogSettings> settingsProvider, IFileSystem fileSystem, IFileNameTuner fileNameTuner)
        {
            var timeSuffixFormatter = new TimeBasedSuffixFormatter(() => settingsProvider().RollingStrategy.Period);
            var sizeSuffixFormatter = new SizeBasedSuffixFormatter();
            var timeStrategy = CreateTimeBasedStrategy(settingsProvider, fileSystem, fileNameTuner, timeSuffixFormatter);
            var sizeStrategy = CreateSizeBasedStrategy(settingsProvider, fileSystem, fileNameTuner, sizeSuffixFormatter);
            var suffixFormatter = new HybridSuffixFormatter(timeSuffixFormatter, sizeSuffixFormatter);

            return new HybridRollingStrategy(fileSystem, timeStrategy, sizeStrategy, suffixFormatter, fileNameTuner);
        }
    }
}