using System;
using System.Linq;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling;
using Vostok.Logging.File.Rolling.Strategies;

namespace Vostok.Logging.File.EventsWriting
{
    internal class EventsWriterProvider : IEventsWriterProvider
    {
        private readonly FilePath basePath;
        private readonly IFileSystem fileSystem;
        private readonly IRollingStrategyProvider rollingStrategyProvider;
        private readonly IRollingGarbageCollector garbageCollector;
        private readonly Func<FileLogSettings> settingsProvider;
        private readonly ICooldownController cooldownController;
        private readonly object sync = new object();

        private (FilePath file, FileLogSettings settings, IEventsWriter writer) currentItem;
        private bool isDisposed;
        private bool wasUsed;

        public EventsWriterProvider(
            FilePath basePath,
            IRollingStrategyProvider rollingStrategyProvider,
            IFileSystem fileSystem,
            IRollingGarbageCollector garbageCollector,
            ICooldownController cooldownController,
            Func<FileLogSettings> settingsProvider)
        {
            this.basePath = basePath;
            this.rollingStrategyProvider = rollingStrategyProvider;
            this.fileSystem = fileSystem;
            this.garbageCollector = garbageCollector;
            this.settingsProvider = settingsProvider;
            this.cooldownController = cooldownController;
        }

        public bool IsHealthy
        {
            get
            {
                lock (sync)
                    return !wasUsed || ObtainWriter() != null;
            }
        }

        public IEventsWriter ObtainWriter()
        {
            lock (sync)
            {
                if (isDisposed)
                    throw new ObjectDisposedException(GetType().Name);

                wasUsed = true;

                if (cooldownController.IsCool)
                {
                    var rollingStrategy = rollingStrategyProvider.ObtainStrategy();

                    var currentFile = rollingStrategy.GetCurrentFile(basePath.NormalizedPath);

                    var settings = settingsProvider();

                    if (currentFile != currentItem.file || ShouldReopenWriter(currentItem.settings, settings) || currentItem.writer == null)
                    {
                        currentItem.writer?.Dispose();
                        currentItem = (currentFile, settings.Clone(), fileSystem.OpenFile(currentFile.NormalizedPath, settings.FileOpenMode, settings.Encoding, settings.OutputBufferSize));
                        garbageCollector.RemoveStaleFiles(rollingStrategy.DiscoverExistingFiles(basePath.NormalizedPath).ToArray());
                    }

                    cooldownController.IncurCooldown(settings.RollingUpdateCooldown);
                }

                return currentItem.writer;
            }
        }

        public void Dispose()
        {
            lock (sync)
            {
                isDisposed = true;

                currentItem.writer?.Dispose();
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