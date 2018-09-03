using System;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Rolling;
using Vostok.Logging.File.Rolling.Strategies;

namespace Vostok.Logging.File
{
    internal class RollingStrategyProvider
    {
        private readonly FilePath basePath;
        private readonly RollingStrategyFactory strategyFactory;
        private readonly Func<FileLogSettings> settingsProvider;

        public RollingStrategyProvider(FilePath basePath, RollingStrategyFactory strategyFactory, Func<FileLogSettings> settingsProvider)
        {
            this.basePath = basePath;
            this.strategyFactory = strategyFactory;
            this.settingsProvider = settingsProvider;
        }

        private (RollingStrategyType type, IRollingStrategy strategy) currentItem;

        public IRollingStrategy ObtainStrategy()
        {
            var newType = settingsProvider().RollingStrategy.Type;

            if (newType != currentItem.type || currentItem.strategy == null)
                currentItem = (newType, strategyFactory.CreateStrategy(basePath, newType, settingsProvider));

            return currentItem.strategy;
        }
    }
}