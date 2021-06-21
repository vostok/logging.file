using System;
using System.Threading;
using System.Threading.Tasks;
using Signal = Vostok.Commons.Threading.AsyncManualResetEvent;

namespace Vostok.Logging.File.EventsWriting
{
    internal class CooldownController : ICooldownController
    {
        private readonly Signal endImmediately = new Signal(false);
        private Task cooldownTask = Task.CompletedTask;

        public bool IsCool => cooldownTask.IsCompleted;

        public Task WaitForCooldownAsync() => cooldownTask;

        public void IncurCooldown(TimeSpan duration, CancellationToken cancellation)
        {
            endImmediately.Reset();
            cooldownTask = Task.WhenAny(Task.Delay(duration, cancellation), endImmediately.WaitAsync(cancellation)).Unwrap();
        }

        public void DropCooldown() => endImmediately.Set();
    }
}