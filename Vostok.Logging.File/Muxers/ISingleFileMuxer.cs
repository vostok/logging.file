using System;
using System.Threading.Tasks;

namespace Vostok.Logging.File.Muxers
{
    internal interface ISingleFileMuxer : IDisposable
    {
        long EventsLost { get; }

        void AddReference();

        Task FlushAsync();

        bool RemoveReference();

        bool TryAdd(LogEventInfo info, object instigator);

        Task TryWaitForNewItemsAsync(TimeSpan timeout);

        void WriteEvents(LogEventInfo[] temporaryBuffer);
    }
}