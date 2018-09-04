using JetBrains.Annotations;

namespace Vostok.Logging.File.Configuration
{
    /// <summary>
    /// Settings that are shared between all file logs.
    /// </summary>
    [PublicAPI]
    public class FileLogGlobalSettings
    {
        /// <summary>
        /// Specifies how many log events are processed in one iteration for each file.
        /// </summary>
        public int EventsTemporaryBufferCapacity { get; set; } = 10 * 1000;
    }
}