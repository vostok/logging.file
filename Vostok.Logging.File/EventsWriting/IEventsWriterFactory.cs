using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.EventsWriting
{
    internal interface IEventsWriterFactory
    {
        IEventsWriter CreateWriter(FilePath currentFile, FileLogSettings settings);
    }
}