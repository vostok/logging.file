using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vostok.Logging.File.Configuration
{
    internal class SafeSettingsCache
    {
        private readonly SafeSettingsProvider provider;
        private readonly TimeSpan ttl = TimeSpan.FromSeconds(1);
        private readonly bool enabled;
        private volatile Lazy<FileLogSettings> container;

        public SafeSettingsCache(Func<FileLogSettings> provider)
        {
            this.provider = new SafeSettingsProvider(provider);

            enabled = this.provider.Get().EnableFileLogSettingsCache;

            if (enabled)
                ResetContainer();
        }

        public FileLogSettings Get() => enabled
            ? container.Value
            : provider.Get();

        private void ResetContainer()
        {
            container = new Lazy<FileLogSettings>(
                () =>
                {
                    try
                    {
                        return provider.Get();
                    }
                    finally
                    {
                        Task.Delay(ttl).ContinueWith(_ => ResetContainer());
                    }
                },
                LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}