using System.IO;
using System.Threading;
using Vostok.Commons.Collections;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File
{
    internal class FileLogState
    {
        private volatile int writers;
        private volatile bool isClosedForWriting;
        private volatile int eventsLost;

        public FileLogState(FileLogSettings settings, FileLog owner)
        {
            Settings = settings;
            Owner = owner;

            TemporaryBuffer = new LogEventInfo[settings.EventsQueueCapacity];
            Events = new ConcurrentBoundedQueue<LogEventInfo>(settings.EventsQueueCapacity);
        }

        public FileLogSettings Settings { get; }
        public FileLog Owner { get; }

        public LogEventInfo[] TemporaryBuffer { get; }

        public ConcurrentBoundedQueue<LogEventInfo> Events { get; }

        public EventsWriter ObtainWriter()
        {
            var currentFilePath = Settings.FilePath; // TODO(krait): obtain from rolling strategy

            if (writerState.writer == null || writerState.path != currentFilePath)
            {
                writerState.writer?.Dispose();
                writerState = (currentFilePath, new EventsWriter(new StreamWriter(currentFilePath))); // TODO(krait): create stream with factory
            }

            return writerState.writer;
        }

        private (string path, EventsWriter writer) writerState;

        public int EventsLost => eventsLost;

        public void WaitForNoWriters()
        {
            var spinWait = new SpinWait();

            while (writers > 0)
                spinWait.SpinOnce();
        }

        public bool TryAddEvent(LogEventInfo eventInfo)
        {
            Interlocked.Increment(ref writers);

            var willAdd = !isClosedForWriting;
            if (willAdd)
            {
                if (!Events.TryAdd(eventInfo))
                    Interlocked.Increment(ref eventsLost);
            }

            Interlocked.Decrement(ref writers);

            return willAdd;
        }

        public void CloseForWriting() => isClosedForWriting = true;
    }
}