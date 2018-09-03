using System.Threading.Tasks;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File
{
    internal interface IFileLogMuxer
    {
        long EventsLost { get; }

        bool TryLog(LogEvent @event, FileLogSettings settings, object instigator, bool firstTime);

        Task FlushAsync(string file);

        Task FlushAsync();

        void RemoveLogReference(string file);
    }
}