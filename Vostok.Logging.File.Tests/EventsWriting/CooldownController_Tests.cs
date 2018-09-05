using System;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Commons.Testing;
using Vostok.Logging.File.EventsWriting;

namespace Vostok.Logging.File.Tests.EventsWriting
{
    [TestFixture]
    internal class CooldownController_Tests
    {
        private CooldownController controller;

        [SetUp]
        public void TestSetup()
        {
            controller = new CooldownController();
        }

        [Test]
        public void IsCool_should_return_true_initially()
        {
            controller.IsCool.Should().BeTrue();
        }

        [Test]
        public void IsCool_should_return_false_before_cooldown_expiration()
        {
            controller.IncurCooldown(TimeSpan.FromMilliseconds(100));

            controller.IsCool.Should().BeFalse();
        }

        [Test]
        public void IsCool_should_return_true_after_cooldown_expiration()
        {
            controller.IncurCooldown(TimeSpan.FromMilliseconds(100));

            new Action(() => controller.IsCool.Should().BeTrue())
                .ShouldPassIn(1.Seconds());
        }

        [Test]
        public void WaitForCooldownAsync_should_return_completed_task_if_there_is_no_cooldown()
        {
            controller.WaitForCooldownAsync().IsCompleted.Should().BeTrue();
        }

        [Test]
        public void WaitForCooldownAsync_should_wait_for_cooldown()
        {
            controller.IncurCooldown(TimeSpan.FromMilliseconds(100));

            var task = controller.WaitForCooldownAsync();
            task.IsCompleted.Should().BeFalse();

            task.Wait(1000);
            task.IsCompleted.Should().BeTrue();
        }

        [Test]
        public void Should_be_reusable()
        {
            controller.IncurCooldown(TimeSpan.FromMilliseconds(100));

            controller.IsCool.Should().BeFalse();
            new Action(() => controller.IsCool.Should().BeTrue())
                .ShouldPassIn(1.Seconds());

            controller.IncurCooldown(TimeSpan.FromMilliseconds(100));

            controller.IsCool.Should().BeFalse();
            new Action(() => controller.IsCool.Should().BeTrue())
                .ShouldPassIn(1.Seconds());
        }
    }
}