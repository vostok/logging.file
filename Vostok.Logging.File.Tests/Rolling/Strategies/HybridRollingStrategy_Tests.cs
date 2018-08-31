using System;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Rolling;
using Vostok.Logging.File.Rolling.Strategies;
using Vostok.Logging.File.Rolling.SuffixFormatters;

namespace Vostok.Logging.File.Tests.Rolling.Strategies
{
    [TestFixture]
    internal class HybridRollingStrategy_Tests
    {
        private HybridRollingStrategy strategy;
        private IFileSystem fileSystem;
        private IFileNameTuner fileNameTuner;
        private IRollingStrategy timeStrategy;
        private IRollingStrategy sizeStrategy;

        [SetUp]
        public void TestSetup()
        {
            fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetFilesByPrefix(@"logs\log").Returns(new[] { @"logs\log3", @"logs\log1", @"logs\log2" });

            fileNameTuner = Substitute.For<IFileNameTuner>();
            fileNameTuner.RemoveExtension(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());
            fileNameTuner.RestoreExtension(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>());

            timeStrategy = Substitute.For<IRollingStrategy>();
            sizeStrategy = Substitute.For<IRollingStrategy>();

            var suffixFormatter = Substitute.For<IFileSuffixFormatter<(DateTime, int)>>();
            suffixFormatter.TryParseSuffix(Arg.Any<string>())
                .Returns(callInfo => callInfo.Arg<string>().Split('.').Transform(p => p.Length == 2 && DateTime.TryParse(p[0], out var v) && int.TryParse(p[1], out var w) ? (v, w) : null as (DateTime, int)?));

            strategy = new HybridRollingStrategy(fileSystem, timeStrategy, sizeStrategy, suffixFormatter, fileNameTuner);
        }

        [Test]
        public void DiscoverExistingFiles_should_ignore_files_without_correct_suffix()
        {
            fileSystem.GetFilesByPrefix(@"logs\log").Returns(new[] { @"logs\log2018-08-25", @"logs\log.2", @"logs\log2018-08-26.1" });

            strategy.DiscoverExistingFiles(@"logs\log").Should().Equal(@"logs\log2018-08-26.1");
        }

        [Test]
        public void DiscoverExistingFiles_should_order_files_by_date_suffix_then_by_part_suffix()
        {
            fileSystem.GetFilesByPrefix(@"logs\log").Returns(new[] { @"logs\log2018-08-27.2", @"logs\log2018-08-27.1", @"logs\log2018-08-26.1", @"logs\log2018-08-25.2" });

            strategy.DiscoverExistingFiles(@"logs\log").Should().Equal(@"logs\log2018-08-25.2", @"logs\log2018-08-26.1", @"logs\log2018-08-27.1", @"logs\log2018-08-27.2");
        }

        [Test]
        public void DiscoverExistingFiles_should_support_file_extensions()
        {
            fileSystem.GetFilesByPrefix(@"logs\log").Returns(new[] { @"logs\log2018-08-27.1.txt", @"logs\log2018-08-26.1.txt", @"logs\log2018-08-25.2.txt" });
            fileNameTuner.RemoveExtension(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>().Replace(".txt", ""));
            fileNameTuner.RestoreExtension(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>() + ".txt");

            strategy.DiscoverExistingFiles(@"logs\log.txt").Should().Equal(@"logs\log2018-08-25.2.txt", @"logs\log2018-08-26.1.txt", @"logs\log2018-08-27.1.txt");
        }

        [Test]
        public void GetCurrentFile_should_return_base_path_plus_current_part_inside_current_date_suffix()
        {
            sizeStrategy.GetCurrentFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>() + ".1");
            sizeStrategy.GetCurrentFile(@"logs\log2018-08-27").Returns(@"logs\log2018-08-27.2");

            timeStrategy.GetCurrentFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>() + "2018-08-25");
            strategy.GetCurrentFile(@"logs\log").Should().Be(@"logs\log2018-08-25.1");

            timeStrategy.GetCurrentFile(Arg.Any<string>()).Returns(callInfo => callInfo.Arg<string>() + "2018-08-27");
            strategy.GetCurrentFile(@"logs\log").Should().Be(@"logs\log2018-08-27.2");
        }

        [Test]
        public void GetCurrentFile_should_support_file_extensions()
        {
            sizeStrategy.GetCurrentFile(@"logs\log2018-08-25.txt").Returns(@"logs\log2018-08-25.1.txt");
            sizeStrategy.GetCurrentFile(@"logs\log2018-08-27.txt").Returns(@"logs\log2018-08-27.2.txt");

            timeStrategy.GetCurrentFile(@"logs\log.txt").Returns(@"logs\log2018-08-25.txt");
            strategy.GetCurrentFile(@"logs\log.txt").Should().Be(@"logs\log2018-08-25.1.txt");

            timeStrategy.GetCurrentFile(@"logs\log.txt").Returns(@"logs\log2018-08-27.txt");
            strategy.GetCurrentFile(@"logs\log.txt").Should().Be(@"logs\log2018-08-27.2.txt");
        }
    }
}