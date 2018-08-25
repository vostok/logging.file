using System;
using System.Globalization;

namespace Vostok.Logging.File.Rolling
{
    internal class TimeBasedSuffixFormatter : IFileSuffixFormatter<DateTime>
    {
        private readonly Func<TimeSpan> rollingPeriodProvider;

        public TimeBasedSuffixFormatter(Func<TimeSpan> rollingPeriodProvider) => 
            this.rollingPeriodProvider = rollingPeriodProvider;

        public string FormatSuffix(DateTime now)
        {
            var rollingPeriod = rollingPeriodProvider();

            if (rollingPeriod.Ticks % TimeSpan.TicksPerDay == 0)
                return now.ToString("yyyy.MM.dd");

            return now.ToString("yyyy.MM.dd.HH.mm.ss");
        }

        public DateTime? TryParseSuffix(string suffix)
        {
            if (DateTime.TryParseExact(suffix, new[] {"yyyy.MM.dd", "yyyy.MM.dd.HH.mm.ss"}, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                return time;

            return default;
        }
    }
}