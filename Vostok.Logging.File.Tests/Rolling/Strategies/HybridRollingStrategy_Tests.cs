using System;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Strategies;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Tests.Rolling.Strategies
{
    [TestFixture]
    internal class HybridRollingStrategy_Tests
    {
        private HybridRollingStrategy strategy;
        private IFileSystem fileSystem;
        private IRollingStrategy sizeStrategy;
        private IFileSuffixFormatter<DateTime> timeSuffixFormatter;
        private IFileSuffixFormatter<(DateTime, int)> hybridSuffixFormatter;

        [SetUp]
        public void TestSetup()
        {
            fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetFilesByPrefix("logs/log").Returns(new FilePath[] { "logs/log3", "logs/log1", "logs/log2" });

            sizeStrategy = Substitute.For<IRollingStrategy>();

            timeSuffixFormatter = Substitute.For<IFileSuffixFormatter<DateTime>>();
            hybridSuffixFormatter = Substitute.For<IFileSuffixFormatter<(DateTime, int)>>();
            
            hybridSuffixFormatter.TryParseSuffix(Arg.Any<string>())
                .Returns(callInfo => callInfo.Arg<string>().Split('#').Transform(p => p.Length == 2 && DateTime.TryParse(p[0], out var v) && int.TryParse(p[1], out var w) ? (v, w) : null as (DateTime, int)?));

            strategy = new HybridRollingStrategy(fileSystem, sizeStrategy, () => DateTime.Now, timeSuffixFormatter, hybridSuffixFormatter, () => '-');
        }

        [Test]
        public void DiscoverExistingFiles_should_ignore_files_without_correct_suffix()
        {
            fileSystem.GetFilesByPrefix("logs/log").Returns(new FilePath[] { "logs/log2018-08-25", "logs/log#2", "logs/log2018-08-26#1" });

            strategy.DiscoverExistingFiles("logs/log").Should().Equal("logs/log2018-08-26#1");
        }

        [Test]
        public void DiscoverExistingFiles_should_order_files_by_date_suffix_then_by_part_suffix()
        {
            fileSystem.GetFilesByPrefix("logs/log").Returns(new FilePath[] { "logs/log2018-08-27#2", "logs/log2018-08-27#1", "logs/log2018-08-26#1", "logs/log2018-08-25#2" });

            strategy.DiscoverExistingFiles("logs/log").Should().Equal("logs/log2018-08-25#2", "logs/log2018-08-26#1", "logs/log2018-08-27#1", "logs/log2018-08-27#2");
        }

        [Test]
        public void DiscoverExistingFiles_should_support_file_extensions()
        {
            fileSystem.GetFilesByPrefix("logs/log.txt").Returns(new FilePath[] { "logs/log2018-08-27#1.txt", "logs/log2018-08-26#1.txt", "logs/log2018-08-25#2.txt" });

            strategy.DiscoverExistingFiles("logs/log.txt").Should().Equal("logs/log2018-08-25#2.txt", "logs/log2018-08-26#1.txt", "logs/log2018-08-27#1.txt");
        }

        [Test]
        public void GetCurrentFile_should_return_base_path_plus_current_part_inside_current_date_suffix()
        {
            sizeStrategy.GetCurrentFile(Arg.Any<FilePath>()).Returns(callInfo => callInfo.Arg<FilePath>() + "#1");
            sizeStrategy.GetCurrentFile("logs/log-2018-08-27").Returns("logs/log-2018-08-27#2");

            timeSuffixFormatter.FormatSuffix(Arg.Any<DateTime>()).Returns("2018-08-25");
            strategy.GetCurrentFile("logs/log").Should().Be((FilePath)"logs/log-2018-08-25#1");

            timeSuffixFormatter.FormatSuffix(Arg.Any<DateTime>()).Returns("2018-08-27");
            strategy.GetCurrentFile("logs/log").Should().Be((FilePath)"logs/log-2018-08-27#2");
        }
        
        [Test]
        public void GetCurrentFile_should_support_file_extensions()
        {
            sizeStrategy.GetCurrentFile("logs/log-2018-08-25.txt").Returns("logs/log-2018-08-25#1.txt");
            sizeStrategy.GetCurrentFile("logs/log-2018-08-27.txt").Returns("logs/log-2018-08-27#2.txt");

            timeSuffixFormatter.FormatSuffix(Arg.Any<DateTime>()).Returns("2018-08-25");
            strategy.GetCurrentFile("logs/log.txt").Should().Be((FilePath)"logs/log-2018-08-25#1.txt");

            timeSuffixFormatter.FormatSuffix(Arg.Any<DateTime>()).Returns("2018-08-27");
            strategy.GetCurrentFile("logs/log.txt").Should().Be((FilePath)"logs/log-2018-08-27#2.txt");
        }
    }
}