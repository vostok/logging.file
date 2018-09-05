using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.EventsWriting
{
    internal class EventsWriterFactory : IEventsWriterFactory
    {
        private readonly IFileSystem fileSystem;

        public EventsWriterFactory(IFileSystem fileSystem) => 
            this.fileSystem = fileSystem;

        public IEventsWriter CreateWriter(FilePath currentFile, FileLogSettings settings) =>
            new EventsWriter(fileSystem.OpenFile(currentFile, settings.FileOpenMode, settings.Encoding, settings.OutputBufferSize));
    }
}