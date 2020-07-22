﻿using JetBrains.Annotations;

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
        /// Separator between base path and rolling suffix. Does not affect <see cref="RollingStrategyType.None"/> strategy.
        /// Symbol before suffix is equal to symbol before {RollingSuffix} placeholder, or equal to last symbol of base path, if {RollingSuffix} placeholder is missing.
        /// If symbol before suffix is equal to Separator, or dot, or directory separator char, then Separator will not be added before suffix.
        /// </summary>
        public char SuffixSeparator { get; set; } = '-';
    }
}