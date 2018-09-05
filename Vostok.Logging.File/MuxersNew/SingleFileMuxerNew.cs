using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Collections;
using Vostok.Commons.Threading;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;
using Vostok.Logging.File.Helpers;
using Waiter = System.Threading.Tasks.TaskCompletionSource<bool>;

namespace Vostok.Logging.File.MuxersNew
{
    /// <summary>
    /// <para>An instance of <see cref="SingleFileMuxerNew"/> is responsible for writing log events from multiple <see cref="FileLog"/> instances routed to the same base file path.</para>
    /// <para>Its static settings are:</para>
    /// <list type="bullet">
    ///     <item><description><see cref="FileLogSettings.FilePath"/></description></item>
    ///     <item><description><see cref="FileLogSettings.EventsQueueCapacity"/></description></item>
    ///     <item><description><see cref="FileLogSettings.EventsBufferCapacity"/></description></item>
    /// </list>
    /// <para>Its dynamic settings are:</para>
    /// <list type="bullet">
    ///     <item><description><see cref="FileLogSettings.Encoding"/></description></item>
    ///     <item><description><see cref="FileLogSettings.FileOpenMode"/></description></item>
    ///     <item><description><see cref="FileLogSettings.OutputBufferSize"/></description></item>
    ///     <item><description><see cref="FileLogSettings.RollingStrategy"/></description></item>
    ///     <item><description><see cref="FileLogSettings.RollingUpdateCooldown"/></description></item>
    /// </list>
    /// </summary>
    internal class SingleFileMuxerNew : ISingleFileMuxerNew
    {
        private static readonly TimeSpan NewEventsTimeout = TimeSpan.FromSeconds(1);
        private static readonly List<Waiter> EmptyWaitersList = new List<Waiter>();

        private readonly IEventsWriterProvider writerProvider;
        private readonly ISingleFileWorker worker;

        private readonly Lazy<ConcurrentBoundedQueue<LogEventInfo>> eventsQueue;
        private readonly Lazy<LogEventInfo[]> eventsBuffer;
        private volatile FileLogSettings settings;

        private readonly CancellationTokenSource workerCancellation;
        private readonly Waiter workerCancellationWaiter;
        private readonly object workerInitLock;
        private volatile Task workerTask;

        private readonly AsyncManualResetEvent flushSignal;
        private readonly List<Waiter> flushWaiters;

        private readonly AtomicLong eventsLostCurrently;
        private readonly AtomicLong eventsLostSinceLastIteration;

        public SingleFileMuxerNew(
            [NotNull] IEventsWriterProviderFactory writerProviderFactory,
            [NotNull] ISingleFileWorker worker,
            [NotNull] FileLogSettings settings)
        {
            this.settings = settings;
            this.worker = worker;

            writerProvider = writerProviderFactory.CreateProvider(settings.FilePath, () => this.settings);

            eventsQueue = new Lazy<ConcurrentBoundedQueue<LogEventInfo>>(
                () => new ConcurrentBoundedQueue<LogEventInfo>(settings.EventsQueueCapacity), LazyThreadSafetyMode.ExecutionAndPublication);

            eventsBuffer = new Lazy<LogEventInfo[]>(
                () => new LogEventInfo[settings.EventsBufferCapacity], LazyThreadSafetyMode.ExecutionAndPublication);

            eventsLostCurrently = new AtomicLong(0);
            eventsLostSinceLastIteration = new AtomicLong(0);

            flushSignal = new AsyncManualResetEvent(true);
            flushWaiters = new List<Waiter>();

            workerInitLock = new object();
            workerCancellationWaiter = new Waiter();
            workerCancellation = new CancellationTokenSource();
            workerCancellation.Token.Register(() => workerCancellationWaiter.TrySetResult(true));
        }

        public long EventsLost => eventsLostCurrently;

        public bool TryAdd(LogEventInfo info, bool fromOwner)
        {
            if (fromOwner)
                settings = info.Settings;

            InitializeIfNeeded();

            if (eventsQueue.Value.TryAdd(info))
                return true;

            eventsLostCurrently.Increment();

            return false;
        }

        /// <summary>
        /// Not expected to be called concurrently with TryAdd().
        /// </summary>
        public void Dispose()
        {
            TryFlushAsync().GetAwaiter().GetResult();

            workerCancellation.Cancel();

            WaitForWorkerCompletion();

            SignalFlushWaiters(DrainFlushWaiters(), false);

            workerCancellation.Dispose();

            writerProvider.Dispose();
        }

        public async Task FlushAsync()
        {
            if (!await TryFlushAsync().ConfigureAwait(false))
                throw new FileLogException($"Failed to flush log events to file '{settings.FilePath}'.");
        }

        private async Task<bool> TryFlushAsync()
        {
            if (workerTask == null)
                return true;

            if (await writerProvider.ObtainWriterAsync().ConfigureAwait(false) == null)
                return false;

            var waiter = new Waiter();

            lock (flushWaiters)
                flushWaiters.Add(waiter);

            flushSignal.Set();

            return await waiter.Task.ConfigureAwait(false);
        }

        private void InitializeIfNeeded()
        {
            if (workerTask != null)
                return;

            lock (workerInitLock)
            {
                if (workerTask != null)
                    return;

                workerTask = Task.Run(WorkerRoutineAsync);
            }
        }

        private async Task WorkerRoutineAsync()
        {
            while (!workerCancellation.IsCancellationRequested)
            {
                try
                {
                    var currentWaiters = DrainFlushWaiters();

                    var iterationResult = await worker.WritePendingEventsAsync(
                            writerProvider,
                            eventsQueue.Value,
                            eventsBuffer.Value,
                            eventsLostCurrently,
                            eventsLostSinceLastIteration,
                            workerCancellation.Token)
                        .ConfigureAwait(false);

                    SignalFlushWaiters(currentWaiters, iterationResult);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception error)
                {
                    SafeConsole.ReportError($"Failure in writing log events to file '{settings.FilePath}':", error);

                    await Task.Delay(100).ConfigureAwait(false);
                }

                await Task.WhenAny(
                        eventsQueue.Value.TryWaitForNewItemsAsync(NewEventsTimeout),
                        flushSignal.WaitAsync(),
                        workerCancellationWaiter.Task)
                    .ConfigureAwait(false);

                flushSignal.Reset();
            }
        }

        private List<Waiter> DrainFlushWaiters()
        {
            List<Waiter> currentWaiters;

            lock (flushWaiters)
            {
                flushWaiters.RemoveAll(w => w.Task.IsCompleted);
                currentWaiters = flushWaiters.Count > 0 ? flushWaiters.ToList() : EmptyWaitersList;
            }

            return currentWaiters;
        }

        private static void SignalFlushWaiters(IEnumerable<Waiter> waiters, bool result)
        {
            foreach (var waiter in waiters)
            {
                Task.Run(() => waiter.TrySetResult(result));
            }
        }

        private void WaitForWorkerCompletion()
        {
            var currentWorkerTask = workerTask;
            if (currentWorkerTask == null)
                return;

            try
            {
                currentWorkerTask
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception error)
            {
                SafeConsole.ReportError($"An error occured while finishing background worker for file '{settings.FilePath}':", error);
            }
            finally
            {
                workerTask = null;
            }
        }
    }
}
