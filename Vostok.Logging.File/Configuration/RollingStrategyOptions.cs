using System;
using Vostok.Commons.Primitives;

namespace Vostok.Logging.File.Configuration
{
    public class RollingStrategyOptions
    {
        public int MaxFiles = 5;

        public RollingStrategyType Type { get; set; } = RollingStrategyType.None;

        public TimeSpan Period { get; set; } = TimeSpan.FromDays(1);

        public DataSize MaxSize { get; set; } = DataSize.FromMegabytes(100);
    }
}