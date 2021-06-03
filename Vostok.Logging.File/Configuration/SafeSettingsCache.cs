using System;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Commons.Threading;

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
        private volatile AtomicBoolean refreshing;
        private volatile FileLogSettings currentSettings;

        public SafeSettingsCache(Func<FileLogSettings> provider)
        {
            this.provider = new SafeSettingsProvider(provider);

            var settings = Get();

            enabled = settings.EnableFileLogSettingsCache;

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

        public void ForceRefresh()
        {
            SpinWait.SpinUntil(() => refreshing.TrySetTrue());

            try
            {
                currentSettings = provider.UnsafeGet();
            }
            finally
            {
                refreshing = false;
            }
        }

        private void ScheduleRefresh()
        {
            if (updateCooldown == null && Interlocked.CompareExchange(ref updateCooldown, CooldownGuard, null) == null)
            {
                if (refreshing.TrySetTrue())
                    updateCacheTask = Task.Run(
                        () =>
                        {
                            currentSettings = provider.Get();
                            refreshing = false;
                        });

                updateCacheTask
                   .ContinueWith(_ => Task.Delay(ttl))
                   .Unwrap()
                   .ContinueWith(_ => updateCooldown = null);
            }
        }
    }
}