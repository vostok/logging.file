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
    public class FileLog : ILog
    {
        private static readonly FileLogMuxerProvider DefaultMuxerProvider = new FileLogMuxerProvider();

        private readonly FileLogMuxerProvider muxerProvider;
        private readonly SafeSettingsProvider settingsProvider;
        private long eventsLost;

        public FileLog(FileLogSettings settings)
            : this(() => settings)
        {
        }

        public FileLog(Func<FileLogSettings> settingsProvider) =>
            this.settingsProvider = new SafeSettingsProvider(() => SettingsValidator.ValidateSettings(settingsProvider()));

        public long EventsLost => Interlocked.Read(ref eventsLost);

        public void Log(LogEvent @event)
        {
            if (@event == null)
                return;

            if (!DefaultMuxerProvider.ObtainMuxer().TryLog(@event, settingsProvider.Get(), this))
                Interlocked.Increment(ref eventsLost);
        }

        public bool IsEnabledFor(LogLevel level) => settingsProvider.Get().EnabledLogLevels.Contains(level);

        // TODO(krait): implement same as in ConsoleLog
        public ILog ForContext(string context) => this;

        public Task FlushAsync() => DefaultMuxerProvider.ObtainMuxer().FlushAsync();

        public void Flush() => FlushAsync().GetAwaiter().GetResult();

        public void Close() => DefaultMuxerProvider.ObtainMuxer().Close(settingsProvider.Get());
    }
}