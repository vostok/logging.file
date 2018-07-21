using System.Collections.Concurrent;
using System.Linq;
using Vostok.Configuration.Abstractions;
using Vostok.Logging.Abstractions;
using Vostok.Logging.FileLog.Configuration;

namespace Vostok.Logging.FileLog
{
    public class FileLog : ILog
    {
        private static readonly ConcurrentDictionary<IConfigurationSource, FileLogConfigProvider> providersBySource =
            new ConcurrentDictionary<IConfigurationSource, FileLogConfigProvider>();

        private static readonly ConcurrentDictionary<string, FileLogConfigProvider> providersByName =
            new ConcurrentDictionary<string, FileLogConfigProvider>();

        private readonly FileLogConfigProvider configProvider;

        public FileLog(FileLogSettings settings) => configProvider = new FileLogConfigProvider(settings);

        public FileLog(string name) =>
            configProvider = providersByName.GetOrAdd(name, s => new FileLogConfigProvider(s));

        public FileLog(IConfigurationSource configSource) =>
            configProvider = providersBySource.GetOrAdd(configSource, cs => new FileLogConfigProvider(cs));

        public void Log(LogEvent @event)
        {
            if (@event == null)
                return;

            FileLogMuxer.Log(configProvider, @event);
        }

        public bool IsEnabledFor(LogLevel level) => configProvider.Settings.EnabledLogLevels.Contains(level);

        public ILog ForContext(string context) => this;
    }
}