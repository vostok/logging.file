using System;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Tests
{
    [TestFixture]
    internal class FileLogMuxerProvider_Tests
    {
        private FileLogMuxerProvider muxerProvider;

        [SetUp]
        public void TestSetup()
        {
            var factory = Substitute.For<ISingleFileMuxerFactory>();

            muxerProvider = new FileLogMuxerProvider(factory);
        }

        [Test]
        public void Should_validate_settings()
        {
            var invalidSettings = new FileLogGlobalSettings { EventsTemporaryBufferCapacity = -1 };

            new Action(() => muxerProvider.UpdateSettings(invalidSettings))
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void Should_create_muxer_only_once()
        {
            var muxer1 = muxerProvider.ObtainMuxer();

            muxerProvider.UpdateSettings(new FileLogGlobalSettings { EventsTemporaryBufferCapacity = 10 });

            var muxer2 = muxerProvider.ObtainMuxer();

            muxer2.Should().BeSameAs(muxer1);
        }
    }
}