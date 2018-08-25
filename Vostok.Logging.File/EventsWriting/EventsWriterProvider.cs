using Vostok.Logging.File.Rolling;

namespace Vostok.Logging.File.EventsWriting
{
    internal class EventsWriterProvider
    {
        private readonly IRollingStrategy rollingStrategy;
        private readonly IFileSystem fileSystem;
        private readonly IRollingGarbageCollector garbageCollector;
        private (string file, IEventsWriter writer) currentItem;

        public EventsWriterProvider(IRollingStrategy rollingStrategy, IFileSystem fileSystem, IRollingGarbageCollector garbageCollector)
        {
            this.rollingStrategy = rollingStrategy;
            this.fileSystem = fileSystem;
            this.garbageCollector = garbageCollector;
        }

        // TODO(krait): reopen writer if settings changed
        public IEventsWriter ObtainWriter()
        {
            var currentFile = rollingStrategy.GetCurrentFile();

            if (currentFile != currentItem.file)
            {
                currentItem.writer?.Dispose();
                currentItem = (currentFile, fileSystem.OpenFile(currentFile));
                garbageCollector.RemoveStaleFiles(rollingStrategy.DiscoverExistingFiles());
            }

            return currentItem.writer;
        }
    }
}