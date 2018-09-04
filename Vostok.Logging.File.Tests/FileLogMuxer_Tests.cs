using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    internal class FileLogMuxer_Tests
    {
        private FileLogMuxer muxer;
        private ISingleFileMuxerFactory singleMuxerFactory;
        private ISingleFileMuxer singleFileMuxer;

        [SetUp]
        public void TestSetup()
        {
            singleFileMuxer = Substitute.For<ISingleFileMuxer>();
            singleFileMuxer.TryAdd(Arg.Any<LogEventInfo>(), Arg.Any<object>()).Returns(true);

            singleMuxerFactory = Substitute.For<ISingleFileMuxerFactory>();
            singleMuxerFactory.CreateMuxer(Arg.Any<object>(), Arg.Any<FilePath>(), Arg.Any<FileLogSettings>()).Returns(singleFileMuxer);

            muxer = new FileLogMuxer(100, singleMuxerFactory);
        }

        [Test]
        public void EventsLost_should_return_sum_of_events_lost_in_each_single_file_muxer()
        {
            AddEvent();
            singleFileMuxer.EventsLost.Returns(2);

            muxer.EventsLost.Should().Be(2);
        }

        [Test]
        public void TryLog_should_return_true_if_event_was_added_successfully()
        {
            AddEvent().Should().BeTrue();
        }

        [Test]
        public void TryLog_should_return_false_if_event_was_not_added()
        {
            singleFileMuxer.TryAdd(Arg.Any<LogEventInfo>(), Arg.Any<object>()).Returns(false);

            AddEvent().Should().BeFalse();
        }

        [Test]
        public void Flush_should_complete_immediately_when_muxer_is_not_initialized_yet()
        {
            muxer.FlushAsync().IsCompleted.Should().BeTrue();
        }

        private static LogEvent CreateLogEvent()
        {
            return new LogEvent(LogLevel.Info, DateTimeOffset.Now, "Hey!");
        }

        private bool AddEvent(LogEvent @event = null)
        {
            var result = muxer.TryLog(@event ?? CreateLogEvent(), new FilePath("log"), new FileLogSettings(), null, false);
            return result;
        }
    }
}