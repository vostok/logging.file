using System;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Rolling;

namespace Vostok.Logging.File.Tests
{
    [TestFixture]
    internal class TimeBasedRollingStrategy_Tests
    {
        private TimeBasedRollingStrategy strategy;
        private IFileSystem fileSystem;
        private DateTime now;

        [SetUp]
        public void TestSetup()
        {
            fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetFilesByPrefix(@"logs\log").Returns(new[] {@"logs\log3", @"logs\log1", @"logs\log2"});

            var suffixFormatter = Substitute.For<IFileSuffixFormatter<DateTime>>();
            suffixFormatter.FormatSuffix(Arg.Any<DateTime>()).Returns(callInfo => callInfo.Arg<DateTime>().ToString("yyyy.MM.dd"));
            suffixFormatter.TryParseSuffix(Arg.Any<string>()).Returns(callInfo => DateTime.TryParse(callInfo.Arg<string>(), out var dt) ? dt : null as DateTime?);

            strategy = new TimeBasedRollingStrategy(@"logs\log", fileSystem, suffixFormatter, () => now);
        }

        [Test]
        public void DiscoverExistingFiles_should_return_files_in_order_provided_by_file_system()
        {
            strategy.DiscoverExistingFiles().Should().Equal(@"logs\log3", @"logs\log1", @"logs\log2");
        }

        [Test]
        public void DiscoverExistingFiles_should_put_files_without_date_suffix_before_files_with_date_suffix()
        {
            fileSystem.GetFilesByPrefix(@"logs\log").Returns(new[] { @"logs\log2018.08.25", @"logs\log2", @"logs\log3" });

            strategy.DiscoverExistingFiles().Should().Equal(@"logs\log2", @"logs\log3", @"logs\log2018.08.25");
        }

        [Test]
        public void DiscoverExistingFiles_should_order_files_with_date_suffix_by_date_suffix()
        {
            fileSystem.GetFilesByPrefix(@"logs\log").Returns(new[] { @"logs\log2018.08.27", @"logs\log2018.08.26", @"logs\log2018.08.25", @"logs\log3" });

            strategy.DiscoverExistingFiles().Should().Equal(@"logs\log3", @"logs\log2018.08.25", @"logs\log2018.08.26", @"logs\log2018.08.27");
        }

        [TestCase("2018-08-25")]
        [TestCase("2018-08-27")]
        public void GetCurrentFile_should_return_base_path_plus_current_date_suffix(string date)
        {
            now = DateTime.Parse(date);

            strategy.GetCurrentFile().Should().Be(@"logs\log" + now.ToString("yyyy.MM.dd"));
        }
    }
}