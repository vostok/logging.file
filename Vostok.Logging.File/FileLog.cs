using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File
{
    [PublicAPI]
    public class FileLog : ILog, IDisposable
    {
        private static readonly FileLogMuxerProvider DefaultMuxerProvider = new FileLogMuxerProvider();

        private readonly FileLogMuxerProvider muxerProvider;
        private readonly SafeSettingsProvider settingsProvider;
        private readonly object handle = new object();
        private readonly FilePath filePath;

        private volatile bool isDisposed;
        private long eventsLost;
        private int wasUsed; // TODO(krait): use AtomicBoolean

        public FileLog(FileLogSettings settings)
            : this(() => settings)
        {
        }

        public FileLog(Func<FileLogSettings> settingsProvider)
        {
            this.settingsProvider = new SafeSettingsProvider(() => SettingsValidator.ValidateSettings(settingsProvider()));
            filePath = new FilePath(settingsProvider().FilePath);
        }

        ~FileLog() => Dispose();

        public long EventsLost => Interlocked.Read(ref eventsLost);

        public void Log(LogEvent @event)
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            if (@event == null)
                return;

            if (!DefaultMuxerProvider.ObtainMuxer().TryLog(@event, filePath, settingsProvider.Get(), handle, Interlocked.Increment(ref wasUsed) == 1))
                Interlocked.Increment(ref eventsLost);
        }

        public bool IsEnabledFor(LogLevel level) => settingsProvider.Get().EnabledLogLevels.Contains(level);

        // TODO(krait): implement same as in ConsoleLog
        public ILog ForContext(string context) => this;

        public Task FlushAsync() => DefaultMuxerProvider.ObtainMuxer().FlushAsync(filePath);

        public void Flush() => FlushAsync().GetAwaiter().GetResult();

        public Task FlushAllAsync() => DefaultMuxerProvider.ObtainMuxer().FlushAsync();

        public void FlushAll() => FlushAllAsync().GetAwaiter().GetResult();

        /// <inheritdoc />
        public void Dispose()
        {
            isDisposed = true;
            if (wasUsed != 0)
                DefaultMuxerProvider.ObtainMuxer().RemoveLogReference(filePath);
            GC.SuppressFinalize(this);
        }
    }
}