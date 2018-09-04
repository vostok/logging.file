using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File
{
    internal class LogEventInfo
    {
        public LogEventInfo(LogEvent @event, FileLogSettings settings)
        {
            Event = @event;
            Settings = settings;
        }

        public LogEvent Event { get; }

        public FileLogSettings Settings { get; }
    }
}
