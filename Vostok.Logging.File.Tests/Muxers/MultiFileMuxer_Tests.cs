using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Muxers;

namespace Vostok.Logging.File.Tests.Muxers
{
    [TestFixture]
    internal class MultiFileMuxer_Tests
    {
        private MultiFileMuxer muxer;
        private ISingleFileMuxerFactory singleMuxerFactory;
        private ISingleFileMuxer singleFileMuxer;
        private TaskCompletionSource<bool> flushBlocker;

        [SetUp]
        public void TestSetup()
        {
            flushBlocker = new TaskCompletionSource<bool>();

            singleFileMuxer = Substitute.For<ISingleFileMuxer>();
            singleFileMuxer.TryAdd(Arg.Any<LogEventInfo>(), Arg.Any<bool>()).Returns(true);
            singleFileMuxer.FlushAsync().Returns(flushBlocker.Task);

            singleMuxerFactory = Substitute.For<ISingleFileMuxerFactory>();
            singleMuxerFactory.Create(Arg.Any<FileLogSettings>()).Returns(singleFileMuxer);

            muxer = new MultiFileMuxer(singleMuxerFactory);
        }

        [Test]
        public void EventsLost_should_return_sum_of_events_lost_in_each_single_file_muxer()
        {
            var muxer1 = Substitute.For<ISingleFileMuxer>();
            var muxer2 = Substitute.For<ISingleFileMuxer>();
            var settings1 = new FileLogSettings();
            var settings2 = new FileLogSettings();
            singleMuxerFactory.Create(settings1).Returns(muxer1);
            singleMuxerFactory.Create(settings2).Returns(muxer2);
            muxer1.EventsLost.Returns(2);
            muxer2.EventsLost.Returns(3);

            Register("log1", settings1);
            Register("log2", settings2);

            muxer.EventsLost.Should().Be(5);
        }

        [Test]
        public void TryAdd_should_return_false_if_there_is_no_registration()
        {
            TryAddEvent().Should().BeFalse();
        }

        [Test]
        public void TryAdd_should_return_true_if_event_was_added()
        {
            Register();

            TryAddEvent().Should().BeTrue();
        }

        [Test]
        public void TryAdd_should_return_false_if_event_was_not_added()
        {
            singleFileMuxer.TryAdd(Arg.Any<LogEventInfo>(), Arg.Any<bool>()).Returns(false);
            Register();

            TryAddEvent().Should().BeFalse();
        }

        [Test]
        public void TryAdd_should_track_owner()
        {
            var owner = new object();
            Register(owner: owner);

            TryAddEvent();
            singleFileMuxer.Received().TryAdd(Arg.Any<LogEventInfo>(), false);
            singleFileMuxer.ClearReceivedCalls();

            TryAddEvent(owner: owner);
            singleFileMuxer.Received().TryAdd(Arg.Any<LogEventInfo>(), true);
        }

        [Test]
        public void FlushAsync_should_return_immediately_if_there_is_no_registration()
        {
            muxer.FlushAsync().IsCompleted.Should().BeTrue();
        }

        [Test]
        public void FlushAsync_should_flush_muxer()
        {
            Register();

            var flushTask = muxer.FlushAsync();
            flushTask.IsCompleted.Should().BeFalse();

            Task.Run(() => flushBlocker.TrySetResult(true));
            flushTask.Wait(100);
            flushTask.IsCompleted.Should().BeTrue();
        }

        [Test]
        public void Global_FlushAsync_should_flush_all_muxers()
        {
            var muxer1 = Substitute.For<ISingleFileMuxer>();
            var muxer2 = Substitute.For<ISingleFileMuxer>();
            var settings1 = new FileLogSettings();
            var settings2 = new FileLogSettings();
            singleMuxerFactory.Create(settings1).Returns(muxer1);
            singleMuxerFactory.Create(settings2).Returns(muxer2);
            Register("log1", settings1);
            Register("log2", settings2);

            muxer.FlushAsync();

            muxer1.Received().FlushAsync();
            muxer2.Received().FlushAsync();
        }

        [Test]
        public void Should_track_reference_count_for_registrations()
        {
            var r1 = Register();
            var r2 = Register();
            var r3 = Register();

            TryAddEvent().Should().BeTrue();

            r1.Dispose();

            TryAddEvent().Should().BeTrue();

            r2.Dispose();
            r3.Dispose();

            TryAddEvent().Should().BeFalse();
        }

        private static LogEventInfo CreateLogEvent(FileLogSettings settings = null)
        {
            return new LogEventInfo(new LogEvent(LogLevel.Info, DateTimeOffset.Now, "Hey!"), settings ?? new FileLogSettings());
        }

        private bool TryAddEvent(LogEventInfo @event = null, object owner = null)
        {
            return muxer.TryAdd("log", @event ?? CreateLogEvent(), owner ?? new object());
        }

        private IMuxerRegistration Register(FilePath path = null, FileLogSettings settings = null, object owner = null)
        {
            return muxer.Register(path ?? "log", settings ?? new FileLogSettings(), owner ?? new object());
        }
    }
}