using System.Runtime.InteropServices;
using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Logging.File.Tests
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
        public void Should_normalize_paths()
        {
            new FilePath("log").Equals(new FilePath("logs/../log")).Should().BeTrue();
        }

        [Test]
        public void GetHashCode_should_return_hash_of_lowercase_normalized_path()
        {
            new FilePath("log").GetHashCode().Should().Be(new FilePath("log").NormalizedPath.ToLowerInvariant().GetHashCode());
        }
    }
}