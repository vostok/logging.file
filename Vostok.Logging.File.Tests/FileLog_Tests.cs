using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.Formatting;

namespace Vostok.Logging.File.Tests
{
    [TestFixture]
    internal class FileLog_Tests
    {
        [Test]
        public void Should_validate_settings()
        {
            var settings = new FileLogSettings()
            {
                OutputTemplate = null
            };

            new Action(() => new FileLog(settings).Info("xx")).Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Should_be_enabled_for_configured_levels([Values] LogLevel level)
        {
            var settings = new FileLogSettings();
            settings.EnabledLogLevels = new[] {LogLevel.Error, LogLevel.Fatal};

            new FileLog(settings).IsEnabledFor(level).Should().Be(settings.EnabledLogLevels.Contains(level));
        }

        [Test]
        public void Should_log_messages()
        {
            CaptureEvents(log => log.Info("Test."))
                .Should()
                .ContainSingle(e => e.MessageTemplate == "Test.");
        }

        [Test]
        public void ForContext_should_add_SourceContext_property()
        {
            CaptureEvents(log => log.ForContext("ctx").Info("Test."))
                .Should()
                .ContainSingle(e => (string)e.Properties[WellKnownProperties.SourceContext] == "ctx");
        }

        [Test]
        public void ForContext_should_replace_SourceContext_property()
        {
            CaptureEvents(
                    log => log
                        .ForContext("ctx")
                        .ForContext("ctx2")
                        .ForContext("ctx3")
                        .Info("Test."))
                .Should()
                .ContainSingle(e => (string)e.Properties[WellKnownProperties.SourceContext] == "ctx3");
        }

        private static IEnumerable<LogEvent> CaptureEvents(Action<FileLog> action)
        {
            var events = new List<LogEvent>();

            var muxer = Substitute.For<IFileLogMuxer>();
            muxer.TryLog(Arg.Do<LogEvent>(e => events.Add(e)), Arg.Any<FilePath>(), Arg.Any<FileLogSettings>(), Arg.Any<object>(), Arg.Any<bool>()).Returns(true);

            var muxerProvider = Substitute.For<IFileLogMuxerProvider>();
            muxerProvider.ObtainMuxer().Returns(muxer);

            var log = new FileLog(muxerProvider, () => new FileLogSettings { OutputTemplate = OutputTemplate.Parse("{Message}") });

            action(log);

            return events;
        }
    }
}