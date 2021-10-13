using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Muxers
{
    internal class SynchronousSingleFileMuxer : ISingleFileMuxer
    {
        private readonly object sync = new object();
        private readonly LogEventInfo[] buffer;
        private readonly CancellationTokenSource workerCancellation;
        private volatile IEventsWriterProvider writerProvider;
        private volatile FileLogSettings settings;

        public SynchronousSingleFileMuxer(
            [NotNull] IEventsWriterProviderFactory writerProviderFactory,
            [NotNull] FileLogSettings settings)
        {
            this.settings = settings;

            buffer = new LogEventInfo[1];
            workerCancellation = new CancellationTokenSource();
            writerProvider = writerProviderFactory.CreateProvider(settings.FilePath, () => this.settings);
        }

        public long EventsLost => 0;

        public bool TryAdd(LogEventInfo info, bool fromOwner)
        {
            if (fromOwner)
                settings = info.Settings;

            while (writerProvider != null)
            {
                try
                {
                    lock (sync)
                    {
                        if (writerProvider == null)
                            return false;

                        buffer[0] = info;
                        writerProvider
                            .ObtainWriterAsync(workerCancellation.Token)
                            .GetAwaiter()
                            .GetResult()
                            .WriteEvents(buffer, 1);
                    }

                    return true;
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                catch (Exception error)
                {
                    SafeConsole.ReportError($"Failure in writing log event to file '{settings.FilePath}':", error);

                    Thread.Sleep(100);
                }
            }

            return false;
        }

        public void Dispose()
        {
            workerCancellation.Cancel();

            lock (sync)
            {
                writerProvider.Dispose();
                writerProvider = null;
                workerCancellation.Dispose();
            }
        }

        public Task FlushAsync() => Task.CompletedTask;

        public Task RefreshSettingsAsync()
        {
            lock (sync)
            {
                writerProvider?.DropCooldown();
            }

            return Task.CompletedTask;
        }
    }
}