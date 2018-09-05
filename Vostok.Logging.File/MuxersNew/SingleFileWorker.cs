using System;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Commons.Collections;
using Vostok.Commons.Threading;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.MuxersNew
{
    internal class SingleFileWorker : ISingleFileWorker
    {
        public async Task<bool> WritePendingEventsAsync(
            IEventsWriterProvider writerProvider,
            ConcurrentBoundedQueue<LogEventInfo> queue,
            LogEventInfo[] buffer,
            AtomicLong eventsLostCurrently,
            AtomicLong eventsLostSinceLastIteration,
            CancellationToken cancellationToken)
        {
            // TODO(iloktionov): pass cancellation token to ObtainWriterAsync()
            var writer = await writerProvider.ObtainWriterAsync().ConfigureAwait(false);
            if (writer == null)
                return false;

            var eventsToDrain = queue.Count;

            while (eventsToDrain > 0)
            {
                var eventsDrained = queue.Drain(buffer, 0, buffer.Length);
                if (eventsDrained == 0)
                    break;

                eventsToDrain -= eventsDrained;

                try
                {
                    writer.WriteEvents(buffer, eventsDrained);
                }
                catch (Exception error)
                {
                    eventsLostCurrently.Add(eventsDrained);

                    SafeConsole.ReportError("Failure in writing log events to a file:", error);

                    return false;
                }
            }

            var lostEventsAfterWriting = eventsLostCurrently.Value;
            if (lostEventsAfterWriting > eventsLostSinceLastIteration)
            {
                buffer[0] = CreateOverflowEvent(queue, lostEventsAfterWriting - eventsLostSinceLastIteration);

                try
                {
                    writer.WriteEvents(buffer, 1);
                }
                catch
                {
                    // ignored
                }

                eventsLostSinceLastIteration.Value = lostEventsAfterWriting;
            }

            return true;
        }

        private LogEventInfo CreateOverflowEvent(ConcurrentBoundedQueue<LogEventInfo> queue, long discardedEvents)
        {
            var message = $"[{nameof(FileLog)}] Buffer overflow. {discardedEvents} log events were lost (events queue capacity = {queue.Capacity}).";

            var logEvent = new LogEvent(LogLevel.Warn, DateTimeOffset.Now, message);

            return new LogEventInfo(logEvent, new FileLogSettings());
        }
    }
}