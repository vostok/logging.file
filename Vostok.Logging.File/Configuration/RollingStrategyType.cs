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
        /// Switch to next file once in a specified time period.
        /// </summary>
        ByTime = 0b01,

        /// <summary>
        /// Switch to next file when current file reaches specified size.
        /// </summary>
        BySize = 0b10,

        /// <summary>
        /// A combination of <see cref="ByTime"/> and <see cref="BySize"/> approaches. Will switch to next file as soon as any of the conditions is met.
        /// </summary>
        Hybrid = 0b11
    }
}