using System;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Tests.Rolling.Suffixes
{
    [TestFixture]
    internal class TimeBasedSuffixFormatter_Tests
    {
        private TimeBasedSuffixFormatter suffixFormatter;
        private RollingPeriod rollingPeriod;

        [SetUp]
        public void TestSetup()
        {
            suffixFormatter = new TimeBasedSuffixFormatter(() => rollingPeriod);
        }

        [Test]
        public void FormatSuffix_should_use_correct_time_format_for_Day_period()
        {
            rollingPeriod = RollingPeriod.Day;

            suffixFormatter.FormatSuffix(DateTime.Parse("2018-08-31T06:24:13")).Should().Be("2018-08-31");
        }

        [Test]
        public void FormatSuffix_should_use_correct_time_format_for_Hour_period()
        {
            rollingPeriod = RollingPeriod.Hour;

            suffixFormatter.FormatSuffix(DateTime.Parse("2018-08-31T06:24:13")).Should().Be("2018-08-31-06");
        }

        [Test]
        public void FormatSuffix_should_use_correct_time_format_for_Minute_period()
        {
            rollingPeriod = RollingPeriod.Minute;

            suffixFormatter.FormatSuffix(DateTime.Parse("2018-08-31T06:24:13")).Should().Be("2018-08-31-06-24");
        }

        [Test]
        public void FormatSuffix_should_use_correct_time_format_for_Second_period()
        {
            rollingPeriod = RollingPeriod.Second;

            suffixFormatter.FormatSuffix(DateTime.Parse("2018-08-31T06:24:13")).Should().Be("2018-08-31-06-24-13");
        }

        [Test]
        public void TryParseSuffix_should_support_format_for_Day_period()
        {
            suffixFormatter.TryParseSuffix("2018-08-31").Should().Be(DateTime.Parse("2018-08-31"));
        }

        [Test]
        public void TryParseSuffix_should_support_format_for_Hour_period()
        {
            suffixFormatter.TryParseSuffix("2018-08-31-06").Should().Be(DateTime.Parse("2018-08-31T06:00:00"));
        }

        [Test]
        public void TryParseSuffix_should_support_format_for_Minute_period()
        {
            suffixFormatter.TryParseSuffix("2018-08-31-06-24").Should().Be(DateTime.Parse("2018-08-31T06:24:00"));
        }

        [Test]
        public void TryParseSuffix_should_support_format_for_Second_period()
        {
            suffixFormatter.TryParseSuffix("2018-08-31-06-24-13").Should().Be(DateTime.Parse("2018-08-31T06:24:13"));
        }

        [TestCase("")]
        [TestCase("-2018-08-31")]
        [TestCase("#2018-08-21")]
        [TestCase("-1")]
        [TestCase("2018-08-31-066-24-13")]
        [TestCase("2018-08-31-06-24-13-1")]
        [TestCase("2018-08-31-6-24-13")]
        public void TryParseSuffix_should_return_null_for_incorrectly_formatted_suffixes(string suffix)
        {
            suffixFormatter.TryParseSuffix(suffix).Should().BeNull();
        }
    }
}