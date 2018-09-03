using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Commons.Threading;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File
{
    internal class FileLogMuxer : IFileLogMuxer
    {
        private static readonly TimeSpan NewEventsTimeout = TimeSpan.FromSeconds(1);

        private readonly AsyncManualResetEvent flushSignal = new AsyncManualResetEvent(true);
        private readonly object initLock = new object();
        private readonly IFileSystem fileSystem;

        private readonly ConcurrentDictionary<string, SingleFileMuxer> muxersByFile = new ConcurrentDictionary<string, SingleFileMuxer>();
        private readonly LogEventInfo[] temporaryBuffer;

        private bool isInitialized;

        public FileLogMuxer(int temporaryBufferCapacity, IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
            temporaryBuffer = new LogEventInfo[temporaryBufferCapacity];
        }

        public long EventsLost => muxersByFile.Sum(pair => pair.Value.EventsLost);

        public bool TryLog(LogEvent @event, FileLogSettings settings, object instigator, bool firstTime)
        {
            if (!isInitialized)
                Initialize();

            var eventInfo = new LogEventInfo(@event, settings);
            var newMuxer = new Lazy<SingleFileMuxer>(() => new SingleFileMuxer(instigator, settings, fileSystem), LazyThreadSafetyMode.ExecutionAndPublication);
            var muxer = muxersByFile.GetOrAdd(settings.FilePath, _ => newMuxer.Value);

            if (firstTime)
                muxer.AddReference();

            if (!muxer.TryAdd(eventInfo, instigator))
                return false;

            if (newMuxer.IsValueCreated && muxer == newMuxer.Value)
                flushSignal.Set();

            return true;
        }

        public Task FlushAsync(string file)
        {
            if (!muxersByFile.TryGetValue(file, out var muxer))
                return Task.CompletedTask;

            var waiter = muxer.FlushAsync();

            flushSignal.Set();

            return waiter;
        }

        public Task FlushAsync() => Task.WhenAll(muxersByFile.Select(m => m.Value.FlushAsync()));

        public void RemoveLogReference(string file)
        {
            if (!muxersByFile.TryGetValue(file, out var muxer))
                return;

            if (muxer.RemoveReference())
            {
                muxer.Dispose();
                muxersByFile.TryRemove(file, out var _);
            }
        }

        private void StartLoggingTask()
        {
            Task.Run(
                async () =>
                {
                    while (true)
                    {
                        try
                        {
                            foreach (var pair in muxersByFile)
                            {
                                pair.Value.WriteEvents(temporaryBuffer);
                            }

                            var waitTasks = muxersByFile.Select(pair => pair.Value.TryWaitForNewItemsAsync(NewEventsTimeout));
                            await Task.WhenAny(waitTasks.Concat(flushSignal.WaitAsync()));
                            flushSignal.Reset();
                        }
                        catch (Exception error)
                        {
                            SafeConsole.TryWriteLine(error);
                            await Task.Delay(100);
                        }
                    }
                });
        }

        private void Initialize()
        {
            lock (initLock)
                if (!isInitialized)
                {
                    StartLoggingTask();
                    isInitialized = true;
                }
        }
    }
}