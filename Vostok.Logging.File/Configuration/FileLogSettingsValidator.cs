using System;
using System.IO;
using Vostok.Configuration.Abstractions.Validation;

// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace Vostok.Logging.File.Configuration
{
    internal class FileLogSettingsValidator : ISettingsValidator<FileLogSettings>
    {
        public void Validate(FileLogSettings settings, ISettingsValidationErrors errors)
        {
            if (settings.Encoding == null)
                errors.ReportError($"{nameof(settings.Encoding)} is not set.");

            if (settings.OutputTemplate == null)
                errors.ReportError($"{nameof(settings.OutputTemplate)} is not set.");

            if (settings.EventsQueueCapacity <= 0)
                errors.ReportError($"{nameof(settings.Encoding)} is less than or equal to zero.");

            ValidateFilePath(settings, errors);
        }

        private static void ValidateFilePath(FileLogSettings settings, ISettingsValidationErrors errors)
        {
            if (string.IsNullOrWhiteSpace(settings.FilePath))
            {
                errors.ReportError($"{nameof(settings.FilePath)} is null or empty.");
                return;
            }

            try
            {
                Path.GetFullPath(settings.FilePath);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException)
            {
                errors.ReportError($"{nameof(settings.FilePath)} has incorrect format.");
            }
        }
    }
}