﻿using System;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Configuration
{
    internal class SafeSettingsProvider
    {
        private readonly Func<FileLogSettings> settingsProvider;

        private FileLogSettings cachedSettings;

        public SafeSettingsProvider(Func<FileLogSettings> settingsProvider)
        {
            this.settingsProvider = settingsProvider;
        }

        public FileLogSettings UnsafeGet()
        {
            var actualSettings = settingsProvider();

            if (ReferenceEquals(actualSettings, cachedSettings))
                return actualSettings;

            return cachedSettings = SettingsValidator.ValidateSettings(actualSettings);
        }

        public FileLogSettings Get()
        {
            try
            {
                return UnsafeGet();
            }
            catch (Exception exception)
            {
                if (cachedSettings == null)
                    throw;

                SafeConsole.ReportError("Failed to update file log configuration:", exception);
                return cachedSettings;
            }
        }
    }
}