using System;
using System.Text;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Formatting;

namespace Vostok.Logging.File.Configuration
{
    /// <summary>
    /// Settings of a file log instance.
    /// </summary>
    [PublicAPI]
    public class FileLogSettings
    {
        /// <summary>
        /// Path to log file. If a <see cref="RollingStrategy"/> is specified, a suffix may be added to this path. The path is relative to current working directory (<see cref="Environment.CurrentDirectory"/>).
        /// </summary>
        [NotNull]
        public string FilePath { get; set; } = "logs/log";

        /// <summary>
        /// The <see cref="Formatting.OutputTemplate"/> used to render log messages.
        /// </summary>
        [NotNull]
        public OutputTemplate OutputTemplate { get; set; } = OutputTemplate.Default;

        /// <summary>
        /// If specified, this <see cref="IFormatProvider"/> will be used when formatting log events.
        /// </summary>
        [CanBeNull]
        public IFormatProvider FormatProvider { get; set; }

        /// <summary>
        /// Specifies the way to treat an existing log file: append or rewrite.
        /// </summary>
        public FileOpenMode FileOpenMode { get; set; } = FileOpenMode.Append;

        /// <summary>
        /// An optional rolling strategy.
        /// </summary>
        [NotNull]
        public RollingStrategyOptions RollingStrategy { get; set; } = new RollingStrategyOptions();

        /// <summary>
        /// Output text encoding.
        /// </summary>
        [NotNull]
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Output buffer size, in bytes.
        /// </summary>
        public int OutputBufferSize { get; set; } = 65536;

        /// <summary>
        /// Only log events with levels contained in this array will be logged.
        /// </summary>
        [NotNull]
        public LogLevel[] EnabledLogLevels { get; set; } = (LogLevel[])Enum.GetValues(typeof(LogLevel));

        /// <summary>
        /// Capacity of the log events queue.
        /// </summary>
        public int EventsQueueCapacity { get; set; } = 50 * 1000;

        /// <summary>
        /// Specifies how many log events are processed in one iteration for each file.
        /// </summary>
        public int EventsBufferCapacity { get; set; } = 10 * 1000;

        /// <summary>
        /// Cooldown for calls to rolling-related code. This means that when conditions are met to switch to the next part of log file, the switching may be delayed for up to <see cref="RollingUpdateCooldown"/>.
        /// </summary>
        public TimeSpan RollingUpdateCooldown { get; set; } = TimeSpan.FromSeconds(1);
    }
}