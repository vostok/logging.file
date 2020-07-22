﻿using System;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Strategies;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Tests.Rolling.Strategies
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
            fileSystem.GetFilesByPrefix("logs/log").Returns(new FilePath[] {"logs/log3", "logs/log1", "logs/log2"});

            var suffixFormatter = Substitute.For<IFileSuffixFormatter<DateTime>>();
            suffixFormatter.FormatSuffix(Arg.Any<DateTime>()).Returns(callInfo => callInfo.Arg<DateTime>().ToString("yyyy-MM-dd"));
            suffixFormatter.TryParseSuffix(Arg.Any<string>()).Returns(callInfo => DateTime.TryParse(callInfo.Arg<string>(), out var dt) ? dt : null as DateTime?);

            strategy = new TimeBasedRollingStrategy(fileSystem, suffixFormatter, () => now, () => '-');
        }
        
        [Test]
        public void DiscoverExistingFiles_should_ignore_files_without_correct_suffix()
        {
            fileSystem.GetFilesByPrefix("logs/log").Returns(new FilePath[] { "logs/log-2018-08-25", "logs/log-2", "logs/log3" });

            strategy.DiscoverExistingFiles("logs/log").Should().Equal("logs/log-2018-08-25");
        }

        [Test]
        public void DiscoverExistingFiles_should_order_files_by_date_suffix()
        {
            fileSystem.GetFilesByPrefix("logs/log").Returns(new FilePath[] { "logs/log-2018-08-27", "logs/log-2018-08-26", "logs/log-2018-08-25", "logs/log3" });

            strategy.DiscoverExistingFiles("logs/log").Should().Equal("logs/log-2018-08-25", "logs/log-2018-08-26", "logs/log-2018-08-27");
        }

        [Test]
        public void DiscoverExistingFiles_should_support_file_extensions()
        {
            fileSystem.GetFilesByPrefix("logs/log.txt").Returns(new FilePath[] { "logs/log-2018-08-27.txt", "logs/log-2018-08-26.txt", "logs/log-2018-08-25.txt" });

            strategy.DiscoverExistingFiles("logs/log.txt").Should().Equal("logs/log-2018-08-25.txt", "logs/log-2018-08-26.txt", "logs/log-2018-08-27.txt");
        }

        [TestCase("2018-08-25")]
        [TestCase("2018-08-27")]
        public void GetCurrentFile_should_return_base_path_plus_current_date_suffix(string date)
        {
            now = DateTime.Parse(date);

            strategy.GetCurrentFile("logs/log").Should().Be((FilePath)"logs/log" + now.ToString("-yyyy-MM-dd"));
        }

        [Test]
        public void GetCurrentFile_should_support_file_extensions()
        {
            now = DateTime.Parse("2018-08-27");

            strategy.GetCurrentFile("logs/log.txt").Should().Be((FilePath)"logs/log-2018-08-27.txt");
        }
    }
}