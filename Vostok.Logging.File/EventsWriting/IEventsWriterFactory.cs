using JetBrains.Annotations;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.EventsWriting
{
    internal interface IEventsWriterFactory
    {
        [CanBeNull]
        IEventsWriter TryCreateWriter(FilePath currentFile, FileLogSettings settings);
    }
}