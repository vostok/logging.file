using System;
using System.Linq;
using System.Threading.Tasks;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling;
using Vostok.Logging.File.Rolling.Strategies;

namespace Vostok.Logging.File.EventsWriting
{
    internal class EventsWriterProvider : IEventsWriterProvider
    {
        private readonly FilePath basePath;
        private readonly IRollingStrategyProvider rollingStrategyProvider;
        private readonly IEventsWriterFactory eventsWriterFactory;
        private readonly IRollingGarbageCollector garbageCollector;
        private readonly Func<FileLogSettings> settingsProvider;
        private readonly ICooldownController cooldownController;
        private readonly object sync = new object();

        private (FilePath file, FileLogSettings settings, IEventsWriter writer) cache;
        private bool isDisposed;

        public EventsWriterProvider(
            FilePath basePath,
            IRollingStrategyProvider rollingStrategyProvider,
            IEventsWriterFactory eventsWriterFactory,
            IRollingGarbageCollector garbageCollector,
            ICooldownController cooldownController,
            Func<FileLogSettings> settingsProvider)
        {
            this.basePath = basePath;
            this.rollingStrategyProvider = rollingStrategyProvider;
            this.eventsWriterFactory = eventsWriterFactory;
            this.garbageCollector = garbageCollector;
            this.settingsProvider = settingsProvider;
            this.cooldownController = cooldownController;
        }

        public async Task<IEventsWriter> ObtainWriterAsync()
        {
            if (cache.writer == null)
                await cooldownController.WaitForCooldownAsync();

            lock (sync)
            {
                if (isDisposed)
                    throw new ObjectDisposedException(GetType().Name);

                if (cooldownController.IsCool)
                {
                    var settings = settingsProvider();

                    try
                    {
                        var rollingStrategy = rollingStrategyProvider.ObtainStrategy();

                        var currentFile = rollingStrategy.GetCurrentFile(basePath.NormalizedPath);

                        if (currentFile != cache.file || ShouldReopenWriter(cache.settings, settings) || cache.writer == null)
                        {
                            cache.writer?.Dispose();
                            cache.writer = null;
                            cache = (currentFile, settings, eventsWriterFactory.CreateWriter(currentFile, settings));
                            garbageCollector.RemoveStaleFiles(rollingStrategy.DiscoverExistingFiles(basePath.NormalizedPath).ToArray());
                        }
                    }
                    finally
                    {
                        cooldownController.IncurCooldown(settings.RollingUpdateCooldown);
                    }
                }

                return cache.writer;
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                isDisposed = true;

                cache.writer?.Dispose();
                cache.writer = null;
            }
        }

        private static bool ShouldReopenWriter(FileLogSettings oldSettings, FileLogSettings newSettings)
        {
            return oldSettings == null ||
                   oldSettings.FileOpenMode != newSettings.FileOpenMode ||
                   !Equals(oldSettings.Encoding, newSettings.Encoding) ||
                   oldSettings.OutputBufferSize != newSettings.OutputBufferSize;
        }
    }
}