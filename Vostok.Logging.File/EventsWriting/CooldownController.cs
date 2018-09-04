using System;

namespace Vostok.Logging.File.EventsWriting
{
    internal class CooldownController : ICooldownController
    {
        private DateTime cooldownExpiration;

        public bool IsCool => DateTime.UtcNow >= cooldownExpiration;

        public void IncurCooldown(TimeSpan duration) => cooldownExpiration = DateTime.UtcNow + duration;
    }
}