using System.Threading.Tasks;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Muxers
{
    internal interface IFileLogMuxer
    {
        long EventsLost { get; }

        bool TryLog(LogEvent @event, FilePath filePath, FileLogSettings settings, object instigator, bool firstTime);

        Task FlushAsync(FilePath file);

        Task FlushAsync();

        void RemoveLogReference(FilePath file);
    }
}