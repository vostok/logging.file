using System;
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
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Muxers;

namespace Vostok.Logging.File.Tests.Muxers
{
    [TestFixture]
    internal class SynchronousSingleFileMuxer_Tests
    {
        private IEventsWriter eventsWriter;
        private IEventsWriterProvider eventsWriterProvider;
        private IEventsWriterProviderFactory writerProviderFactory;
        private SynchronousSingleFileMuxer muxer;
        
        [SetUp]
        public void TestSetup()
        {
            eventsWriter = Substitute.For<IEventsWriter>();

            eventsWriterProvider = Substitute.For<IEventsWriterProvider>();
            eventsWriterProvider.ObtainWriterAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(eventsWriter));

            writerProviderFactory = Substitute.For<IEventsWriterProviderFactory>();
            writerProviderFactory.CreateProvider(Arg.Any<FilePath>(), Arg.Any<Func<FileLogSettings>>()).Returns(eventsWriterProvider);

            muxer = new SynchronousSingleFileMuxer(writerProviderFactory, new FileLogSettings());
        }
        
        [Test]
        public void TryAdd_should_return_true_if_event_was_added_successfully()
        {
            muxer.TryAdd(CreateEventInfo(), true).Should().BeTrue();
        }
        
        [Test]
        public void TryAdd_should_return_false_if_event_was_not_added_successfully()
        {
            eventsWriter.When(w => w.WriteEvents(Arg.Any<LogEventInfo[]>(), Arg.Any<int>())).Throw<Exception>();

            muxer.TryAdd(CreateEventInfo(), true).Should().BeFalse();
        }
        
        [Test]
        public void TryAdd_should_return_false_if_disposed()
        {
            muxer.TryAdd(CreateEventInfo(), true).Should().BeTrue();
            
            muxer.Dispose();
            
            muxer.TryAdd(CreateEventInfo(), true).Should().BeFalse();
        }

        private static LogEventInfo CreateEventInfo(FileLogSettings settings = null)
        {
            return new LogEventInfo(new LogEvent(LogLevel.Info, DateTimeOffset.Now, ""), settings ?? new FileLogSettings());
        }
    }
}