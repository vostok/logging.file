using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Collections;
using Vostok.Commons.Threading;
using Vostok.Logging.File.EventsWriting;

namespace Vostok.Logging.File.MuxersNew
{
    internal interface ISingleFileWorker
    {
        Task<bool> WritePendingEventsAsync(
            [NotNull] IEventsWriterProvider writerProvider,
            [NotNull] ConcurrentBoundedQueue<LogEventInfo> queue,
            [NotNull] LogEventInfo[] buffer,
            [NotNull] AtomicLong eventsLostCurrently,
            [NotNull] AtomicLong eventsLostSinceLastIteration,
            CancellationToken cancellationToken);
    }
}