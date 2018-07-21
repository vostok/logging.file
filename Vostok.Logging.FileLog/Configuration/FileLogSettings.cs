using System;
using System.Text;
using Vostok.Commons;
using Vostok.Commons.Conversions;
using Vostok.Configuration.Abstractions;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Core.ConversionPattern;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Vostok.Logging.FileLog.Configuration
{
    [ValidateBy(typeof(FileLogSettingsValidator))] // TODO(krait): update validator
    public class FileLogSettings
    {
        public string FilePath { get; set; } = @"logs\log";

        public ConversionPattern ConversionPattern { get; set; } = ConversionPatternParser.Parse("");

        public FileOpenMode FileOpenMode { get; set; } = FileOpenMode.Append;

        public RollingStrategyOptions RollingStrategy { get; set; } = new RollingStrategyOptions();

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        public LogLevel[] EnabledLogLevels { get; set; } = (LogLevel[])Enum.GetValues(typeof (LogLevel));

        public int EventsQueueCapacity { get; set; } = 10000;

        public class RollingStrategyOptions
        {
            public RollingStrategyType Type { get; set; } = RollingStrategyType.None;

            public TimeSpan Period { get; set; } = 1.Days();

            public DataSize MaxSize { get; set; } = 100.Megabytes();

            public int MaxFiles = 5;
        }
    }
}