using System;
using System.Linq;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Rolling;
using Vostok.Logging.File.Rolling.Strategies;

namespace Vostok.Logging.File.EventsWriting
{
    internal class EventsWriterProvider : IDisposable
    {
        private readonly string baseFilePath;
        private readonly IRollingStrategy rollingStrategy;
        private readonly IFileSystem fileSystem;
        private readonly IRollingGarbageCollector garbageCollector;
        private readonly Func<FileLogSettings> settingsProvider;
        private readonly object sync = new object();

        private (string file, IEventsWriter writer) currentItem;
        private bool isDisposed;

        public EventsWriterProvider(string baseFilePath, IRollingStrategy rollingStrategy, IFileSystem fileSystem, IRollingGarbageCollector garbageCollector, Func<FileLogSettings> settingsProvider)
        {
            this.baseFilePath = baseFilePath;
            this.rollingStrategy = rollingStrategy;
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

                var currentFile = rollingStrategy.GetCurrentFile(baseFilePath);

                if (currentFile != currentItem.file)
                {
                    var settings = settingsProvider();

                    currentItem.writer?.Dispose();
                    currentItem = (currentFile, fileSystem.OpenFile(currentFile, settings.FileOpenMode, settings.Encoding, settings.OutputBufferSize));
                    garbageCollector.RemoveStaleFiles(rollingStrategy.DiscoverExistingFiles(baseFilePath).ToArray());
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