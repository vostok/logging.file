using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Tests.Rolling.Suffixes
{
    [TestFixture]
    internal class SizeBasedSuffixFormatter_Tests
    {
        private SizeBasedSuffixFormatter suffixFormatter;

        [SetUp]
        public void TestSetup()
        {
            suffixFormatter = new SizeBasedSuffixFormatter();
        }

        [Test]
        public void FormatSuffix_should_return_part_number_prefixed_by_dash()
        {
            suffixFormatter.FormatSuffix(42).Should().Be("42");
        }

        [Test]
        public void TryParseSuffix_should_correctly_parse_part_number()
        {
            suffixFormatter.TryParseSuffix("4").Should().Be(4);
        }

        [TestCase("")]
        [TestCase("-4")]
        [TestCase("-1-2")]
        [TestCase("-2018-08-31-1")]
        public void TryParseSuffix_should_return_null_for_incorrectly_formatted_suffixes(string suffix)
        {
            suffixFormatter.TryParseSuffix(suffix).Should().BeNull();
        }
    }
}