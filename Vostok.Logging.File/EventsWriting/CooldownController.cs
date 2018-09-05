using System;
using System.Threading.Tasks;

namespace Vostok.Logging.File.EventsWriting
{
    internal class CooldownController : ICooldownController
    {
        private Task cooldownTask = Task.CompletedTask;

        public bool IsCool => cooldownTask.IsCompleted;

        public Task WaitForCooldownAsync() => cooldownTask;

        public void IncurCooldown(TimeSpan duration) => cooldownTask = Task.Delay(duration);
    }
}