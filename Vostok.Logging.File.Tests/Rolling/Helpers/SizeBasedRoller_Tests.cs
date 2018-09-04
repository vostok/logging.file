using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Helpers;

namespace Vostok.Logging.File.Tests.Rolling.Helpers
{
    [TestFixture]
    internal class SizeBasedRoller_Tests
    {
        private long maxSize;
        private long currentSize;
        private SizeBasedRoller roller;

        [SetUp]
        public void TestSetup()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetFileSize("log").Returns(_ => currentSize);

            roller = new SizeBasedRoller(fileSystem, () => maxSize);
        }

        [Test]
        public void Should_not_roll_over_if_file_size_is_less_than_max_size()
        {
            currentSize = 0;
            maxSize = 100;

            roller.ShouldRollOver("log").Should().BeFalse();
        }

        [Test]
        public void Should_roll_over_if_file_size_is_equal_to_max_size()
        {
            currentSize = 100;
            maxSize = 100;

            roller.ShouldRollOver("log").Should().BeTrue();
        }

        [Test]
        public void Should_roll_over_if_file_size_is_greater_than_max_size()
        {
            currentSize = 1000;
            maxSize = 100;

            roller.ShouldRollOver("log").Should().BeTrue();
        }

        [Test]
        public void Should_update_max_size_before_each_check()
        {
            currentSize = 100;

            maxSize = 120;
            roller.ShouldRollOver("log").Should().BeFalse();

            maxSize = 80;
            roller.ShouldRollOver("log").Should().BeTrue();
        }
    }
}