using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Tests.Rolling.Helpers
{
    [TestFixture]
    internal class FileSystem_Tests
    {
        private FileSystem fileSystem;

        [SetUp]
        public void TestSetup()
        {
            fileSystem = new FileSystem();
        }

        [Test]
        public void OpenFile_should_use_shared_handles()
        {
            using (var folder = new TemporaryFolder())
            {
                var file = folder.GetFileName("log");
                using (var writer = fileSystem.OpenFile(file, FileOpenMode.Append, Encoding.UTF8, 4096))
                {
                    writer.WriteLine("test");
                    writer.Flush();

                    using (var reader = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)))
                        reader.ReadLine().Should().Be("test");
                }
            }
        }

        [Test]
        public void OpenFile_should_return_null_if_file_cannot_be_opened()
        {
            using (var folder = new TemporaryFolder())
                fileSystem.OpenFile(folder.Name, FileOpenMode.Append, Encoding.UTF8, 4096).Should().BeNull();
        }

        [Test]
        public void GetFileSize_should_return_zero_if_file_does_not_exist()
        {
            fileSystem.GetFileSize("xx").Should().Be(0);
        }

        [Test]
        public void GetFileSize_should_return_zero_if_file_is_inaccessible()
        {
            using (var folder = new TemporaryFolder())
                fileSystem.GetFileSize(folder.Name).Should().Be(0);
        }

        [Test]
        public void TryRemoveFile_should_never_throw_exceptions()
        {
            using (var folder = new TemporaryFolder())
            {
                var file = folder.GetFileName("log");
                using (System.IO.File.Create(file))
                    fileSystem.TryRemoveFile(file).Should().Be(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            }
        }
    }
}