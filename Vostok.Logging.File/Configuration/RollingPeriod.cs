using JetBrains.Annotations;

namespace Vostok.Logging.File.Configuration
{
    /// <summary>
    /// Specifies how often a time-based rolling strategy should switch to a new file.
    /// </summary>
    [PublicAPI]
    public enum RollingPeriod
    {
        /// <summary>
        /// Change file every day.
        /// </summary>
        Day,

        /// <summary>
        /// Change file every hour.
        /// </summary>
        Hour,

        /// <summary>
        /// Change file every minute.
        /// </summary>
        Minute,

        /// <summary>
        /// Change file every second.
        /// </summary>
        Second
    }
}