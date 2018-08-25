using System;

namespace Vostok.Logging.File.Configuration
{
    [Flags]
    public enum RollingStrategyType
    {
        None = 0b00,
        ByTime = 0b01,
        BySize = 0b10,
        Hybrid = 0b11
    }
}