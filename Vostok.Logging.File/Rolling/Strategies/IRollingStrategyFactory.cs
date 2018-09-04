using System;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal interface IRollingStrategyFactory
    {
        IRollingStrategy CreateStrategy(FilePath basePath, RollingStrategyType type, Func<FileLogSettings> settingsProvider);
    }
}
