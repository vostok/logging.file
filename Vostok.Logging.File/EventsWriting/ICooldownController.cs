using System;
using System.Threading;
using System.Threading.Tasks;

namespace Vostok.Logging.File.EventsWriting
{
    internal interface ICooldownController
    {
        bool IsCool { get; }

        Task WaitForCooldownAsync();

        void IncurCooldown(TimeSpan duration, CancellationToken cancellation);
    }
}