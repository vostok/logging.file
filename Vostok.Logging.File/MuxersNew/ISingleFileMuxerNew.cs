using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Vostok.Logging.File.MuxersNew
{
    internal interface ISingleFileMuxerNew : IDisposable
    {
        long EventsLost { get; }

        bool TryAdd([NotNull] LogEventInfo info, bool fromOwner);

        Task FlushAsync();
    }
}
