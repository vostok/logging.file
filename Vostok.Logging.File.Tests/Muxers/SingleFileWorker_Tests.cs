using System;
using System.Threading;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Commons.Collections;
using Vostok.Commons.Threading;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;
using Vostok.Logging.File.Muxers;

namespace Vostok.Logging.File.Tests.Muxers
{
    [TestFixture]
    internal class SingleFileWorker_Tests
    {
        private SingleFileWorker worker;
        private IEventsWriterProvider writerProvider;
        private IEventsWriter writer;
        private ConcurrentBoundedQueue<LogEventInfo> events;
        private LogEventInfo[] buffer;
        private AtomicLong eventsLost;

        [SetUp]
        public void TestSetup()
        {
            writer = Substitute.For<IEventsWriter>();

            writerProvider = Substitute.For<IEventsWriterProvider>();
            writerProvider.ObtainWriterAsync(Arg.Any<CancellationToken>()).Returns(writer);

            worker = new SingleFileWorker();

            events = new ConcurrentBoundedQueue<LogEventInfo>(2);
            buffer = new LogEventInfo[1];
            eventsLost = new AtomicLong(0);
        }

        [Test]
        public void Should_write_drained_events()
        {
            var e = CreateLogEvent();
            
            events.TryAdd(e);

            WriteEvents().Should().BeTrue();

            writer.Received(1).WriteEvents(buffer, 1);
        }

        [Test]
        public void Should_clear_intermidate_buffer_after_writing()
        {
            var e = CreateLogEvent();

            events.TryAdd(e);

            WriteEvents().Should().BeTrue();

            buffer[0].Should().BeNull();
        }

        [Test]
        public void Should_not_write_events_if_queue_is_empty()
        {
            WriteEvents().Should().BeTrue();

            writer.DidNotReceive().WriteEvents(Arg.Any<LogEventInfo[]>(), Arg.Any<int>());
        }

        [Test]
        public void Should_drain_multiple_times_if_there_are_many_events()
        {
            events.TryAdd(CreateLogEvent());
            events.TryAdd(CreateLogEvent());

            WriteEvents().Should().BeTrue();

            writer.Received(2).WriteEvents(buffer, 1);
        }

        [Test]
        public void Should_return_false_if_writer_throws_error()
        {
            writer.When(w => w.WriteEvents(Arg.Any<LogEventInfo[]>(), Arg.Any<int>())).Throw<Exception>();
            events.TryAdd(CreateLogEvent());

            WriteEvents().Should().BeFalse();
        }

        [Test]
        public void Should_increment_events_lost_on_failure()
        {
            writer.When(w => w.WriteEvents(Arg.Any<LogEventInfo[]>(), Arg.Any<int>())).Throw<Exception>();
            events.TryAdd(CreateLogEvent());

            WriteEvents();

            eventsLost.Value.Should().Be(1);
        }

        [Test]
        public void Should_stop_draining_events_after_error()
        {
            writer.When(w => w.WriteEvents(Arg.Any<LogEventInfo[]>(), Arg.Any<int>())).Throw<Exception>();
            events.TryAdd(CreateLogEvent());
            events.TryAdd(CreateLogEvent());
            events.TryAdd(CreateLogEvent());

            WriteEvents();

            writer.Received(2).WriteEvents(buffer, 1);
        }

        [Test]
        public void Should_log_about_lost_events()
        {
            writer.When(w => w.WriteEvents(Arg.Any<LogEventInfo[]>(), Arg.Any<int>())).Throw<Exception>();
            events.TryAdd(CreateLogEvent());

            WriteEvents();

            buffer[0].Event.Level.Should().Be(LogLevel.Warn);
        }

        private bool WriteEvents()
        {
            return worker.WritePendingEventsAsync(writerProvider, events, buffer, eventsLost, new AtomicLong(0), new CancellationToken()).GetAwaiter().GetResult();
        }

        private static LogEventInfo CreateLogEvent(FileLogSettings settings = null)
        {
            return new LogEventInfo(new LogEvent(LogLevel.Info, DateTimeOffset.Now, "Hey!"), settings ?? new FileLogSettings());
        }
    }
}