using System;
using System.Text;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Formatting;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Vostok.Logging.File.Configuration
{
    public class FileLogSettings
    {
        public string FilePath { get; set; } = @"logs\log";

        public OutputTemplate OutputTemplate { get; set; } = OutputTemplate.Default;

        public FileOpenMode FileOpenMode { get; set; } = FileOpenMode.Append;

        public RollingStrategyOptions RollingStrategy { get; set; } = new RollingStrategyOptions();

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public int OutputBufferSize { get; set; } = 4096;

        public LogLevel[] EnabledLogLevels { get; set; } = (LogLevel[])Enum.GetValues(typeof(LogLevel));

        public int EventsQueueCapacity { get; set; } = 10 * 1000;
    }
}