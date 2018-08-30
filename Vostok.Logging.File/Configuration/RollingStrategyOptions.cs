using System;

namespace Vostok.Logging.File.Configuration
{
    public class RollingStrategyOptions
    {
        public int MaxFiles = 5;

        public RollingStrategyType Type { get; set; } = RollingStrategyType.None;

        public TimeSpan Period { get; set; } = TimeSpan.FromDays(1);

        public long MaxSize { get; set; } = 100 * 1024 * 1024;
    }
}