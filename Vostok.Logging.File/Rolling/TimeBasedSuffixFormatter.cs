using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Rolling
{
    internal class TimeBasedSuffixFormatter : IFileSuffixFormatter<DateTime>
    {
        private readonly Func<RollingPeriod> rollingPeriodProvider;

        public TimeBasedSuffixFormatter(Func<RollingPeriod> rollingPeriodProvider) => 
            this.rollingPeriodProvider = rollingPeriodProvider;

        public string FormatSuffix(DateTime now)
        {
            var rollingPeriod = rollingPeriodProvider();

            if (!formatsByPeriod.TryGetValue(rollingPeriod, out var format))
                format = formatsByPeriod[RollingPeriod.Day];

            return now.ToString(format);
        }

        public DateTime? TryParseSuffix(string suffix)
        {
            if (DateTime.TryParseExact(suffix, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                return time;

            return default;
        }

        private static readonly Dictionary<RollingPeriod, string> formatsByPeriod = new Dictionary<RollingPeriod, string>
        {
            { RollingPeriod.Day, "yyyy.MM.dd" },
            { RollingPeriod.Hour, "yyyy.MM.dd.HH" },
            { RollingPeriod.Minute, "yyyy.MM.dd.HH.mm" },
            { RollingPeriod.Second, "yyyy.MM.dd.HH.mm.ss" }
        };

        private static readonly string[] formats = formatsByPeriod.Values.ToArray();
    }
}