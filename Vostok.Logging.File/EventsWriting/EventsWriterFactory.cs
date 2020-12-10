using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.EventsWriting
{
    internal class EventsWriterFactory : IEventsWriterFactory
    {
        private readonly IFileSystem fileSystem;

        public EventsWriterFactory(IFileSystem fileSystem) =>
            this.fileSystem = fileSystem;

        public IEventsWriter TryCreateWriter(FilePath currentFile, FileLogSettings settings)
        {
            var fileWriter = fileSystem.TryOpenFile(currentFile, settings.FileOpenMode, settings.FileShare, settings.SupportMultipleProcesses, settings.Encoding, settings.OutputBufferSize);
            if (fileWriter == null)
                return null;

            return new EventsWriter(fileWriter);
        }
    }
}