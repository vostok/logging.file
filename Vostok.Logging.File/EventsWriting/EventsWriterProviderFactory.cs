using System;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling;
using Vostok.Logging.File.Rolling.Strategies;

namespace Vostok.Logging.File.EventsWriting
{
    internal class EventsWriterProviderFactory : IEventsWriterProviderFactory
    {
        private static readonly FileSystem FileSystem = new FileSystem();
        private static readonly RollingStrategyFactory RollingStrategyFactory = new RollingStrategyFactory();

        public IEventsWriterProvider CreateProvider(FilePath filePath, Func<FileLogSettings> settingsProvider)
        {
            return new EventsWriterProvider(
                filePath,
                new EventsWriterFactory(FileSystem),
                new RollingStrategyProvider(filePath, RollingStrategyFactory, settingsProvider),
                new RollingGarbageCollector(FileSystem, () => settingsProvider().RollingStrategy.MaxFiles),
                new CooldownController(),
                settingsProvider);
        }
    }
}