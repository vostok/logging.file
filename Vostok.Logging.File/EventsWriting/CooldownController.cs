using System;
using System.Threading;
using System.Threading.Tasks;
using Signal = System.Threading.Tasks.TaskCompletionSource<bool>;

namespace Vostok.Logging.File.EventsWriting
{
    internal class CooldownController : ICooldownController
    {
        private Signal endImmediately = new Signal();
        private Task cooldownTask = Task.CompletedTask;

        public bool IsCool => cooldownTask.IsCompleted;

        public Task WaitForCooldownAsync() => Task.WhenAny(cooldownTask, endImmediately.Task);

        public void IncurCooldown(TimeSpan duration, CancellationToken cancellation)
        {
            endImmediately = new Signal();
            cooldownTask = Task.Delay(duration, cancellation);
        }

        public void DropCooldown()
        {
            endImmediately.SetResult(true);
        }
    }
}