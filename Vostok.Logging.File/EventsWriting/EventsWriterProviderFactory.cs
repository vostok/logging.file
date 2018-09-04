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

        public IEventsWriterProvider CreateProvider(FilePath filePath, Func<FileLogSettings> settingsProvider)
        {
            return new EventsWriterProvider(
                filePath,
                new RollingStrategyProvider(filePath, new RollingStrategyFactory(), settingsProvider),
                FileSystem,
                new RollingGarbageCollector(FileSystem, () => settingsProvider().RollingStrategy.MaxFiles),
                new CooldownController(),
                settingsProvider);
        }
    }
}