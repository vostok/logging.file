using System;
using System.IO;
using System.Runtime.InteropServices;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Tests.Helpers
{
    [TestFixture]
    internal class FilePath_Tests
    {
        [Test]
        public void Should_compare_paths_using_OS_specific_comparer()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                new FilePath("log").Equals(new FilePath("log")).Should().BeTrue();
                new FilePath("log").Equals(new FilePath("LOG")).Should().BeTrue();
                new FilePath("xxx").Equals(new FilePath("log")).Should().BeFalse();
            }
            else
            {
                new FilePath("log").Equals(new FilePath("log")).Should().BeTrue();
                new FilePath("log").Equals(new FilePath("LOG")).Should().BeFalse();
                new FilePath("xxx").Equals(new FilePath("log")).Should().BeFalse();
            }
        }

        [Test]
        public void Should_compute_hash_codes_using_OS_specific_comparer()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                new FilePath("log").GetHashCode().Equals(new FilePath("log").GetHashCode()).Should().BeTrue();
                new FilePath("log").GetHashCode().Equals(new FilePath("LOG").GetHashCode()).Should().BeTrue();
                new FilePath("xxx").GetHashCode().Equals(new FilePath("log").GetHashCode()).Should().BeFalse();
            }
            else
            {
                new FilePath("log").GetHashCode().Equals(new FilePath("log").GetHashCode()).Should().BeTrue();
                new FilePath("log").GetHashCode().Equals(new FilePath("LOG").GetHashCode()).Should().BeFalse();
                new FilePath("xxx").GetHashCode().Equals(new FilePath("log").GetHashCode()).Should().BeFalse();
            }
        }

        [Test]
        public void Should_normalize_paths()
        {
            new FilePath("log").Equals(new FilePath("logs/../log")).Should().BeTrue();
        }

        [Test]
        public void PathWithoutExtension_should_contain_path_without_extension()
        {
            new FilePath("logs/log.txt").PathWithoutExtension.Should().EndWith("log");
        }

        [Test]
        public void Extension_should_contain_extension()
        {
            new FilePath("logs/log.txt").Extension.Should().Be(".txt");
        }

        [Test]
        public void Plus_operator_should_add_suffix_to_path_without_extension()
        {
            var newPath = new FilePath("logs/log.txt") + "_xx";

            newPath.NormalizedPath.Should().EndWith("log_xx.txt");
            newPath.PathWithoutExtension.Should().EndWith("log_xx");
            newPath.Extension.Should().Be(".txt");
        }

        [Test]
        public void Extension_should_only_contain_last_extension()
        {
            var path = new FilePath("xxx/yy.txt.txt");

            path.Extension.Should().Be(".txt");
            path.PathWithoutExtension.Should().EndWith("yy.txt");
        }

        [TestCase("logs/log")]
        [TestCase("logs/log.")]
        public void Extension_should_be_empty_string_if_there_is_no_extension(string basePath)
        {
            new FilePath(basePath).Extension.Should().BeEmpty();
        }

        [Test]
        public void NormalizedPath_should_preserve_absolute_paths()
        {
            var path = typeof(FilePath).Assembly.Location;

            new FilePath(path).NormalizedPath.Should().Be(path);
        }

        [Test]
        public void NormalizePath_should_expand_relative_paths_from_appdomain_base_directory()
        {
            var path = "logs/log";

            var expectedPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path));

            new FilePath(path).NormalizedPath.Should().Be(expectedPath);
        }
    }
}