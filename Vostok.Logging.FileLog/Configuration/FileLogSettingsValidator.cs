using System;
using System.IO;
using Vostok.Configuration.Abstractions.Validation;
using Vostok.Logging.Core;

// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace Vostok.Logging.FileLog.Configuration
{
    internal class FileLogSettingsValidator : ILogSettingsValidator<FileLogSettings>, ISettingsValidator<FileLogSettings>
    {
        public SettingsValidationResult TryValidate(FileLogSettings settings)
        {
            if (settings?.Encoding == null)
                return SettingsValidationResult.EncodingIsNull();

            if (settings.ConversionPattern == null)
                return SettingsValidationResult.ConversionPatternIsNull();

            if (settings.EventsQueueCapacity <= 0)
                return SettingsValidationResult.CapacityIsLessThanZero();

            return FilePathIsValid(settings.FilePath);
        }

        public void Validate(FileLogSettings value, ISettingsValidationErrors errors)
        {
            var validationResult = TryValidate(value);
            if (!validationResult.IsSuccessful)
            {
                errors.ReportError(validationResult.ToString());
            }
        }

        private static SettingsValidationResult FilePathIsValid(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return SettingsValidationResult.FilePathIsNullOrEmpty();

            try
            {
                Path.GetFullPath(filePath);
            }
            catch (ArgumentException exception)
            {
                return SettingsValidationResult.FilePathIsNotCorrect(filePath, exception);
            }
            catch (NotSupportedException exception)
            {
                return SettingsValidationResult.FilePathIsNotCorrect(filePath, exception);
            }

            return SettingsValidationResult.Success();
        }
    }
}