using System;
using System.Text;
using Vostok.Configuration;
using Vostok.Configuration.Abstractions;
using Vostok.Configuration.Binders;
using Vostok.Configuration.Extensions;
using Vostok.Configuration.Sources;
using Vostok.Logging.Formatting;

namespace Vostok.Logging.File.Configuration
{
    internal class FileLogConfigProvider
    {
        private const string ConfigurationTagName = "configuration";

        private readonly IConfigurationProvider configProvider;
        private readonly FileLogSettings defaultSettings = new FileLogSettings();

        public FileLogConfigProvider(string sectionName)
            : this(AppConfigFileName, sectionName)
        {
        }

        public FileLogConfigProvider(FileLogSettings settings)
        {
            configProvider = GetConfiguredConfigProvider().SetManually(settings, true);
        }

        public FileLogConfigProvider(IConfigurationSource configSource)
        {
            configProvider = GetConfiguredConfigProvider().SetupSourceFor<FileLogSettings>(configSource);
        }

        private FileLogConfigProvider(string fileName, string sectionName)
            : this(new XmlFileSource(fileName).ScopeTo(ConfigurationTagName, sectionName))
        {
        }

        public FileLogSettings Settings => TryGetSettings();

        private static string AppConfigFileName => $"{AppDomain.CurrentDomain.FriendlyName}.config";

        private FileLogSettings TryGetSettings()
        {
            try
            {
                return configProvider.Get<FileLogSettings>();
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
                .WithCustomParser<OutputTemplate>(TryParseOutputTemplate)
                .WithCustomParser<Encoding>(EncodingParser.TryParse);

            var configProviderSettings = new ConfigurationProviderSettings {Binder = binder, ErrorCallBack = ErrorCallback};
            return new ConfigurationProvider(configProviderSettings);
        }

        private static bool TryParseOutputTemplate(string value, out OutputTemplate template)
        {
            template = OutputTemplate.Parse(value);
            return true;
        }

        private static void ErrorCallback(Exception exception) =>
            SafeConsole.TryWriteLine(exception);
    }
}