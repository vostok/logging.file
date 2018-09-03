using System;

namespace Vostok.Logging.File.EventsWriting
{
    internal class CooldownController
    {
        private DateTime cooldownExpiration;

        public void IncurCooldown(TimeSpan duration)
        {
            cooldownExpiration = DateTime.UtcNow + duration;
        }

        public bool IsCool => DateTime.UtcNow >= cooldownExpiration;
    }
}