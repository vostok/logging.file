using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Strategies;

namespace Vostok.Logging.File.Tests.Rolling.Strategies
{
    [TestFixture]
    internal class DisabledRollingStrategy_Tests
    {
        private DisabledRollingStrategy strategy;
        private IFileSystem fileSystem;

        [SetUp]
        public void TestSetup()
        {
            fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetFilesByPrefix("logs/log").Returns(new FilePath[] { "logs/log", "logs/log-2" });
            fileSystem.Exists(Arg.Any<FilePath>()).Returns(true);

            strategy = new DisabledRollingStrategy(fileSystem);
        }

        [Test]
        public void DiscoverExistingFiles_should_return_only_base_file()
        {
            strategy.DiscoverExistingFiles("logs/log").Should().Equal("logs/log");
        }

        [Test]
        public void DiscoverExistingFiles_should_return_nothing_if_base_file_does_not_exist()
        {
            fileSystem.Exists("logs/log").Returns(false);

            strategy.DiscoverExistingFiles("logs/log").Should().BeEmpty();
        }

        [Test]
        public void GetCurrentFile_should_return_base_file()
        {
            strategy.GetCurrentFile("logs/log.txt").Should().Be((FilePath)"logs/log.txt");
        }
    }
}