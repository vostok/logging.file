using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vostok.Logging.Abstractions;
using Vostok.Logging.FileLog.Configuration;

namespace Vostok.Logging.FileLog
{
    internal static class FileLogMuxer
    {
        private static readonly bool IsInitialized; // TODO(krait): double-checked locking

        private static readonly ConcurrentDictionary<string, FileLogState> LogStatesByFile = new ConcurrentDictionary<string, FileLogState>();
        private static readonly ConcurrentDictionary<string, FileLogSettings> LogSettingsByFile = new ConcurrentDictionary<string, FileLogSettings>();

        private static readonly IEqualityComparer<FileLogSettings> SettingsComparer = new FileLogSettingsComparer();

        public static void Log(LogEvent @event, FileLogSettings settings, FileLog instigator)
        {
            if (!IsInitialized)
                Initialize();

            var eventInfo = new LogEventInfo(@event, settings);

            var newState = new FileLogState(settings, instigator); // TODO(krait): wrap buffers with lazy

            var currentState = LogStatesByFile.GetOrAdd(settings.FilePath, newState);

            if (ReferenceEquals(currentState.Owner, instigator))
                LogSettingsByFile[settings.FilePath] = settings;

            while (!currentState.TryAddEvent(eventInfo))
                currentState = LogStatesByFile[settings.FilePath];
        }

        private static void StartLoggingTask()
        {
            Task.Run(
                async () =>
                {
                    while (true)
                    {
                        foreach (var pair in LogStatesByFile)
                            LogEvents(pair.Key);

                        if (LogStatesByFile.All(pair => pair.Value.Events.Count == 0)) // TODO(krait): unblock on new log state or settings change
                            await Task.WhenAny(LogStatesByFile.Select(pair => pair.Value.Events.WaitForNewItemsAsync()));
                    }
                });
        }

        private static void LogEvents(string filePath)
        {
            var newSettings = null as FileLogSettings;
            while (!LogSettingsByFile.TryGetValue(filePath, out newSettings)) ;

            var currentState = LogStatesByFile[filePath];
            var stateWasUpdated = false;
            if (!SettingsComparer.Equals(newSettings, currentState.Settings))
            {
                currentState.CloseForWriting();
                LogStatesByFile[filePath] = new FileLogState(newSettings, currentState.Owner);
                currentState.WaitForNoWriters();
                stateWasUpdated = true;
            }

            int eventsCount;
            do
            {
                eventsCount = currentState.Events.Drain(currentState.TemporaryBuffer, 0, currentState.TemporaryBuffer.Length);
                currentState.ObtainWriter().WriteEvents(currentState.TemporaryBuffer, eventsCount);
            } while (eventsCount > 0 && stateWasUpdated);

            if (stateWasUpdated)
                currentState.ObtainWriter().Dispose();
        }

        private static void Initialize()
        {
            StartLoggingTask();
        }
    }
}