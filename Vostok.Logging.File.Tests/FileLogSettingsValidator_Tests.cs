using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Configuration.Abstractions.Validation;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Tests
{
    [TestFixture]
    internal class FileLogSettingsValidator_Tests
    {
        [Test]
        public void Default_FileLogSettings_should_be_valid()
        {
            validator.Validate(settings, errors);

            errorMessages.Should().BeEmpty();
        }

        [Test]
        public void FileLogSettings_with_null_ConversionPattern_should_not_be_valid()
        {
            settings.OutputTemplate = null;

            errorMessages.Single().Should().Contain(nameof(settings.OutputTemplate));
        }

        [Test]
        public void FileLogSettings_with_null_Encoding_should_not_be_valid()
        {
            settings.Encoding = null;

            errorMessages.Single().Should().Contain(nameof(settings.Encoding));
        }

        [Test]
        public void FileLogSettings_with_null_FilePath_should_not_be_valid()
        {
            settings.FilePath = null;

            errorMessages.Single().Should().Contain(nameof(settings.FilePath));
        }

        [Test]
        public void FileLogSettings_with_not_correct_FilePath_should_not_be_valid()
        {
            settings.FilePath = "asdasf:asfdggasg?sadagfasdf";

            errorMessages.Single().Should().Contain(nameof(settings.FilePath));
        }

        [Test]
        public void FileLogSettings_with_absent_FilePath_should_be_valid()
        {
            settings.FilePath = "C:\\HelloWorld\\Hello";

            errorMessages.Should().BeEmpty();
        }

        [Test]
        public void FileLogSettingsValidator_should_detect_multiple_errors()
        {
            settings.OutputTemplate = null;
            settings.Encoding = null;

            errorMessages.Should().HaveCount(2);
        }

        [SetUp]
        public void SetUp()
        {
            settings = new FileLogSettings();

            var logDirectoryPath = settings.FilePath;
            if (!Directory.Exists(Path.GetDirectoryName(logDirectoryPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(logDirectoryPath));

            validator = new FileLogSettingsValidator();
            errorMessages = new List<string>();
            errors = Substitute.For<ISettingsValidationErrors>();
            errors.ReportError(Arg.Do<string>(s => errorMessages.Add(s)));
        }

        private FileLogSettings settings;
        private FileLogSettingsValidator validator;
        private ISettingsValidationErrors errors;
        private List<string> errorMessages;
    }
}