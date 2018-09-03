using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Commons.Collections;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;
using Vostok.Logging.File.Rolling;
using Waiter = System.Threading.Tasks.TaskCompletionSource<bool>;

namespace Vostok.Logging.File
{
    internal class SingleFileMuxer : IDisposable
    {
        private static readonly RollingStrategyFactory RollingStrategyFactory = new RollingStrategyFactory();

        private readonly List<Waiter> flushWaiters = new List<Waiter>();
        private readonly ConcurrentBoundedQueue<LogEventInfo> events;
        private readonly EventsWriterProvider writerProvider;
        private readonly object owner;

        private long eventsLost;
        private long eventsLostSinceLastIteration;
        private volatile FileLogSettings settings;
        private volatile bool isDisposed;

        private int references;

        public SingleFileMuxer(object owner, FileLogSettings settings, IFileSystem fileSystem)
        {
            this.owner = owner;
            events = new ConcurrentBoundedQueue<LogEventInfo>(settings.EventsQueueCapacity);

            // TODO(krait): support warm change of rolling strategy type
            writerProvider = new EventsWriterProvider(
                settings.FilePath,
                RollingStrategyFactory.CreateStrategy(settings.FilePath, settings.RollingStrategy.Type, () => settings),
                fileSystem,
                new RollingGarbageCollector(fileSystem, () => settings.RollingStrategy.MaxFiles),
                () => settings);
        }

        public long EventsLost => Interlocked.Read(ref eventsLost);

        public bool TryAdd(LogEventInfo info, object instigator)
        {
            if (isDisposed)
                return false; // TODO(krait): or throw?

            if (instigator == owner && info.Settings != settings)
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
            List<Waiter> currentWaiters;
            lock (flushWaiters)
            {
                flushWaiters.RemoveAll(w => w.Task.IsCompleted);
                currentWaiters = flushWaiters.ToList();
            }

            WriteEventsInternal(temporaryBuffer);

            foreach (var waiter in currentWaiters)
            {
                waiter.TrySetResult(true);
            }
        }

        public Task FlushAsync()
        {
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

        private void WriteEventsInternal(LogEventInfo[] temporaryBuffer)
        {
            var eventsWriter = writerProvider.ObtainWriter();

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
        }

        private LogEventInfo CreateOverflowEvent(long discardedEvents)
        {
            var message = $"[{nameof(FileLog)}] Buffer overflow. {discardedEvents} log events were lost (events queue capacity = {events.Capacity}).";

            return new LogEventInfo(new LogEvent(LogLevel.Warn, DateTimeOffset.Now, message), new FileLogSettings());
        }
    }
}