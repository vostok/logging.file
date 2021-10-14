using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
            eventsWriterProvider.ObtainWriterAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(eventsWriter));

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
        public void TryAdd_should_return_true_if_event_was_added_successfully([Values] bool fromOwner)
        {
            muxer.TryAdd(CreateEventInfo(), fromOwner).Should().BeTrue();
        }

        [Test]
        public void TryAdd_should_wait_for_buffer_space_if_specified()
        {
            singleFileWorker = new SingleFileWorker();
            muxer = new SingleFileMuxer(writerProviderFactory, singleFileWorker, new FileLogSettings
            {
                RetryIfQueueIsFull = false,
                EventsQueueCapacity = 1
            });

            for (var i = 0; i < 10; i++)
                muxer.TryAdd(CreateEventInfo(), false).Should().BeTrue();

            muxer.Dispose();
            
            eventsWriter.Received(10)
                .WriteEvents(
                    Arg.Any<LogEventInfo[]>(),
                    Arg.Is(1));
        }

        [Test]
        public void TryAdd_should_return_false_if_event_was_not_added([Values] bool fromOwner)
        {
            muxer = new SingleFileMuxer(writerProviderFactory, singleFileWorker, new FileLogSettings {EventsQueueCapacity = 0});

            muxer.TryAdd(CreateEventInfo(), fromOwner).Should().BeFalse();
        }

        [Test]
        public void Should_eventually_write_added_events()
        {
            muxer.TryAdd(CreateEventInfo(), true);

            new Action(() => singleFileWorker.Received().WritePendingEventsAsync(
                    Arg.Any<IEventsWriterProvider>(),
                    Arg.Is<ConcurrentBoundedQueue<LogEventInfo>>(q => q.Count == 1),
                    Arg.Any<LogEventInfo[]>(), 
                    Arg.Any<AtomicLong>(),
                    Arg.Any<AtomicLong>(),
                    Arg.Any<CancellationToken>()))
                .ShouldPassIn(10.Seconds());
        }

        [Test]
        public void Flush_should_wait_until_current_events_are_written()
        {
            var iterationBlocker = new TaskCompletionSource<bool>();
            singleFileWorker.WritePendingEventsAsync(
                    Arg.Any<IEventsWriterProvider>(),
                    Arg.Any<ConcurrentBoundedQueue<LogEventInfo>>(),
                    Arg.Any<LogEventInfo[]>(),
                    Arg.Any<AtomicLong>(),
                    Arg.Any<AtomicLong>(),
                    Arg.Any<CancellationToken>())
                .Returns(iterationBlocker.Task);
            muxer.TryAdd(CreateEventInfo(), true);

            var flushTask = muxer.FlushAsync();

            flushTask.Wait(50);
            flushTask.IsCompleted.Should().BeFalse();

            Task.Run(() => iterationBlocker.TrySetResult(true));
            flushTask.Wait(10.Seconds());
            flushTask.IsCompleted.Should().BeTrue();
            
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
        public void Dispose_should_wait_until_worker_stops()
        {
            var iterationBlocker = new TaskCompletionSource<bool>();
            singleFileWorker.WritePendingEventsAsync(
                    Arg.Any<IEventsWriterProvider>(),
                    Arg.Any<ConcurrentBoundedQueue<LogEventInfo>>(),
                    Arg.Any<LogEventInfo[]>(),
                    Arg.Any<AtomicLong>(),
                    Arg.Any<AtomicLong>(),
                    Arg.Any<CancellationToken>())
                .Returns(iterationBlocker.Task);
            muxer.TryAdd(CreateEventInfo(), true);

            var disposeTask = Task.Run(() => muxer.Dispose());

            disposeTask.Wait(50);
            disposeTask.IsCompleted.Should().BeFalse();

            iterationBlocker.TrySetResult(true);
            disposeTask.Wait(10.Seconds());
            disposeTask.IsCompleted.Should().BeTrue();
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

        [Test]
        public void Should_not_break_if_worker_throws_exception()
        {
            var receivedCalls = 0;
            singleFileWorker.WritePendingEventsAsync(
                    Arg.Any<IEventsWriterProvider>(),
                    Arg.Any<ConcurrentBoundedQueue<LogEventInfo>>(),
                    Arg.Any<LogEventInfo[]>(),
                    Arg.Any<AtomicLong>(),
                    Arg.Any<AtomicLong>(),
                    Arg.Any<CancellationToken>())
                .Throws<Exception>().AndDoes(_ => receivedCalls++);

            muxer.TryAdd(CreateEventInfo(), true).Should().BeTrue();
            muxer.TryAdd(CreateEventInfo(), true).Should().BeTrue();

            new Action(() => receivedCalls.Should().BeGreaterThan(1))
                .ShouldPassIn(5.Seconds());

            Console.WriteLine(); // (krait): to flush console output
        }

        private static LogEventInfo CreateEventInfo(FileLogSettings settings = null)
        {
            return new LogEventInfo(new LogEvent(LogLevel.Info, DateTimeOffset.Now, ""), settings ?? new FileLogSettings());
        }
    }
}