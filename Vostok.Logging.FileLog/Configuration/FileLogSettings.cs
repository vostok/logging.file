using System.Text;
using Vostok.Configuration.Abstractions;
using Vostok.Logging.Core.ConversionPattern;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Vostok.Logging.FileLog.Configuration
{
    [ValidateBy(typeof(FileLogSettingsValidator))]
    public class FileLogSettings
    {
        public string FilePath { get; set; } = "logs\\log$d";
        public ConversionPattern ConversionPattern { get; set; } = null; // TODO(krait):  ConversionPattern.Default;
        public bool AppendToFile { get; set; } = true;
        public bool EnableRolling { get; set; } = true;
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public int EventsQueueCapacity { get; set; } = 10000;
    }
}