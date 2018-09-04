using System.Threading.Tasks;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;

namespace Vostok.Logging.File
{
    internal interface IFileLogMuxer
    {
        long EventsLost { get; }

        bool TryLog(LogEvent @event, FilePath filePath, FileLogSettings settings, IEventsWriterProvider eventsWriterProvider, object instigator, bool firstTime);

        Task FlushAsync(FilePath file);

        Task FlushAsync();

        void RemoveLogReference(FilePath file);
    }
}