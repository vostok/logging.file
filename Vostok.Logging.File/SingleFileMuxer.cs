using System;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Commons.Collections;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;
using Vostok.Logging.File.Rolling;

namespace Vostok.Logging.File
{
    internal class SingleFileMuxer
    {
        private static readonly RollingStrategyFactory RollingStrategyFactory = new RollingStrategyFactory();

        private readonly object closeLock = new object();
        private readonly ConcurrentBoundedQueue<LogEventInfo> events;
        private readonly EventsWriterProvider writerProvider;
        private readonly FileLog owner;

        private long eventsLost;
        private long eventsLostSinceLastIteration;
        private volatile FileLogSettings settings;
        private bool closed;

        public SingleFileMuxer(FileLog owner, FileLogSettings settings)
        {
            this.owner = owner;
            events = new ConcurrentBoundedQueue<LogEventInfo>(settings.EventsQueueCapacity);

            // TODO(krait): support warm change of rolling strategy type
            var fileSystem = new FileSystem();
            writerProvider = new EventsWriterProvider(
                settings.FilePath,
                RollingStrategyFactory.CreateStrategy(settings.RollingStrategy.Type, () => settings),
                fileSystem,
                new RollingGarbageCollector(fileSystem, () => settings.RollingStrategy.MaxFiles),
                () => settings);
        }

        public long EventsLost => Interlocked.Read(ref eventsLost);

        public bool TryAdd(LogEventInfo info, FileLogSettings settings, FileLog instigator)
        {
            if (instigator == owner && settings != this.settings)
            {
                this.settings = settings;
            }

            if (events.TryAdd(info))
                return true;

            Interlocked.Increment(ref eventsLost);
            return false;
        }

        public void WriteEvents(LogEventInfo[] temporaryBuffer)
        {
            lock (closeLock)
            {
                if (closed)
                    return;

                var eventsWriter = writerProvider.ObtainWriter();

                var eventsCount = events.Drain(temporaryBuffer, 0, temporaryBuffer.Length);

                try
                {
                    eventsWriter.WriteEvents(temporaryBuffer, eventsCount);
                }
                catch
                {
                    Interlocked.Add(ref eventsLost, eventsCount);
                    throw;
                }

                var currentEventsLost = EventsLost;
                if (currentEventsLost > eventsLostSinceLastIteration)
                {
                    temporaryBuffer[0] = CreateOverflowEvent(currentEventsLost - eventsLostSinceLastIteration);
                    eventsWriter.WriteEvents(temporaryBuffer, 1);

                    eventsLostSinceLastIteration = currentEventsLost;
                }
            }
        }

        public Task TryWaitForNewItemsAsync(TimeSpan timeout) => events.TryWaitForNewItemsAsync(timeout);

        public void Close()
        {
            lock (closeLock)
            {
                if (closed)
                    return;

                closed = true;
                writerProvider.ObtainWriter().Dispose();
            }
        }

        private LogEventInfo CreateOverflowEvent(long discardedEvents)
        {
            var message = $"[{nameof(FileLog)}] Buffer overflow. {discardedEvents} log events were lost (events queue capacity = {events.Capacity}).";

            return new LogEventInfo(new LogEvent(LogLevel.Warn, DateTimeOffset.Now, message), new FileLogSettings());
        }
    }
}