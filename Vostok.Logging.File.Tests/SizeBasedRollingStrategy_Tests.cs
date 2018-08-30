using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Rolling;
using Vostok.Logging.File.Rolling.Strategies;

namespace Vostok.Logging.File.Tests
{
    [TestFixture]
    internal class SizeBasedRollingStrategy_Tests
    {
        private SizeBasedRollingStrategy strategy;
        private IFileSystem fileSystem;

        [SetUp]
        public void TestSetup()
        {
            fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetFilesByPrefix(@"logs\log").Returns(new[] { @"logs\log3", @"logs\log1", @"logs\log2" });

            var suffixFormatter = Substitute.For<IFileSuffixFormatter<int>>();
            suffixFormatter.FormatSuffix(Arg.Any<int>()).Returns(callInfo => "." + callInfo.Arg<int>());
            suffixFormatter.TryParseSuffix(Arg.Any<string>()).Returns(callInfo => int.TryParse(callInfo.Arg<string>().Substring(1), out var p) ? p : null as int?);

            var roller = Substitute.For<ISizeBasedRoller>();

            strategy = new SizeBasedRollingStrategy(fileSystem, suffixFormatter, roller);
        }

        [Test]
        public void DiscoverExistingFiles_should_ignore_files_without_correct_suffix()
        {
            fileSystem.GetFilesByPrefix(@"logs\log").Returns(new[] { @"logs\log.1", @"logs\log2", @"logs\log3" });

            strategy.DiscoverExistingFiles(@"logs\log").Should().Equal(@"logs\log.1");
        }

        [Test]
        public void DiscoverExistingFiles_should_order_files_by_part_suffix()
        {
            fileSystem.GetFilesByPrefix(@"logs\log").Returns(new[] { @"logs\log.3", @"logs\log.2", @"logs\log.1", @"logs\log3" });

            strategy.DiscoverExistingFiles(@"logs\log").Should().Equal(@"logs\log.1", @"logs\log.2", @"logs\log.3");
        }

        [Test]
        public void GetCurrentFile_should_return_base_path_plus_current_part_suffix()
        {
            strategy.GetCurrentFile(@"logs\log").Should().Be(@"logs\log.1");
        }

        // TODO(krait): test rolling
    }
}