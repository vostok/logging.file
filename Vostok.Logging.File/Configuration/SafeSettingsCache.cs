using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vostok.Logging.File.Configuration
{
    internal class SafeSettingsCache
    {
        private static readonly object CooldownGuard = new object();

        private readonly SafeSettingsProvider provider;
        private readonly TimeSpan ttl;
        private readonly bool enabled;

        private volatile object updateCooldown;
        private volatile Task updateCacheTask = Task.CompletedTask;
        private volatile FileLogSettings currentSettings;

        public SafeSettingsCache(Func<FileLogSettings> provider)
        {
            this.provider = new SafeSettingsProvider(provider);

            var settings = Get();

            enabled = settings.EnableFileLogSettingsCache;
            ttl = settings.FileSettingsUpdateCooldown;

            if (enabled)
                currentSettings = settings;
        }

        public FileLogSettings Get()
        {
            if (!enabled)
                return provider.Get();

            ScheduleRefresh();

            return currentSettings;
        }

        public void ForceRefresh() => Interlocked.Exchange(ref currentSettings, provider.UnsafeGet());

        private void ScheduleRefresh()
        {
            if (updateCooldown == null && Interlocked.CompareExchange(ref updateCooldown, CooldownGuard, null) == null)
            {
                updateCacheTask = Task.Run(
                    () =>
                    {
                        var oldSettings = currentSettings;
                        var newSettings = provider.Get();
                        Interlocked.CompareExchange(ref currentSettings, newSettings, oldSettings);
                    });

                updateCacheTask
                   .ContinueWith(_ => Task.Delay(ttl))
                   .Unwrap()
                   .ContinueWith(_ => Interlocked.Exchange(ref updateCooldown, null));
            }
        }
    }
}