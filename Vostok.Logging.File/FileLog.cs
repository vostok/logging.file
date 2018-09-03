using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Threading;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Wrappers;
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

        private AtomicBoolean wasUsed = new AtomicBoolean(false);
        private volatile bool isDisposed;
        private long eventsLost;

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

            if (!DefaultMuxerProvider.ObtainMuxer().TryLog(@event, filePath, settingsProvider.Get(), handle, wasUsed.TrySetTrue()))
                Interlocked.Increment(ref eventsLost);
        }

        public bool IsEnabledFor(LogLevel level) => settingsProvider.Get().EnabledLogLevels.Contains(level);

        public ILog ForContext(string context) =>
            context == null ? (ILog)this : new SourceContextWrapper(this, context);

        public Task FlushAsync() => DefaultMuxerProvider.ObtainMuxer().FlushAsync(filePath);

        public void Flush() => FlushAsync().GetAwaiter().GetResult();

        public Task FlushAllAsync() => DefaultMuxerProvider.ObtainMuxer().FlushAsync();

        public void FlushAll() => FlushAllAsync().GetAwaiter().GetResult();

        /// <inheritdoc />
        public void Dispose()
        {
            isDisposed = true;
            if (wasUsed)
                DefaultMuxerProvider.ObtainMuxer().RemoveLogReference(filePath);
            GC.SuppressFinalize(this);
        }
    }
}