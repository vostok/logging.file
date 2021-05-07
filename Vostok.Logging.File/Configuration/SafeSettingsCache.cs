using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vostok.Logging.File.Configuration
{
    internal class SafeSettingsCache
    {
        private readonly SafeSettingsProvider provider;
        private readonly TimeSpan ttl = TimeSpan.FromSeconds(1);
        private volatile Lazy<FileLogSettings> container;

        public SafeSettingsCache(Func<FileLogSettings> provider)
        {
            this.provider = new SafeSettingsProvider(provider);

            ResetContainer();
        }

        public SafeSettingsCache(Func<FileLogSettings> provider, TimeSpan ttl)
        {
            this.provider = new SafeSettingsProvider(provider);
            this.ttl = ttl;

            ResetContainer();
        }

        public FileLogSettings Get() => container.Value;

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