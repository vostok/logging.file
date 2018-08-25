using System;
using System.IO;

// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace Vostok.Logging.File.Configuration
{
    internal static class SettingsValidator
    {
        public static FileLogSettings ValidateSettings(FileLogSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (settings.OutputTemplate == null)
                throw new ArgumentNullException(nameof(settings.OutputTemplate));

            if (settings.Encoding == null)
                throw new ArgumentNullException(nameof(settings.Encoding));

            if (settings.EnabledLogLevels == null)
                throw new ArgumentNullException(nameof(settings.EnabledLogLevels));

            ValidateRollingStrategy(settings);

            ValidateFilePath(settings);

            if (settings.EventsQueueCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(settings.EventsQueueCapacity));

            return settings;
        }

        public static FileLogGlobalSettings ValidateGlobalSettings(FileLogGlobalSettings settings)
        {
            if (settings.EventsTemporaryBufferCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(settings.EventsTemporaryBufferCapacity));

            return settings;
        }

        private static void ValidateRollingStrategy(FileLogSettings settings)
        {
            if (settings.RollingStrategy == null)
                throw new ArgumentNullException(nameof(settings.RollingStrategy));

            if (settings.RollingStrategy.MaxFiles < 0)
                throw new ArgumentOutOfRangeException(nameof(settings.RollingStrategy.MaxFiles));

            if ((settings.RollingStrategy.Type & RollingStrategyType.BySize) > 0 && settings.RollingStrategy.MaxSize.Bytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(settings.RollingStrategy.MaxSize));

            if ((settings.RollingStrategy.Type & RollingStrategyType.ByTime) > 0 && settings.RollingStrategy.Period <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(settings.RollingStrategy.Period));
        }

        private static void ValidateFilePath(FileLogSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.FilePath))
                throw new ArgumentNullException(nameof(settings.FilePath));

            try
            {
                Path.GetFullPath(settings.FilePath);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException)
            {
                throw new ArgumentException("File path has incorrect format.", nameof(settings.FilePath), exception);
            }
        }
    }
}