using System;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Tests.Configuration
{
    [TestFixture]
    internal class SafeSettingsProvider_Tests
    {
        [Test]
        public void Should_not_cache_settings_when_there_are_no_errors()
        {
            var settings = new FileLogSettings();
            var settings2 = new FileLogSettings();
            var provider = new SafeSettingsProvider(() => settings);

            provider.Get().Should().BeSameAs(settings);

            settings = settings2;
            provider.Get().Should().BeSameAs(settings2);
        }

        [Test]
        public void Should_catch_errors()
        {
            var settings = new FileLogSettings();
            var oldSettings = settings;
            var provider = new SafeSettingsProvider(() => settings ?? throw new Exception());

            provider.Get().Should().BeSameAs(oldSettings);

            settings = null;
            new Action(() => provider.Get()).Should().NotThrow();
        }

        [Test]
        public void Should_return_cached_value_on_error()
        {
            var settings = new FileLogSettings();
            var oldSettings = settings;
            var provider = new SafeSettingsProvider(() => settings ?? throw new Exception());

            provider.Get().Should().BeSameAs(oldSettings);

            settings = null;
            provider.Get().Should().BeSameAs(oldSettings);
        }

        [Test]
        public void Should_throw_if_there_was_no_cached_value()
        {
            var provider = new SafeSettingsProvider(() => throw new Exception());

            new Action(() => provider.Get()).Should().Throw<Exception>();
        }
    }
}