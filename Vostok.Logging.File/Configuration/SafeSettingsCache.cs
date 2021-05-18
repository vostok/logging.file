using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vostok.Logging.File.Configuration
{
    internal class SafeSettingsCache
    {
        private static readonly object CooldownGuard = new object();

        private readonly SafeSettingsProvider provider;
        private readonly TimeSpan ttl = TimeSpan.FromSeconds(1);
        private readonly bool enabled;

        private volatile object updateCooldown;
        private volatile Task updateCacheTask = Task.CompletedTask;
        private volatile FileLogSettings currentSettings;

        public SafeSettingsCache(Func<FileLogSettings> provider)
        {
            this.provider = new SafeSettingsProvider(provider);

            var settings = this.provider.Get();

            enabled = settings.EnableFileLogSettingsCache;

            if (enabled)
                currentSettings = settings;
        }

        public FileLogSettings Get()
        {
            if (!enabled)
                return provider.Get();

            RefreshCacheAsync();

            return currentSettings;
        }

        public void RefreshCache() => RefreshCacheAsync().GetAwaiter().GetResult();

        private Task RefreshCacheAsync()
        {
            if (updateCooldown == null && Interlocked.CompareExchange(ref updateCooldown, CooldownGuard, null) == null)
            {
                updateCacheTask = new Task(() => currentSettings = provider.Get());

                var incurCooldown = updateCacheTask
                   .ContinueWith(_ => Task.Delay(ttl))
                   .Unwrap()
                   .ContinueWith(_ => updateCooldown = null);

                incurCooldown.Start();
            }

            return updateCacheTask;
        }
    }
}