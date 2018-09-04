using System;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Tests.Rolling.Suffixes
{
    [TestFixture]
    internal class HybridSuffixFormatter_Tests
    {
        private HybridSuffixFormatter suffixFormatter;
        private IFileSuffixFormatter<DateTime> timeSuffixFormatter;
        private IFileSuffixFormatter<int> sizeSuffixFormatter;

        [SetUp]
        public void TestSetup()
        {
            timeSuffixFormatter = Substitute.For<IFileSuffixFormatter<DateTime>>();
            sizeSuffixFormatter = Substitute.For<IFileSuffixFormatter<int>>();

            suffixFormatter = new HybridSuffixFormatter(timeSuffixFormatter, sizeSuffixFormatter);
        }

        [Test]
        public void FormatSuffix_should_be_not_supported()
        {
            new Action(() => suffixFormatter.FormatSuffix((DateTime.Now, 42))).Should().Throw<NotSupportedException>();
        }

        [Test]
        public void TryParseSuffix_should_split_suffix_before_last_dash()
        {
            timeSuffixFormatter.TryParseSuffix("1-2-3-4").Returns(default(DateTime));
            sizeSuffixFormatter.TryParseSuffix("-5").Returns(5);

            suffixFormatter.TryParseSuffix("1-2-3-4-5").Should().Be((default(DateTime), 5));
        }

        [Test]
        public void TryParseSuffix_should_return_null_if_there_is_no_dash()
        {
            timeSuffixFormatter.TryParseSuffix(Arg.Any<string>()).Returns(default(DateTime));
            sizeSuffixFormatter.TryParseSuffix(Arg.Any<string>()).Returns(5);

            suffixFormatter.TryParseSuffix("12345").Should().BeNull();
        }

        [Test]
        public void TryParseSuffix_should_return_null_if_time_suffix_parsing_fails()
        {
            sizeSuffixFormatter.TryParseSuffix(Arg.Any<string>()).Returns(5);

            suffixFormatter.TryParseSuffix("1-2-3-4-5").Should().BeNull();
        }

        [Test]
        public void TryParseSuffix_should_return_null_if_size_suffix_parsing_fails()
        {
            timeSuffixFormatter.TryParseSuffix(Arg.Any<string>()).Returns(default(DateTime));

            suffixFormatter.TryParseSuffix("1-2-3-4-5").Should().BeNull();
        }
    }
}