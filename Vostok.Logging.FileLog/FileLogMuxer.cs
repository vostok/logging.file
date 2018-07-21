using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vostok.Commons.Synchronization;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Core;
using Vostok.Logging.Core.ConversionPattern;
using Vostok.Logging.FileLog.Configuration;

namespace Vostok.Logging.FileLog
{
    internal static class FileLogMuxer
    {
        private static readonly ConcurrentDictionary<FileLogConfigProvider, FileLogState> logStates =
            new ConcurrentDictionary<FileLogConfigProvider, FileLogState>();

        private static readonly AtomicBoolean isInitialized = new AtomicBoolean(false);

        private static readonly ConversionPatternRenderer conversionPatternRenderer = new ConversionPatternRenderer();

        public static void Log(FileLogConfigProvider provider, LogEvent @event)
        {
            if (!isInitialized)
                Initialize();

            FileLogState state;

            do
            {
                state = logStates.GetOrAdd(provider, s => new FileLogState(null, provider.Settings));
            } while (state.IsClosedForWriting);

            state.Events.TryAdd(@event);
        }

        private static void StartLoggingTask()
        {
            Task.Run(
                async () =>
                {
                    while (true)
                    {
                        // TODO(krait): monitor new instances
                        foreach (var pair in logStates)
                            LogEventsForInstance(pair.Key, pair.Value);

                        if (logStates.Select(pair => pair.Value).All(e => e.Events.Count == 0))
                            await Task.WhenAny(logStates.Select(pair => pair.Value).Select(e => e.Events.WaitForNewItemsAsync()));
                    }
                });
        }

        private static void LogEventsForInstance(FileLogConfigProvider configProvider, FileLogState state)
        {
            var newSettings = configProvider.Settings;

            if (!ReferenceEquals(newSettings, state.Settings))
            {
                state.IsClosedForWriting = true;
                logStates[configProvider] = new FileLogState(state, newSettings);
            }

            var eventsCount = state.Events.Drain(state.TemporaryBuffer, 0, state.TemporaryBuffer.Length);
            for (var i = 0; i < eventsCount; i++)
            {
                var currentEvent = state.TemporaryBuffer[i];
                conversionPatternRenderer.Render(state.Settings.ConversionPattern, currentEvent, state.TextWriter);
            }

            state.TextWriter.Flush();
        }

        private static void Initialize()
        {
            if (isInitialized.TrySetTrue())
                StartLoggingTask();
        }

        private class FileLogState
        {
            public FileLogState(FileLogState oldState, FileLogSettings settings)
            {
                Settings = settings;

                if (oldState?.Settings.EventsQueueCapacity == settings.EventsQueueCapacity)
                {
                    TemporaryBuffer = oldState.TemporaryBuffer;
                    Events = oldState.Events;
                }
                else
                {
                    TemporaryBuffer = new LogEvent[settings.EventsQueueCapacity];
                    Events = new BoundedBuffer<LogEvent>(settings.EventsQueueCapacity);
                }

                TextWriter = CreateFileWriter();
            }

            public FileLogSettings Settings { get; }

            public LogEvent[] TemporaryBuffer { get; }

            public BoundedBuffer<LogEvent> Events { get; }

            public TextWriter TextWriter { get; }

            public bool IsClosedForWriting { get; set; }

            private TextWriter CreateFileWriter() // TODO(krait): use RollingStrategy
            {
                var fileMode = Settings.FileOpenMode == FileOpenMode.Rewrite ? FileMode.OpenOrCreate : FileMode.Append;

                var stream = File.Open(Settings.FilePath, fileMode, FileAccess.Write, FileShare.Read);
                return new StreamWriter(stream, Settings.Encoding);
            }
        }
    }
}