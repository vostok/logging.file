using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling;
using Vostok.Logging.File.Rolling.Strategies;

namespace Vostok.Logging.File.EventsWriting
{
    /// <summary>
    /// Not thread-safe. Expected usage pattern: <see cref="ObtainWriterAsync"/> --> <see cref="ObtainWriterAsync"/> --> ... --> <see cref="Dispose"/>.
    /// </summary>
    internal class EventsWriterProvider : IEventsWriterProvider
    {
        private readonly FilePath basePath;
        private readonly IEventsWriterFactory eventsWriterFactory;
        private readonly IRollingStrategyProvider rollingStrategyProvider;
        private readonly IRollingGarbageCollector garbageCollector;
        private readonly ICooldownController cooldownController;
        private readonly Func<FileLogSettings> settingsProvider;

        private (FilePath file, FileLogSettings settings, IEventsWriter writer) cache;

        public EventsWriterProvider(
            FilePath basePath,
            IEventsWriterFactory eventsWriterFactory,
            IRollingStrategyProvider rollingStrategyProvider,
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

        public async Task<IEventsWriter> ObtainWriterAsync(CancellationToken cancellation)
        {
            if (cache.writer == null)
                await cooldownController.WaitForCooldownAsync().ConfigureAwait(false);

            if (cooldownController.IsCool)
            {
                var settings = settingsProvider();

                try
                {
                    var rollingStrategy = rollingStrategyProvider.ObtainStrategy();

                    var currentFile = rollingStrategy.GetCurrentFile(basePath);
                    if (currentFile != cache.file || ShouldReopenWriter(cache.settings, settings) || cache.writer == null)
                    {
                        cache.writer?.Dispose();
                        cache.writer = null;
                        cache = (currentFile, settings, eventsWriterFactory.CreateWriter(currentFile, settings));
                        garbageCollector.RemoveStaleFiles(rollingStrategy.DiscoverExistingFiles(basePath).ToArray());
                    }
                }
                finally
                {
                    cooldownController.IncurCooldown(settings.RollingUpdateCooldown, cancellation);
                }
            }

            return cache.writer;
        }

        public void Dispose()
        {
            cache.writer?.Dispose();
            cache.writer = null;
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