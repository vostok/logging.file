using System;
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

        public FileLogSettings Get()
        {
            try
            {
                return cachedSettings = settingsProvider();
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