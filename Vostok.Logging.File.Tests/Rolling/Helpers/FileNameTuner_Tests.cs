using System.Runtime.InteropServices;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.File.Rolling;
using Vostok.Logging.File.Rolling.Helpers;

namespace Vostok.Logging.File.Tests.Rolling.Helpers
{
    [TestFixture]
    internal class FileNameTuner_Tests
    {
        private FileNameTuner tuner;

        [SetUp]
        public void TestSetup()
        {
            tuner = new FileNameTuner(@"logs\log.txt");
        }

        [Test]
        public void RemoveExtension_should_remove_extension_from_base_path()
        {
            tuner.RemoveExtension(@"logs\log.txt").Should().Be(@"logs\log");
        }

        [Test]
        public void RemoveExtension_should_be_case_insensitive_on_windows()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                tuner.RemoveExtension(@"logs\log.TxT").Should().Be(@"logs\log");
            else
                tuner.RemoveExtension(@"logs\log.TxT").Should().Be(@"logs\log.TxT");
        }

        [Test]
        public void RemoveExtension_should_remove_extension_from_other_path()
        {
            tuner.RemoveExtension(@"xxx\yy.txt").Should().Be(@"xxx\yy");
        }

        [Test]
        public void RemoveExtension_should_remove_only_last_extension()
        {
            tuner.RemoveExtension(@"xxx\yy.txt.txt").Should().Be(@"xxx\yy.txt");
        }

        [Test]
        public void RemoveExtension_should_remove_only_preconfigured_extension()
        {
            tuner.RemoveExtension(@"logs\log.exe").Should().Be(@"logs\log.exe");
        }

        [TestCase(@"logs\log")]
        [TestCase(@"logs\log.")]
        [TestCase(@"logs\log.txt")]
        public void RemoveExtension_should_do_nothing_if_preconfigured_path_has_no_extension(string basePath)
        {
            tuner = new FileNameTuner(@"logs\log");

            tuner.RemoveExtension(basePath).Should().Be(basePath);
        }

        [Test]
        public void RestoreExtension_should_restore_extension_to_base_path()
        {
            tuner.RestoreExtension(@"logs\log").Should().Be(@"logs\log.txt");
        }

        [Test]
        public void RestoreExtension_should_restore_extension_to_other_path()
        {
            tuner.RestoreExtension(@"xxx\yy").Should().Be(@"xxx\yy.txt");
        }

        [Test]
        public void RestoreExtension_should_restore_extension_regardless_of_existing_extension()
        {
            tuner.RestoreExtension(@"logs\log.txt").Should().Be(@"logs\log.txt.txt");
        }

        [TestCase(@"logs\log")]
        [TestCase(@"logs\log.")]
        [TestCase(@"logs\log.txt")]
        public void RestoreExtension_should_do_nothing_if_preconfigured_path_has_no_extension(string basePath)
        {
            tuner = new FileNameTuner(@"logs\log");

            tuner.RemoveExtension(basePath).Should().Be(basePath);
        }
    }
}