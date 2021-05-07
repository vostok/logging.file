using System;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable MemberInitializerValueIgnored

namespace Vostok.Logging.File.Configuration
{
    internal class SafeSettingsCache
    {
        private readonly SafeSettingsProvider provider;
        private readonly TimeSpan ttl = TimeSpan.FromSeconds(1);
        private readonly bool enabled = true;
        private volatile Lazy<FileLogSettings> container;

        public SafeSettingsCache(Func<FileLogSettings> provider)
        {
            this.provider = new SafeSettingsProvider(provider);

            ResetContainer();

            enabled = Get().EnableFileLogSettingsCache;
        }

        public FileLogSettings Get() => enabled 
            ? container.Value 
            : provider.Get();

        private void ResetContainer()
        {
            if (enabled)
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
}