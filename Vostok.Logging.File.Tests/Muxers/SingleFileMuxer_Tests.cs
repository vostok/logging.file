using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Commons.Collections;
using Vostok.Commons.Testing;
using Vostok.Commons.Threading;
using Vostok.Logging.Abstractions;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Muxers;

namespace Vostok.Logging.File.Tests.Muxers
{
    [TestFixture]
    internal class SingleFileMuxer_Tests
    {
        private IEventsWriter eventsWriter;
        private IEventsWriterProvider eventsWriterProvider;
        private IEventsWriterProviderFactory writerProviderFactory;
        private SingleFileMuxer muxer;
        private Func<FileLogSettings> settingsInsideMuxer;
        private ISingleFileWorker singleFileWorker;

        [SetUp]
        public void TestSetup()
        {
            eventsWriter = Substitute.For<IEventsWriter>();

            eventsWriterProvider = Substitute.For<IEventsWriterProvider>();
            eventsWriterProvider.ObtainWriterAsync().Returns(Task.FromResult(eventsWriter));

            writerProviderFactory = Substitute.For<IEventsWriterProviderFactory>();
            writerProviderFactory.CreateProvider(Arg.Any<FilePath>(), Arg.Do<Func<FileLogSettings>>(x => settingsInsideMuxer = x)).Returns(eventsWriterProvider);

            singleFileWorker = Substitute.For<ISingleFileWorker>();
            singleFileWorker.WritePendingEventsAsync(
                    Arg.Any<IEventsWriterProvider>(),
                    Arg.Any<ConcurrentBoundedQueue<LogEventInfo>>(),
                    Arg.Any<LogEventInfo[]>(),
                    Arg.Any<AtomicLong>(),
                    Arg.Any<AtomicLong>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(true));

            muxer = new SingleFileMuxer(writerProviderFactory, singleFileWorker, new FileLogSettings());
        }

        [Test]
        public void EventsLost_should_be_incremented_after_losing_an_event()
        {
            muxer = new SingleFileMuxer(writerProviderFactory, singleFileWorker, new FileLogSettings {EventsQueueCapacity = 0});

            muxer.TryAdd(CreateEventInfo(), true);
            muxer.TryAdd(CreateEventInfo(), true);

            muxer.EventsLost.Should().Be(2);
        }

        [Test]
        public void TryAdd_should_return_true_if_event_was_added_successfully()
        {
            muxer.TryAdd(CreateEventInfo(), true).Should().BeTrue();
        }

        [Test]
        public void TryAdd_should_return_false_if_event_was_not_added()
        {
            muxer = new SingleFileMuxer(writerProviderFactory, singleFileWorker, new FileLogSettings {EventsQueueCapacity = 0});

            muxer.TryAdd(CreateEventInfo(), true).Should().BeFalse();
        }

        [Test]
        public void Should_eventually_write_added_events()
        {
            var e = CreateEventInfo();

            muxer.TryAdd(CreateEventInfo(), true);

            new Action(() => singleFileWorker.Received().WritePendingEventsAsync(
                    Arg.Any<IEventsWriterProvider>(),
                    Arg.Is<ConcurrentBoundedQueue<LogEventInfo>>(q => q.Count == 1),
                    Arg.Any<LogEventInfo[]>(), 
                    Arg.Any<AtomicLong>(),
                    Arg.Any<AtomicLong>(),
                    Arg.Any<CancellationToken>()))
                .ShouldPassIn(1.Seconds());
        }

        [Test]
        public void Flush_should_wait_until_current_events_are_written()
        {
            muxer.TryAdd(CreateEventInfo(), true);
            muxer.FlushAsync().Wait();

            singleFileWorker.Received().WritePendingEventsAsync(
                Arg.Any<IEventsWriterProvider>(),
                Arg.Is<ConcurrentBoundedQueue<LogEventInfo>>(q => q.Count == 1),
                Arg.Any<LogEventInfo[]>(), 
                Arg.Any<AtomicLong>(),
                Arg.Any<AtomicLong>(),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public void Flush_should_complete_immediately_when_muxer_is_not_initialized_yet()
        {
            muxer.FlushAsync().IsCompleted.Should().BeTrue();
        }

        [Test]
        public void Dispose_should_dispose_writer_provider()
        {
            muxer.Dispose();

            eventsWriterProvider.Received().Dispose();
        }

        [Test]
        public void Should_update_settings_only_from_owner()
        {
            var initialBufferSize = new FileLogSettings().OutputBufferSize;

            muxer.TryAdd(CreateEventInfo(new FileLogSettings {OutputBufferSize = 10}), false);
            settingsInsideMuxer().OutputBufferSize.Should().Be(initialBufferSize);

            muxer.TryAdd(CreateEventInfo(new FileLogSettings {OutputBufferSize = 10}), true);
            settingsInsideMuxer().OutputBufferSize.Should().Be(10);
        }

        private static LogEventInfo CreateEventInfo(FileLogSettings settings = null)
        {
            return new LogEventInfo(new LogEvent(LogLevel.Info, DateTimeOffset.Now, ""), settings ?? new FileLogSettings());
        }
    }
}