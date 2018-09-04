using System;

namespace Vostok.Logging.File.EventsWriting
{
    internal interface ICooldownController
    {
        bool IsCool { get; }

        void IncurCooldown(TimeSpan duration);
    }
}