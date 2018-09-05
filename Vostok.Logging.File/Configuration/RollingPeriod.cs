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
        /// Change to a new file every day.
        /// </summary>
        Day,

        /// <summary>
        /// Change to a new file every hour.
        /// </summary>
        Hour,

        /// <summary>
        /// Change to a new file every minute.
        /// </summary>
        Minute,

        /// <summary>
        /// Change to a new file every second.
        /// </summary>
        Second
    }
}