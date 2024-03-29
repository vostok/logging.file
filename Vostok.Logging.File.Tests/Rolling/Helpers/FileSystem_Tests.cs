﻿using System.IO;
using System.Runtime.InteropServices;
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
        [Platform("Win", Reason = "Doesn't work as expected on Unix due to file locking mechanism.")]
        public void TryOpenFile_should_use_shared_handles_if_specified()
        {
            var settings = new FileLogSettings
            {
                FileShare = FileShare.ReadWrite
            };

            using (var folder = new TemporaryFolder())
            {
                var file = folder.GetFileName("log");
                using (var writer = fileSystem.TryOpenFile(file, settings))
                {
                    writer.WriteLine("test");
                    writer.Flush();

                    using (var reader = new StreamReader(new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)))
                        reader.ReadLine().Should().Be("test");
                }
            }
        }

        [Test]
        [Platform("Win", Reason = "Doesn't work as expected on Unix due to file locking mechanism.")]
        public void TryOpenFile_should_return_null_if_locked()
        {
            var settings = new FileLogSettings
            {
                UseSeparateFileOnConflict = false
            };

            using (var folder = new TemporaryFolder())
            {
                var file = folder.GetFileName("log");
                using (var writer1 = fileSystem.TryOpenFile(file, settings))
                using (var writer2 = fileSystem.TryOpenFile(file, settings))
                {
                    writer1.Should().NotBeNull();
                    writer2.Should().BeNull();
                }
            }
        }

        [Test]
        [Platform("Win", Reason = "Doesn't work as expected on Unix due to file locking mechanism.")]
        public void TryOpenFile_should_append_suffix_if_locked()
        {
            var settings = new FileLogSettings
            {
                UseSeparateFileOnConflict = true
            };

            using (var folder = new TemporaryFolder())
            {
                var file = folder.GetFileName("log");
                using (var writer1 = fileSystem.TryOpenFile(file, settings))
                using (var writer2 = fileSystem.TryOpenFile(file, settings))
                {
                    writer1.WriteLine("test1");
                    writer2.WriteLine("test2");
                }

                System.IO.File.ReadAllLines(file).Should().BeEquivalentTo("test1");
                System.IO.File.ReadAllLines(file + "-1").Should().BeEquivalentTo("test2");
            }
        }

        [Test]
        public void TryOpenFile_should_return_null_if_file_cannot_be_opened()
        {
            using (var folder = new TemporaryFolder())
                fileSystem.TryOpenFile(folder.Name, new FileLogSettings {UseSeparateFileOnConflict = false}).Should().BeNull();
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
        [Platform("Win", Reason = "Doesn't work as expected on Unix due to file locking mechanism.")]
        public void TryRemoveFile_should_never_throw_exceptions()
        {
            using (var folder = new TemporaryFolder())
            {
                var file = folder.GetFileName("log");
                using (System.IO.File.Create(file))
                    fileSystem.TryRemoveFile(file).Should().BeFalse();
            }
        }
    }
}