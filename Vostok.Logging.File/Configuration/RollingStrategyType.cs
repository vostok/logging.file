using System;
using JetBrains.Annotations;

namespace Vostok.Logging.File.Configuration
{
    /// <summary>
    /// Specifies the rolling behavior: when to switch to the next part of log file.
    /// </summary>
    [PublicAPI]
    [Flags]
    public enum RollingStrategyType
    {
        /// <summary>
        /// No rolling, just always write to one file.
        /// </summary>
        None = 0b00,

        /// <summary>
        /// <para>Switch to next file once in a specified time period.</para>
        /// <para>The number of log files is limited with <see cref="RollingStrategyOptions.MaxFiles"/>.</para>
        /// </summary>
        ByTime = 0b01,

        /// <summary>
        /// <para>Switch to next file when current file reaches specified size.</para>
        /// <para>The size of log files is limited with <see cref="RollingStrategyOptions.MaxSize"/>.</para>
        /// </summary>
        BySize = 0b10,

        /// <summary>
        /// <para>A combination of <see cref="ByTime"/> and <see cref="BySize"/> approaches. Will switch to next file as soon as any of the conditions is met.</para>
        /// <para>The number of log files is limited with <see cref="RollingStrategyOptions.MaxFiles"/> and the size of log files is limited with <see cref="RollingStrategyOptions.MaxSize"/>.</para>
        /// </summary>
        Hybrid = 0b11
    }
}