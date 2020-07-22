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
        /// How many log files to keep. Older files are automatically deleted when switching to a new file. Specify a value &lt;= 0 to avoid deleting old files.
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
        public long MaxSize { get; set; } = 1024 * 1024 * 1024;

        /// <summary>
        /// <para>Separator between base path and rolling suffix.</para>
        /// <para>Does not affect <see cref="RollingStrategyType.None"/> strategy.</para>
        /// <para>If <c>{RollingSuffix}</c> placeholder is explicitly used in log file path, this separator is inserted before it (unless the placeholder is explicitly preceded by a dot, slash or dash).</para>
        /// </summary>
        public char SuffixSeparator { get; set; } = '-';
    }
}