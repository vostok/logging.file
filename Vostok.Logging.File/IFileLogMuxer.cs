using System.Threading.Tasks;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File
{
    internal interface IFileLogMuxer
    {
        long EventsLost { get; }

        bool TryLog(LogEvent @event, FileLogSettings settings, FileLog fileLog);

        Task FlushAsync();

        void Close(FileLogSettings settings);
    }
}