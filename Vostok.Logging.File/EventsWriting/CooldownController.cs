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

        public Task WaitForCooldownAsync() => Task.WhenAny(cooldownTask, endImmediately);

        public void IncurCooldown(TimeSpan duration, CancellationToken cancellation)
        {
            endImmediately.Reset();
            cooldownTask = Task.Delay(duration, cancellation);
        }

        public void DropCooldown() => endImmediately.Set();
    }
}