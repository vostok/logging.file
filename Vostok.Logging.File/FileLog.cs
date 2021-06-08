using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Commons.Collections;
using Vostok.Commons.Threading;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Wrappers;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Muxers;

namespace Vostok.Logging.File
{
    /// <summary>
    /// <para>A log which outputs events to a file.</para>
    /// <para>
    ///     The implementation is asynchronous and thread-safe: logged messages are not immediately rendered and written to file. 
    ///     Instead, they are added to a lock-free queue which is processed by a background worker. The capacity of the queue 
    ///     can be changed in settings if a settings provider is used. In case of a queue overflow some events may be dropped.
    /// </para>
    /// <para>Use <see cref="Flush"/> or <see cref="FlushAsync"/> to ensure that logged events are written to file.</para>
    /// <para>Use <see cref="EventsLost"/> counter to see how many events were lost due to queue overflow.</para>
    /// <para>Remember to <see cref="Dispose"/> a <see cref="FileLog"/> instance when you no longer need it to close the file handle.</para>
    /// <para><see cref="Log"/> method never throws exceptions. On the other hand, <see cref="Flush"/> and <see cref="FlushAsync"/> may do so.</para>
    /// </summary>
    [PublicAPI]
    public class FileLog : ILog, IDisposable
    {
        private static readonly MultiFileMuxer DefaultMuxer;

        private readonly IMultiFileMuxer muxer;
        private readonly object muxerRegistrationLock;
        private readonly object muxerHandle;
        private readonly WeakReference muxerHandleRef;

        private readonly SafeSettingsProvider settingsProvider;
        private readonly AtomicLong eventsLost;
        private readonly CachingTransform<FileLogSettings, FilePath> filePathProvider;

        private volatile IMuxerRegistration muxerRegistration;

        static FileLog()
        {
            DefaultMuxer = new MultiFileMuxer(new SingleFileMuxerFactory());
            DefaultMuxer.InitiateOrphanedRegistrationsCleanup(TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Create a new <see cref="FileLog"/> with given static settings.
        /// </summary>
        public FileLog(FileLogSettings settings)
            : this(() => settings)
        {
        }

        /// <summary>
        /// <para>Create a new file log with the dynamic settings provided by given delegate.</para>
        /// <para>There are some subtleties about updating <see cref="FileLog"/> settings. There are three types of settings:</para>
        /// <list type="bullet">
        /// <item><description>
        /// <para>Settings that cannot be changed after the first event was logged through this <see cref="FileLog"/> instance:</para>
        /// <para><see cref="FileLogSettings.EventsQueueCapacity"/>, <see cref="FileLogSettings.EventsBufferCapacity"/></para>
        /// <para>These two options, however, have a per-file scope (rather than global).</para>
        /// </description></item>
        /// <item><description>
        /// <para>Settings that will cause re-opening of log file when changed:</para>
        /// <para>
        /// <see cref="FileLogSettings.FilePath"/>,
        /// <see cref="FileLogSettings.FileOpenMode"/>,
        /// <see cref="FileLogSettings.Encoding"/>,
        /// <see cref="FileLogSettings.OutputBufferSize"/>,
        /// <see cref="FileLogSettings.RollingStrategy"/></para>
        /// <para>These settings are set on per-file level (rather than per-instance). Only the first <see cref="FileLog"/> to log something to a file will be allowed to modify settings for that file.</para>
        /// </description></item>
        /// <item><description>
        /// <para>All other settings can be changed any time and come into effect immediately.</para> // TODO
        /// </description></item>
        /// </list>
        /// </summary>
        public FileLog(Func<FileLogSettings> settingsProvider)
            : this(DefaultMuxer, settingsProvider)
        {
        }

        internal FileLog(IMultiFileMuxer muxer, Func<FileLogSettings> settingsProvider)
        {
            this.settingsProvider = new SafeSettingsProvider(settingsProvider);
            this.settingsProvider.Get();
            this.muxer = muxer;

            muxerHandle = new object();
            muxerHandleRef = new WeakReference(muxerHandle);
            muxerRegistrationLock = new object();
            eventsLost = new AtomicLong(0);

            filePathProvider = new CachingTransform<FileLogSettings, FilePath>(
                settings => new FilePath(settings.FilePath), preventParallelProcessing: false);
        }

        /// <summary>
        /// The total number of events dropped by all <see cref="FileLog"/> instances in process due to event queue overflows.
        /// </summary>
        public static long TotalEventsLost => DefaultMuxer.EventsLost;

        /// <summary>
        /// Waits asynchronously until all currently buffered log events are actually written to their log files.
        /// </summary>
        /// <exception cref="FileLogException">Unable to flush events to at least one of the files.</exception>
        public static Task FlushAllAsync() => DefaultMuxer.FlushAsync();

        /// <summary>
        /// Waits until all currently buffered log events are actually written to their log files.
        /// </summary>
        /// <exception cref="FileLogException">Unable to flush events to at least one of the files.</exception>
        public static void FlushAll() => FlushAllAsync().GetAwaiter().GetResult();

        /// <summary>
        /// The number of events dropped by this <see cref="FileLog"/> instance due to events queue overflow.
        /// </summary>
        public long EventsLost => eventsLost;

        /// <inheritdoc />
        public void Log(LogEvent @event)
        {
            if (@event == null)
                return;

            if (!IsEnabledFor(@event.Level))
                return;

            while (true)
            {
                var settings = settingsProvider.Get();
                var file = filePathProvider.Get(settings);
                var registration = ObtainMuxerRegistration(file, settings);

                if (!muxer.TryAdd(file, new LogEventInfo(@event, settings), muxerHandleRef))
                {
                    eventsLost.Increment();
                    break;
                }

                if (registration.IsValid(file))
                    break;
            }
        }

        /// <inheritdoc />
        public bool IsEnabledFor(LogLevel level) =>
            settingsProvider.Get().EnabledLogLevels.Contains(level);

        /// <inheritdoc />
        public ILog ForContext(string context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return new SourceContextWrapper(this, context);
        }

        /// <summary>
        /// Waits asynchronously until all log events buffered for current log file are actually written.
        /// </summary>
        /// <exception cref="FileLogException">Unable to flush events to the file.</exception>
        public Task FlushAsync() => muxer.FlushAsync(filePathProvider.Get(settingsProvider.Get()));

        /// <summary>
        /// Waits until all log events buffered for current log file are actually written.
        /// </summary>
        /// <exception cref="FileLogException">Unable to flush events to the file.</exception>
        public void Flush() => FlushAsync().GetAwaiter().GetResult();

        /// <inheritdoc />
        public void Dispose()
        {
            if (muxerRegistration != null)
                try
                {
                    Flush();
                }
                catch (Exception error)
                {
                    SafeConsole.ReportError($"Failed to flush FileLog on Dispose() call for file '{settingsProvider.Get().FilePath}'.", error);
                }

            lock (muxerRegistrationLock)
            {
                muxerRegistration?.Dispose();
                muxerRegistration = null;
            }
        }

        private IMuxerRegistration ObtainMuxerRegistration(FilePath file, FileLogSettings settings)
        {
            var currentRegistration = muxerRegistration;
            if (currentRegistration != null && currentRegistration.IsValid(file))
                return currentRegistration;

            lock (muxerRegistrationLock)
            {
                if (muxerRegistration != null && muxerRegistration.IsValid(file))
                    return muxerRegistration;

                muxerRegistration?.Dispose();
                muxerRegistration = null;

                return muxerRegistration = muxer.Register(file, settings, muxerHandleRef);
            }
        }
    }
}