using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Threading;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Wrappers;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Muxers;

namespace Vostok.Logging.File
{
    /// <summary>
    /// <para>A log which outputs events to a file.</para>
    /// <para>
    ///     The implementation is asynchronous: logged messages are not immediately rendered and written to file. 
    ///     Instead, they are added to a queue which is processed by a background worker. The capacity of the queue 
    ///     can be changed in settings if a settings provider is used. In case of a queue overflow some events may be dropped.
    /// </para>
    /// </summary>
    [PublicAPI]
    public class FileLog : ILog, IDisposable
    {
        private static readonly FileLogMuxerProvider DefaultMuxerProvider = new FileLogMuxerProvider(new SingleFileMuxerFactory(new EventsWriterProviderFactory()));

        private readonly SafeSettingsProvider settingsProvider;
        private readonly IFileLogMuxerProvider muxerProvider;
        private readonly object handle = new object();
        private readonly FilePath filePath;

        private AtomicBoolean wasUsed = new AtomicBoolean(false);
        private volatile bool isDisposed;
        private long eventsLost;

        /// <summary>
        /// Create a new console log with the given settings.
        /// </summary>
        public FileLog(FileLogSettings settings)
            : this(() => settings)
        {
        }

        /// <summary>
        /// <para>Create a new console log with the given settings provider.</para>
        /// <para>There are some subtleties about updating <see cref="FileLog"/> settings. There are three types of settings:</para>
        /// <list type="bullet">
        /// <item><description>
        /// <para>Settings that cannot be changed after the first event was logged through this <see cref="FileLog"/> instance:</para>
        /// <para><see cref="FileLogSettings.EventsQueueCapacity"/>, <see cref="FileLogSettings.EventsBufferCapacity"/></para>
        /// </description></item>
        /// <item><description>
        /// <para>Settings that will cause re-opening of log file when changed:</para>
        /// <para><see cref="FileLogSettings.FileOpenMode"/>, <see cref="FileLogSettings.Encoding"/>, <see cref="FileLogSettings.OutputBufferSize"/>, <see cref="FileLogSettings.RollingStrategy"/></para>
        /// <para>These settings are set on per-file level (rather than per-instance). Only the first <see cref="FileLog"/> to log something to a file will be allowed to modify settings for that file.</para>
        /// </description></item>
        /// <item><description>
        /// <para>All other settings can be changed any time and come into effect immediately.</para>
        /// </description></item>
        /// </list>
        /// </summary>
        public FileLog(Func<FileLogSettings> settingsProvider)
            : this(DefaultMuxerProvider, settingsProvider)
        {
        }

        internal FileLog(IFileLogMuxerProvider muxerProvider, Func<FileLogSettings> settingsProvider)
        {
            this.muxerProvider = muxerProvider;
            this.settingsProvider = new SafeSettingsProvider(() => SettingsValidator.ValidateSettings(settingsProvider()));
            filePath = settingsProvider().FilePath;
        }

        /// <inheritdoc />
        ~FileLog() => Dispose();

        /// <summary>
        /// The total number of events dropped by all <see cref="FileLog"/> instances in process due to events queue overflow.
        /// </summary>
        public static long TotalEventsLost => DefaultMuxerProvider.ObtainMuxer().EventsLost;

        /// <summary>
        /// Waits until all currently buffered log events are actually written to their log files.
        /// </summary>
        public static Task FlushAllAsync() => DefaultMuxerProvider.ObtainMuxer().FlushAsync();

        /// <summary>
        /// Waits until all currently buffered log events are actually written to their log files.
        /// </summary>
        public static void FlushAll() => FlushAllAsync().GetAwaiter().GetResult();

        /// <summary>
        /// The number of events dropped by this <see cref="FileLog"/> instance due to events queue overflow.
        /// </summary>
        public long EventsLost => Interlocked.Read(ref eventsLost);

        /// <inheritdoc />
        public void Log(LogEvent @event)
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            if (@event == null)
                return;

            if (!muxerProvider.ObtainMuxer().TryLog(@event, filePath, settingsProvider.Get(), handle, wasUsed.TrySetTrue()))
                Interlocked.Increment(ref eventsLost);
        }

        /// <inheritdoc />
        public bool IsEnabledFor(LogLevel level) => settingsProvider.Get().EnabledLogLevels.Contains(level);

        /// <summary>
        /// Returns a log based on this <see cref="FileLog"/> instance that puts given <paramref name="context" /> string into <see cref="F:Vostok.Logging.Abstractions.WellKnownProperties.SourceContext" /> property of all logged events.
        /// </summary>
        public ILog ForContext(string context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return new SourceContextWrapper(this, context);
        }

        /// <summary>
        /// Waits until all log events buffered for current log file are actually written.
        /// </summary>
        public Task FlushAsync() => DefaultMuxerProvider.ObtainMuxer().FlushAsync(filePath);

        /// <summary>
        /// Waits until all log events buffered for current log file are actually written.
        /// </summary>
        public void Flush() => FlushAsync().GetAwaiter().GetResult();

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