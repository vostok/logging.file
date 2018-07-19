using System;
using System.Text;
using Vostok.Configuration;
using Vostok.Configuration.Abstractions;
using Vostok.Configuration.Binders;
using Vostok.Configuration.Extensions;
using Vostok.Configuration.Sources;
using Vostok.Logging.Core;
using Vostok.Logging.Core.Parsing;
using Console = Vostok.Logging.Core.Console;

namespace Vostok.Logging.FileLog.Configuration
{
    internal class FileLogConfigProvider<TSettings> : IFileLogConfigProvider<TSettings>
        where TSettings : new()
    {
        private const string configurationTagName = "configuration";

        private readonly IConfigurationProvider configProvider;
        private readonly TSettings defaultSettings = new TSettings();

        public FileLogConfigProvider(string fileName, string sectionName)
            : this(new XmlFileSource(fileName).ScopeTo(configurationTagName, sectionName))
        {
        }

        public FileLogConfigProvider(string sectionName)
            : this(AppConfigFileName, sectionName)
        {
        }

        public FileLogConfigProvider(TSettings settings)
        {
            configProvider = GetConfiguredConfigProvider().SetManually(settings, true);
        }

        private FileLogConfigProvider(IConfigurationSource settingsSource)
        {
            configProvider = GetConfiguredConfigProvider().SetupSourceFor<TSettings>(settingsSource);
        }

        public TSettings Settings => TryGetSettings();

        private static string AppConfigFileName => $"{AppDomain.CurrentDomain.FriendlyName}.config";

        private TSettings TryGetSettings()
        {
            try
            {
                return configProvider.Get<TSettings>();
            }
            catch (Exception exception)
            {
                ErrorCallback(exception);
                return defaultSettings;
            }
        }

        private static ConfigurationProvider GetConfiguredConfigProvider()
        {
            var binder = new DefaultSettingsBinder()
                .WithCustomParser<ConversionPattern>(ConversionPattern.TryParse)
                .WithCustomParser<Encoding>(EncodingParser.TryParse);

            var configProviderSettings = new ConfigurationProviderSettings {Binder = binder, ErrorCallBack = ErrorCallback};
            return new ConfigurationProvider(configProviderSettings);
        }

        private static void ErrorCallback(Exception exception)
        {
            Console.TryWriteLine(exception);
        }
    }
}