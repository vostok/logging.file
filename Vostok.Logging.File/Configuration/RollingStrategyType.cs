using System;
using JetBrains.Annotations;

#pragma warning disable 1591

namespace Vostok.Logging.File.Configuration
{
    [PublicAPI]
    [Flags]
    public enum RollingStrategyType
    {
        None = 0b00,
        ByTime = 0b01,
        BySize = 0b10,
        Hybrid = 0b11
    }
}