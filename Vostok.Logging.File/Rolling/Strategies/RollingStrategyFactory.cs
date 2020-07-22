﻿using System;
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

            return new TimeBasedRollingStrategy(fileSystem, suffixFormatter, () => DateTime.Now, () => settingsProvider().RollingStrategy.SuffixSeparator);
        }

        private static IRollingStrategy CreateSizeBasedStrategy(Func<FileLogSettings> settingsProvider, IFileSystem fileSystem, SizeBasedSuffixFormatter suffixFormatter = null)
        {
            suffixFormatter = suffixFormatter ?? new SizeBasedSuffixFormatter();

            return new SizeBasedRollingStrategy(fileSystem, suffixFormatter, new SizeBasedRoller(fileSystem, () => settingsProvider().RollingStrategy.MaxSize), () => settingsProvider().RollingStrategy.SuffixSeparator);
        }

        private static IRollingStrategy CreateHybridStrategy(Func<FileLogSettings> settingsProvider, IFileSystem fileSystem)
        {
            var sizeSuffixFormatter = new SizeBasedSuffixFormatter();
            var timeSuffixFormatter = new TimeBasedSuffixFormatter(() => settingsProvider().RollingStrategy.Period);
            var hybridSuffixFormatter = new HybridSuffixFormatter(timeSuffixFormatter, sizeSuffixFormatter, () => settingsProvider().RollingStrategy.SuffixSeparator);
            var sizeStrategy = CreateSizeBasedStrategy(settingsProvider, fileSystem, sizeSuffixFormatter);

            return new HybridRollingStrategy(fileSystem, sizeStrategy, () => DateTime.Now, timeSuffixFormatter, hybridSuffixFormatter, () => settingsProvider().RollingStrategy.SuffixSeparator);
        }
    }
}