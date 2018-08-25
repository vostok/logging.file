using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vostok.Commons.Threading;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;
using Waiter = System.Threading.Tasks.TaskCompletionSource<bool>;

namespace Vostok.Logging.File
{
    internal class FileLogMuxer : IFileLogMuxer
    {
        private static readonly TimeSpan NewEventsTimeout = TimeSpan.FromSeconds(1);

        private readonly AsyncManualResetEvent flushSignal = new AsyncManualResetEvent(true);
        private readonly List<Waiter> flushWaiters = new List<Waiter>();
        private readonly object initLock = new object();

        private readonly LogEventInfo[] temporaryBuffer;
        private readonly ConcurrentDictionary<string, LogState> statesByFile = new ConcurrentDictionary<string, LogState>();

        private bool isInitialized;

        public FileLogMuxer(int temporaryBufferCapacity) => 
            temporaryBuffer = new LogEventInfo[temporaryBufferCapacity];

        public long EventsLost => statesByFile.Sum(pair => pair.Value.EventsLost);

        public bool TryLog(LogEvent @event, FileLogSettings settings, FileLog instigator)
        {
            if (!isInitialized)
                Initialize();

            var eventInfo = new LogEventInfo(@event, settings);
            var newState = new LogState(instigator, settings); // TODO(krait): lazy
            var state = statesByFile.GetOrAdd(settings.FilePath, newState);

            if (!state.TryAdd(eventInfo, settings, instigator))
                return false;

            if (state != newState)
                flushSignal.Set();

            return true;
        }

        // TODO(krait): flush by file?
        public Task FlushAsync()
        {
            var waiter = new Waiter();

            lock (flushWaiters)
                flushWaiters.Add(waiter);

            flushSignal.Set();

            return waiter.Task;
        }

        public void Close(FileLogSettings settings)
        {
            if (!statesByFile.TryGetValue(settings.FilePath, out var state))
                return;

            state.Close();
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
                            List<Waiter> currentWaiters;
                            lock (flushWaiters)
                            {
                                flushWaiters.RemoveAll(w => w.Task.IsCompleted);
                                currentWaiters = flushWaiters.ToList();
                            }

                            LogEvents();

                            foreach (var waiter in currentWaiters)
                            {
                                waiter.TrySetResult(true);
                            }

                            var waitTasks = statesByFile.Select(pair => pair.Value.TryWaitForNewItemsAsync(NewEventsTimeout));
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

        private void LogEvents()
        {
            foreach (var pair in statesByFile)
            {
                pair.Value.WriteEvents(temporaryBuffer);
            }
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