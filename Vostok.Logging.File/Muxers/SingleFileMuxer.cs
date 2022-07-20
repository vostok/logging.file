﻿using System;
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

namespace Vostok.Logging.File.Muxers
{
    /// <summary>
    /// <para>An instance of <see cref="SingleFileMuxer"/> is responsible for writing log events from multiple <see cref="FileLog"/> instances routed to the same base file path.</para>
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
    ///     <item><description><see cref="FileLogSettings.FileSettingsUpdateCooldown"/></description></item>
    /// </list>
    /// </summary>
    internal class SingleFileMuxer : ISingleFileMuxer
    {
        private static readonly TimeSpan NewEventsTimeout = TimeSpan.FromMilliseconds(100);
        private static readonly List<Waiter> EmptyWaitersList = new List<Waiter>();

        private readonly IEventsWriterProvider writerProvider;
        private readonly ISingleFileWorker worker;

        private readonly Lazy<LogEventInfo[]> eventsBuffer;

        private readonly CancellationTokenSource workerCancellation;
        private readonly Waiter workerCancellationWaiter;
        private readonly object workerInitLock;

        private readonly AsyncManualResetEvent flushSignal;
        private readonly List<Waiter> flushWaiters;

        private readonly AtomicLong eventsLostCurrently;
        private readonly AtomicLong eventsLostSinceLastIteration;
        private volatile FileLogSettings settings;
        private volatile Task workerTask;
        private volatile Lazy<ConcurrentBoundedQueue<LogEventInfo>> eventsQueue;

        public SingleFileMuxer(
            [NotNull] IEventsWriterProviderFactory writerProviderFactory,
            [NotNull] ISingleFileWorker worker,
            [NotNull] FileLogSettings settings)
        {
            this.settings = settings;
            this.worker = worker;

            writerProvider = writerProviderFactory.CreateProvider(settings.FilePath, () => this.settings);

            eventsQueue = new Lazy<ConcurrentBoundedQueue<LogEventInfo>>(
                () => new ConcurrentBoundedQueue<LogEventInfo>(settings.EventsQueueCapacity, Math.Max(1, settings.EventsQueueCapacity / 20)),
                LazyThreadSafetyMode.ExecutionAndPublication);

            eventsBuffer = new Lazy<LogEventInfo[]>(
                () => new LogEventInfo[settings.EventsBufferCapacity],
                LazyThreadSafetyMode.ExecutionAndPublication);

            eventsLostCurrently = new AtomicLong(0);
            eventsLostSinceLastIteration = new AtomicLong(0);

            flushSignal = new AsyncManualResetEvent(true);
            flushWaiters = new List<Waiter>();

            workerInitLock = new object();
            workerCancellationWaiter = new Waiter(TaskCreationOptions.RunContinuationsAsynchronously);
            workerCancellation = new CancellationTokenSource();
            workerCancellation.Token.Register(() => workerCancellationWaiter.TrySetResult(true));
        }

        public long EventsLost => eventsLostCurrently;

        public bool TryAdd(LogEventInfo info, bool fromOwner)
        {
            if (fromOwner)
                settings = info.Settings;

            InitializeIfNeeded();

            if (settings.WaitIfQueueIsFull)
            {
                while (eventsQueue?.Value.TryAdd(info) == false)
                    Thread.Sleep(100);
                return true;
            }

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

            eventsQueue = null;

            SignalFlushWaiters(DrainFlushWaiters(), false);

            workerCancellation.Dispose();

            writerProvider.Dispose();
        }

        public async Task FlushAsync()
        {
            if (!await TryFlushAsync().ConfigureAwait(false))
                throw new FileLogException($"Failed to flush log events to file '{settings.FilePath}'.");
        }

        public Task RefreshSettingsAsync()
        {
            writerProvider.DropCooldown();

            // NOTE: We have to flush all events so that event writer refreshes its settings
            // because if event writer is currently processing some events with old settings, then new events that are
            // put to the bounded queue will be processed with old settings as well.
            return FlushAsync();
        }

        private static void SignalFlushWaiters(IEnumerable<Waiter> waiters, bool result)
        {
            foreach (var waiter in waiters)
            {
                Task.Run(() => waiter.TrySetResult(result));
            }
        }

        private async Task<bool> TryFlushAsync()
        {
            if (workerTask == null)
                return true;

            var waiter = new Waiter(TaskCreationOptions.RunContinuationsAsynchronously);

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
                        eventsQueue.Value.TryWaitForNewItemsBatchAsync(NewEventsTimeout),
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
        }
    }
}