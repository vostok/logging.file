using System;
using System.Threading.Tasks;

namespace Vostok.Logging.File
{
    internal interface ISingleFileMuxer : IDisposable
    {
        long EventsLost { get; }

        bool IsHealthy { get; }

        void AddReference();

        Task FlushAsync();

        bool RemoveReference();

        bool TryAdd(LogEventInfo info, object instigator);

        Task TryWaitForNewItemsAsync(TimeSpan timeout);

        void WriteEvents(LogEventInfo[] temporaryBuffer);
    }
}