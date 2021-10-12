using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Commons.Testing;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Abstractions.Values;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Muxers;
using Vostok.Logging.Formatting;

namespace Vostok.Logging.File.Tests
{
    [TestFixture]
    internal class FileLog_Tests
    {
        private IMuxerRegistration registration;
        private IMultiFileMuxer muxer;
        private List<LogEvent> capturedEvents;
        private FileLog log;
        private FileLogSettings settings;

        [SetUp]
        public void TestSetup()
        {
            capturedEvents = new List<LogEvent>();

            registration = Substitute.For<IMuxerRegistration>();
            registration.IsValid("logs/log").Returns(true);

            muxer = Substitute.For<IMultiFileMuxer>();
            muxer.TryAdd(Arg.Any<FilePath>(), Arg.Do<LogEventInfo>(e => capturedEvents.Add(e.Event)), Arg.Any<WeakReference>()).Returns(true);
            muxer.Register(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>(), Arg.Any<WeakReference>()).Returns(registration);

            settings = new FileLogSettings {FilePath = "logs/log", OutputTemplate = OutputTemplate.Parse("{Message}"), EnableFileLogSettingsCache = false};

            log = new FileLog(muxer, () => settings);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            log.Dispose();
            registration.Dispose();
            log = null;
            registration = null;
            muxer = null;
        }

        [Test]
        public void Should_validate_settings()
        {
            settings = new FileLogSettings()
            {
                OutputTemplate = null
            };

            new Action(() => new FileLog(settings)).Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Should_be_enabled_for_configured_levels([Values] LogLevel level)
        {
            settings = new FileLogSettings
            {
                EnabledLogLevels = new[] {LogLevel.Error, LogLevel.Fatal}
            };

            new FileLog(settings).IsEnabledFor(level).Should().Be(settings.EnabledLogLevels.Contains(level));
        }

        [Test]
        public void Should_obtain_registration_before_logging()
        {
            log.Info("Test.");

            Received.InOrder(
                () =>
                {
                    muxer.Register("logs/log", Arg.Any<FileLogSettings>(), Arg.Any<WeakReference>());
                    muxer.TryAdd("logs/log", Arg.Any<LogEventInfo>(), Arg.Any<WeakReference>());
                });
        }

        [Test]
        public void Should_obtain_registration_only_once()
        {
            log.Info("Test.");
            log.Info("Test.");
            log.Info("Test.");

            muxer.Received(1).Register("logs/log", Arg.Any<FileLogSettings>(), Arg.Any<WeakReference>());
        }

        [Test]
        public void Should_dispose_registration_on_dispose()
        {
            log.Info("Test.");
            log.Dispose();

            registration.Received().Dispose();
        }

        [Test]
        public void Should_dispose_registration_on_path_change()
        {
            registration.IsValid("xxx").Returns(false, false, true);
            log.Info("Test.");

            settings = new FileLogSettings {FilePath = "xxx"};
            log.Info("Test.");

            registration.Received().Dispose();
        }

        [Test]
        public void Should_obtain_new_registration_on_path_change()
        {
            registration.IsValid("xxx").Returns(false, false, true);
            log.Info("Test.");

            settings = new FileLogSettings {FilePath = "xxx"};
            log.Info("Test.");

            muxer.Received(1).Register("logs/log", Arg.Any<FileLogSettings>(), Arg.Any<WeakReference>());
            muxer.Received(1).Register("xxx", Arg.Any<FileLogSettings>(), Arg.Any<WeakReference>());
        }

        [Test]
        public void Should_flush_by_updated_file_path()
        {
            registration.IsValid("xxx").Returns(false, false, true);
            settings = new FileLogSettings {FilePath = "xxx"};
            log.Info("Test.");
            log.Flush();

            muxer.Received().FlushAsync("xxx");
        }
        
        [Test]
        public void Should_flush_if_synchronous()
        {
            registration.IsValid("xxx").Returns(false, false, true);
            settings = new FileLogSettings {FilePath = "xxx", WriteSynchronous = true};
            log.Info("Test.");
            
            muxer.Received().FlushAsync("xxx");
        }

        [Test]
        public void Should_increment_events_lost_on_losing_event()
        {
            muxer.TryAdd(Arg.Any<FilePath>(), Arg.Any<LogEventInfo>(), Arg.Any<WeakReference>()).Returns(false);

            log.Info("Test.");
            log.Info("Test.");

            log.EventsLost.Should().Be(2);
        }
        
        [Test]
        public void Should_not_lost_events_if_synchronous()
        {
            muxer.TryAdd(Arg.Any<FilePath>(), Arg.Any<LogEventInfo>(), Arg.Any<WeakReference>()).Returns(false);
            settings = new FileLogSettings {WriteSynchronous = true};
            
            var task = Task.Run(() => log.Info("Test."));
            
            task.ShouldNotCompleteIn(1.Seconds());
            
            muxer.TryAdd(Arg.Any<FilePath>(), Arg.Any<LogEventInfo>(), Arg.Any<WeakReference>()).Returns(true);

            task.ShouldCompleteIn(1.Seconds());
            
            log.EventsLost.Should().Be(0);
        }

        [Test]
        public void Should_log_messages()
        {
            log.Info("Test.");

            capturedEvents.Should().ContainSingle(e => e.MessageTemplate == "Test.");
        }

        [Test]
        public void ForContext_should_add_SourceContext_property()
        {
            log.ForContext("ctx").Info("Test.");

            capturedEvents.Should().ContainSingle(e => e.Properties[WellKnownProperties.SourceContext].Equals(new SourceContextValue("ctx")));
        }

        [Test]
        public void ForContext_should_accumulate_SourceContext_property()
        {
            log
                .ForContext("ctx")
                .ForContext("ctx2")
                .ForContext("ctx3")
                .Info("Test.");

            capturedEvents.Should()
                .ContainSingle(e => e.Properties[WellKnownProperties.SourceContext].Equals(new SourceContextValue(new [] {"ctx", "ctx2", "ctx3"})));
        }

        [Test]
        public void Should_not_log_after_dispose()
        {
            log.Info("Before dispose");
            log.Dispose();
            log.Info("After dispose");

            capturedEvents.Should().Contain(e => e.MessageTemplate.Contains("Before dispose"));
            capturedEvents.Should().NotContain(e => e.MessageTemplate.Contains("After dispose"));
        }
        
        [Test]
        public void Should_not_log_after_dispose_if_synchronous()
        {
            settings = new FileLogSettings {WriteSynchronous = true};
            
            log.Info("Before dispose");
            log.Dispose();
            log.Info("After dispose");

            capturedEvents.Should().Contain(e => e.MessageTemplate.Contains("Before dispose"));
            capturedEvents.Should().NotContain(e => e.MessageTemplate.Contains("After dispose"));
        }
    }
}