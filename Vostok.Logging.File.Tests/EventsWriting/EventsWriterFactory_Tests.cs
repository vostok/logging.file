using System.IO;
using System.Text;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Tests.EventsWriting
{
    [TestFixture]
    internal class EventsWriterFactory_Tests
    {
        private IFileSystem fileSystem;
        private EventsWriterFactory factory;

        [SetUp]
        public void TestSetup()
        {
            fileSystem = Substitute.For<IFileSystem>();
            fileSystem.TryOpenFile(Arg.Any<FilePath>(), Arg.Any<FileLogSettings>()).Returns(null as TextWriter);

            factory = new EventsWriterFactory(fileSystem);
        }

        [Test]
        public void TryCreateWriter_should_return_null_when_file_system_fails_to_open_a_file()
        {
            factory.TryCreateWriter("log", new FileLogSettings()).Should().BeNull();
        }
    }
}