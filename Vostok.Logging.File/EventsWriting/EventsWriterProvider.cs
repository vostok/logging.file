using System;
using System.Linq;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Rolling;

namespace Vostok.Logging.File.EventsWriting
{
    internal class EventsWriterProvider : IDisposable
    {
        private readonly FilePath basePath;
        private readonly IFileSystem fileSystem;
        private readonly RollingStrategyProvider rollingStrategyProvider;
        private readonly IRollingGarbageCollector garbageCollector;
        private readonly Func<FileLogSettings> settingsProvider;
        private readonly object sync = new object();

        private (string file, IEventsWriter writer) currentItem;
        private bool isDisposed;

        public EventsWriterProvider(FilePath basePath, RollingStrategyProvider rollingStrategyProvider, IFileSystem fileSystem, IRollingGarbageCollector garbageCollector, Func<FileLogSettings> settingsProvider)
        {
            this.basePath = basePath;
            this.rollingStrategyProvider = rollingStrategyProvider;
            this.fileSystem = fileSystem;
            this.garbageCollector = garbageCollector;
            this.settingsProvider = settingsProvider;
        }

        // TODO(krait): reopen writer if settings changed
        public IEventsWriter ObtainWriter()
        {
            lock (sync)
            {
                if (isDisposed)
                    throw new ObjectDisposedException(GetType().Name);

                var rollingStrategy = rollingStrategyProvider.ObtainStrategy();

                var currentFile = rollingStrategy.GetCurrentFile(basePath.NormalizedPath);

                if (currentFile != currentItem.file)
                {
                    var settings = settingsProvider();

                    currentItem.writer?.Dispose();
                    currentItem = (currentFile, fileSystem.OpenFile(currentFile, settings.FileOpenMode, settings.Encoding, settings.OutputBufferSize));
                    garbageCollector.RemoveStaleFiles(rollingStrategy.DiscoverExistingFiles(basePath.NormalizedPath).ToArray());
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
    }
}