using JetBrains.Annotations;

namespace Vostok.Logging.File.Configuration
{
    /// <summary>
    /// Configuration of rolling strategy.
    /// </summary>
    [PublicAPI]
    public class RollingStrategyOptions
    {
        /// <summary>
        /// How many log files to keep. Older files are automatically deleted when switching to a new file. Specify zero to avoid deleting old files.
        /// </summary>
        public int MaxFiles { get; set; } = 5;

        /// <summary>
        /// Type of rolling strategy.
        /// </summary>
        public RollingStrategyType Type { get; set; } = RollingStrategyType.None;

        /// <summary>
        /// Period of switching to the next part of log file. Affects only <see cref="RollingStrategyType.ByTime"/> and <see cref="RollingStrategyType.Hybrid"/> strategies.
        /// </summary>
        public RollingPeriod Period { get; set; } = RollingPeriod.Day;

        /// <summary>
        /// Maximal size of one part of log file, in bytes. Affects only <see cref="RollingStrategyType.BySize"/> and <see cref="RollingStrategyType.Hybrid"/> strategies.
        /// </summary>
        public long MaxSize { get; set; } = 100 * 1024 * 1024;

        internal RollingStrategyOptions Clone() => new RollingStrategyOptions
        {
            MaxFiles = MaxFiles,
            Type = Type,
            Period = Period,
            MaxSize = MaxSize
        };
    }
}
