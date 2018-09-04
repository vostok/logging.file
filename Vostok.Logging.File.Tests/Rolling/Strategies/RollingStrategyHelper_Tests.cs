using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling;
using Vostok.Logging.File.Rolling.Helpers;
using Vostok.Logging.File.Rolling.Strategies;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Tests.Rolling.Strategies
{
    [TestFixture]
    internal class RollingStrategyHelper_Tests
    {
        private IFileSuffixFormatter<int> suffixFormatter;
        private IFileNameTuner tuner;
        private IFileSystem fileSystem;

        [SetUp]
        public void TestSetup()
        {
            fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetFilesByPrefix(@"logs\log").Returns(new[] {@"logs\log1.txt"});

            tuner = Substitute.For<IFileNameTuner>();
            tuner.RemoveExtension(@"logs\log.txt").Returns(@"logs\log");
            tuner.RemoveExtension(@"logs\log1.txt").Returns(@"logs\log1");
            tuner.RemoveExtension(@"logs\log~.txt").Returns(@"logs\log~");

            suffixFormatter = Substitute.For<IFileSuffixFormatter<int>>();
            suffixFormatter.TryParseSuffix("1").Returns(1);
        }

        [Test]
        public void Should_remove_extensions_using_provided_tuner()
        {
            RollingStrategyHelper.DiscoverExistingFiles(@"logs\log.txt", fileSystem, suffixFormatter, tuner)
                .Single().path.Should().Be(@"logs\log1");
        }

        [Test]
        public void Should_parse_suffix_using_provided_formatter()
        {
            RollingStrategyHelper.DiscoverExistingFiles(@"logs\log.txt", fileSystem, suffixFormatter, tuner)
                .Single().suffix.Should().Be(1);
        }

        [Test]
        public void Should_not_return_paths_where_suffix_could_not_be_parsed()
        {
            fileSystem.GetFilesByPrefix(@"logs\log").Returns(new[] {@"logs\log1.txt", @"logs\log~.txt"});

            RollingStrategyHelper.DiscoverExistingFiles(@"logs\log.txt", fileSystem, suffixFormatter, tuner).Should().HaveCount(1);
        }
    }
}