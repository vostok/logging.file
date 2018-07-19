using System.Text;
using Vostok.Configuration.Abstractions;
using Vostok.Logging.Core;
using Vostok.Logging.FileLog.Configuration;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Vostok.Logging.FileLog
{
    [ValidateBy(typeof(FileLogSettingsValidator))]
    public class FileLogSettings
    {
        public string FilePath { get; set; } = "logs\\log$d";
        public ConversionPattern ConversionPattern { get; set; } = ConversionPattern.Default;
        public bool AppendToFile { get; set; } = true;
        public bool EnableRolling { get; set; } = true;
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public int EventsQueueCapacity { get; set; } = 10000;
    }
}