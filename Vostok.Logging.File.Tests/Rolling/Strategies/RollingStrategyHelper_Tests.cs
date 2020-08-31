using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Strategies;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Tests.Rolling.Strategies
{
    [TestFixture]
    internal class RollingStrategyHelper_Tests
    {
        private IFileSuffixFormatter<int> suffixFormatter;
        private IFileSystem fileSystem;
        private char suffixSeparator;

        [SetUp]
        public void TestSetup()
        {
            fileSystem = Substitute.For<IFileSystem>();
            fileSystem.GetFilesByPrefix("logs/log.txt").Returns(new FilePath[] {"logs/log1.txt"});

            suffixFormatter = Substitute.For<IFileSuffixFormatter<int>>();
            suffixFormatter.TryParseSuffix(Arg.Any<string>()).Returns(info => int.TryParse(info.Arg<string>(), out var value) ? value : null as int?);

            suffixSeparator = '-';
        }

        [Test]
        public void Should_parse_suffix_using_provided_formatter()
        {
            RollingStrategyHelper.DiscoverExistingFiles("logs/log.txt", fileSystem, suffixFormatter, suffixSeparator)
                .Single().suffix.Should().Be(1);
        }

        [Test]
        public void Should_not_return_paths_where_suffix_could_not_be_parsed()
        {
            fileSystem.GetFilesByPrefix("logs/log.txt").Returns(new FilePath[] {"logs/log1.txt", "logs/log~.txt"});

            RollingStrategyHelper.DiscoverExistingFiles("logs/log.txt", fileSystem, suffixFormatter, suffixSeparator).Should().HaveCount(1);
        }

        [Test]
        public void Should_return_files_in_correct_order()
        {
            fileSystem.GetFilesByPrefix("logs/log.txt").Returns(new FilePath[] { "logs/log3.txt", "logs/log1.txt", "logs/log2.txt" });

            RollingStrategyHelper.DiscoverExistingFiles("logs/log.txt", fileSystem, suffixFormatter, suffixSeparator).Select(e => e.path)
                .Should().Equal("logs/log1.txt", "logs/log2.txt", "logs/log3.txt");
        }

        [Test]
        public void Should_handle_base_paths_with_placeholders()
        {
            fileSystem.GetFilesByPrefix(Arg.Any<FilePath>()).Returns(new FilePath[]
            {
                "logs/log3file.txt", 
                "logs/log1file.txt", 
                "logs/log", 
                "logs/log513file.txt",
                "logs/log2file.txt"
            });

            RollingStrategyHelper.DiscoverExistingFiles("logs/log{RollingSuffix}file.txt", fileSystem, suffixFormatter, suffixSeparator).Select(e => e.path)
                .Should().Equal("logs/log1file.txt", "logs/log2file.txt", "logs/log3file.txt", "logs/log513file.txt");

            RollingStrategyHelper.DiscoverExistingFiles("logs/log{RollingSuffix}file.txt", fileSystem, suffixFormatter, suffixSeparator).Select(e => e.suffix)
                .Should().Equal(1, 2, 3, 513);
        }

        [TestCase("log", "suffix", "log-suffix")]
        [TestCase("log.txt", "suffix", "log-suffix.txt")]
        [TestCase("log.txt.json", "suffix", "log.txt-suffix.json")]
        [TestCase("log{RollingSuffix}.txt.json", "suffix", "log-suffix.txt.json")]
        [TestCase("{RollingSuffix}.txt.json", "suffix", "suffix.txt.json")]
        [TestCase("log.{RollingSuffix}.txt.json", "suffix", "log.suffix.txt.json")]
        [TestCase("log", "suffix", "log_suffix", '_')]
        [TestCase("log.txt", "suffix", "log_suffix.txt", '_')]
        [TestCase("log{RollingSuffix}.txt.json", "suffix", "log_suffix.txt.json", '_')]
        public void AddSuffix_should_correctly_insert_the_suffix(string basePath, string suffix, string expected, char suffixSeparator = '-')
        {
            var path = RollingStrategyHelper.AddSuffix(new FilePath(basePath), suffix, false, suffixSeparator).NormalizedPath;

            path = path.Remove(0, Environment.CurrentDirectory.Length);
            path = path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            path.Should().Be(expected);
        }
    }
}