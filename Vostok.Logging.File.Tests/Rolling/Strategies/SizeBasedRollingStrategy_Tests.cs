using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Helpers;
using Vostok.Logging.File.Rolling.Strategies;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Tests.Rolling.Strategies
{
    [TestFixture]
    internal class SizeBasedRollingStrategy_Tests
    {
        private SizeBasedRollingStrategy strategy;
        private IFileSystem fileSystem;
        private ISizeBasedRoller roller;

        [SetUp]
        public void TestSetup()
        {
            fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetFilesByPrefix("logs/log").Returns(new FilePath[] { "logs/log-3", "logs/log-1", "logs/log-2" });

            var suffixFormatter = Substitute.For<IFileSuffixFormatter<int>>();
            suffixFormatter.FormatSuffix(Arg.Any<int>()).Returns(callInfo => callInfo.Arg<int>().ToString());
            suffixFormatter.TryParseSuffix(Arg.Any<string>()).Returns(callInfo => int.TryParse(callInfo.Arg<string>(), out var p) ? p : null as int?);

            roller = Substitute.For<ISizeBasedRoller>();

            strategy = new SizeBasedRollingStrategy(fileSystem, suffixFormatter, roller);
        }

        [Test]
        public void DiscoverExistingFiles_should_ignore_files_without_correct_suffix()
        {
            fileSystem.GetFilesByPrefix("logs/log").Returns(new FilePath[] { "logs/log-1", "logs/logtwo", "logs/logthree" });

            strategy.DiscoverExistingFiles("logs/log").Should().Equal("logs/log-1");
        }

        [Test]
        public void DiscoverExistingFiles_should_order_files_by_part_suffix()
        {
            fileSystem.GetFilesByPrefix("logs/log").Returns(new FilePath[] { "logs/log-3", "logs/log-2", "logs/log-1", "logs/logwhatever" });

            strategy.DiscoverExistingFiles("logs/log").Should().Equal("logs/log-1", "logs/log-2", "logs/log-3");
        }

        [Test]
        public void DiscoverExistingFiles_should_support_file_extensions()
        {
            fileSystem.GetFilesByPrefix("logs/log.txt").Returns(new FilePath[] { "logs/log-3.txt", "logs/log-2.txt", "logs/log-1.txt" });

            strategy.DiscoverExistingFiles("logs/log.txt").Should().Equal("logs/log-1.txt", "logs/log-2.txt", "logs/log-3.txt");
        }

        [Test]
        public void GetCurrentFile_should_return_base_path_plus_current_part_suffix()
        {
            strategy.GetCurrentFile("logs/log").Should().Be((FilePath)"logs/log-3");
        }

        [Test]
        public void GetCurrentFile_should_roll_over_when_max_size_is_reached()
        {
            fileSystem.GetFilesByPrefix("logs/log").Returns(new FilePath[] { "logs/log-1" });
            roller.ShouldRollOver("logs/log-1").Returns(true);

            strategy.GetCurrentFile("logs/log").Should().Be((FilePath)"logs/log-2");
        }

        [Test]
        public void GetCurrentFile_should_consider_existing_files_when_choosing_new_part_number()
        {
            fileSystem.GetFilesByPrefix("logs/log").Returns(new FilePath[] { "logs/log-2" });
            roller.ShouldRollOver("logs/log-2").Returns(true);

            strategy.GetCurrentFile("logs/log").Should().Be((FilePath)"logs/log-3");
        }

        [Test]
        public void GetCurrentFile_should_support_file_extensions()
        {
            strategy.GetCurrentFile("logs/log.txt").Should().Be((FilePath)"logs/log-1.txt");
        }

        [Test]
        public void GetCurrentFile_should_restore_extension_before_calling_roller()
        {
            fileSystem.GetFilesByPrefix("logs/log.txt").Returns(new FilePath[] { "logs/log-1.txt" });
            roller.ShouldRollOver("logs/log-1.txt").Returns(true);

            strategy.GetCurrentFile("logs/log.txt").Should().Be((FilePath)"logs/log-2.txt");
        }
    }
}