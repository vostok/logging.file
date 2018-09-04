using System;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Commons.Testing;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;

namespace Vostok.Logging.File.Tests
{
    [TestFixture]
    internal class SingleFileMuxer_Tests
    {
        private IEventsWriter eventsWriter;
        private IEventsWriterProvider eventsWriterProvider;
        private SingleFileMuxer muxer;
        private object owner;
        private LogEventInfo[] tempBuffer;

        [SetUp]
        public void TestSetup()
        {
            eventsWriter = Substitute.For<IEventsWriter>();

            eventsWriterProvider = Substitute.For<IEventsWriterProvider>();
            eventsWriterProvider.ObtainWriter().Returns(eventsWriter);
            eventsWriterProvider.IsHealthy.Returns(true);

            owner = new object();
            tempBuffer = new LogEventInfo[1];

            muxer = new SingleFileMuxer(null, new FilePath("log"), new FileLogSettings(), eventsWriterProvider);
        }

        [Test]
        public void EventsLost_should_be_incremented_after_losing_an_event()
        {
            muxer = new SingleFileMuxer(owner, new FilePath("log"), new FileLogSettings {EventsQueueCapacity = 0}, eventsWriterProvider);

            muxer.TryAdd(CreateEventInfo(), owner);
            muxer.TryAdd(CreateEventInfo(), owner);

            muxer.EventsLost.Should().Be(2);
        }

        [Test]
        public void TryLog_should_return_true_if_event_was_added_successfully()
        {
            muxer.TryAdd(CreateEventInfo(), owner).Should().BeTrue();
        }

        [Test]
        public void TryLog_should_return_false_if_event_was_not_added()
        {
            muxer = new SingleFileMuxer(owner, new FilePath("log"), new FileLogSettings { EventsQueueCapacity = 0 }, eventsWriterProvider);

            muxer.TryAdd(CreateEventInfo(), owner).Should().BeFalse();
        }

        [Test]
        public void Should_write_added_events()
        {
            var e = CreateEventInfo();

            muxer.TryAdd(e, owner);

            muxer.WriteEvents(tempBuffer);

            eventsWriter.Received().WriteEvents(
                Arg.Is<LogEventInfo[]>(events =>
                    events.Length == 1 && ReferenceEquals(events[0], e)), 1);
        }

        [Test]
        public void Flush_should_wait_until_current_events_are_written()
        {
            muxer.TryAdd(CreateEventInfo(), owner);

            var task = muxer.FlushAsync();
            task.IsCompleted.Should().BeFalse();

            muxer.WriteEvents(tempBuffer);

            task.IsCompleted.Should().BeTrue();
        }

        private static LogEventInfo CreateEventInfo(FileLogSettings settings = null)
        {
            return new LogEventInfo(new LogEvent(LogLevel.Info, DateTimeOffset.Now, ""), settings ?? new FileLogSettings());
        }
    }
}