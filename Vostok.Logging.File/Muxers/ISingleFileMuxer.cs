using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Vostok.Logging.File.Muxers
{
    internal interface ISingleFileMuxer : IDisposable
    {
        long EventsLost { get; }

        bool TryAdd([NotNull] LogEventInfo info, bool fromOwner);

        Task FlushAsync();

        Task RefreshSettingsAsync();
    }
}