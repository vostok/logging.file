using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Commons.Collections;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;
using Vostok.Logging.File.Helpers;
using Waiter = System.Threading.Tasks.TaskCompletionSource<bool>;

namespace Vostok.Logging.File.Muxers
{
    internal class SingleFileMuxer : ISingleFileMuxer
    {
        private readonly List<Waiter> flushWaiters = new List<Waiter>();
        private readonly ConcurrentBoundedQueue<LogEventInfo> events;
        private readonly IEventsWriterProvider writerProvider;
        private readonly FilePath filePath;
        private readonly object owner;

        private long eventsLost;
        private long eventsLostSinceLastIteration;
        private volatile FileLogSettings settings;
        private volatile bool isDisposed;
        private volatile bool wasUsed;

        private int references;

        public SingleFileMuxer(object owner, FilePath filePath, FileLogSettings settings, IEventsWriterProviderFactory writerProviderFactory)
        {
            this.owner = owner;
            this.filePath = filePath;
            this.settings = settings;

            writerProvider = writerProviderFactory.CreateProvider(filePath, () => this.settings);
            events = new ConcurrentBoundedQueue<LogEventInfo>(settings.EventsQueueCapacity);
        }

        public long EventsLost => Interlocked.Read(ref eventsLost);

        public bool IsHealthy => writerProvider.IsHealthy;

        public bool TryAdd(LogEventInfo info, object instigator)
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            wasUsed = true;

            if (instigator == owner && info.Settings != settings) // TODO(krait): Bug: properties of cached instance can be modified from outside.
            {
                settings = info.Settings;
            }

            if (events.TryAdd(info))
                return true;

            Interlocked.Increment(ref eventsLost);
            return false;
        }

        public void WriteEvents(LogEventInfo[] temporaryBuffer)
        {
            wasUsed = true;

            try
            {
                List<Waiter> currentWaiters;
                lock (flushWaiters)
                {
                    flushWaiters.RemoveAll(w => w.Task.IsCompleted);
                    currentWaiters = flushWaiters.ToList();
                }

                if (TryWriteEventsInternal(temporaryBuffer))
                {
                    foreach (var waiter in currentWaiters)
                    {
                        waiter.TrySetResult(true);
                    }
                }
            }
            catch (Exception error)
            {
                SafeConsole.ReportError($"Failure in writing log events to file '{filePath.NormalizedPath}':", error);
            }
        }

        public Task FlushAsync()
        {
            if (!wasUsed)
                return Task.CompletedTask;

            var waiter = new Waiter();

            lock (flushWaiters)
                flushWaiters.Add(waiter);

            return waiter.Task;
        }

        public Task TryWaitForNewItemsAsync(TimeSpan timeout) => events.TryWaitForNewItemsAsync(timeout);

        public void AddReference() => Interlocked.Increment(ref references);

        public bool RemoveReference() => Interlocked.Decrement(ref references) == 0;

        public void Dispose()
        {
            isDisposed = true;
            FlushAsync().GetAwaiter().GetResult();
            writerProvider.Dispose();
        }

        private bool TryWriteEventsInternal(LogEventInfo[] temporaryBuffer)
        {
            var eventsWriter = writerProvider.ObtainWriter();
            if (eventsWriter == null)
                return false;

            var eventsToDrain = events.Count;

            while (eventsToDrain > 0)
            {
                var eventsDrained = events.Drain(temporaryBuffer, 0, temporaryBuffer.Length);
                if (eventsDrained == 0)
                    break;
                eventsToDrain -= eventsDrained;

                try
                {
                    eventsWriter.WriteEvents(temporaryBuffer, eventsDrained);
                }
                catch
                {
                    Interlocked.Add(ref eventsLost, eventsDrained);
                    throw;
                }
            }

            var currentEventsLost = EventsLost;
            if (currentEventsLost > eventsLostSinceLastIteration)
            {
                temporaryBuffer[0] = CreateOverflowEvent(currentEventsLost - eventsLostSinceLastIteration);
                eventsWriter.WriteEvents(temporaryBuffer, 1);

                eventsLostSinceLastIteration = currentEventsLost;
            }

            return true;
        }

        private LogEventInfo CreateOverflowEvent(long discardedEvents)
        {
            var message = $"[{nameof(FileLog)}] Buffer overflow. {discardedEvents} log events were lost (events queue capacity = {events.Capacity}).";

            return new LogEventInfo(new LogEvent(LogLevel.Warn, DateTimeOffset.Now, message), new FileLogSettings());
        }
    }
}
