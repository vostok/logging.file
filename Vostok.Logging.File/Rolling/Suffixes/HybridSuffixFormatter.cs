using System;

namespace Vostok.Logging.File.Rolling.Suffixes
{
    internal class HybridSuffixFormatter : IFileSuffixFormatter<(DateTime, int)>
    {
        private readonly IFileSuffixFormatter<DateTime> timeSuffixFormatter;
        private readonly IFileSuffixFormatter<int> sizeSuffixFormatter;

        public HybridSuffixFormatter(IFileSuffixFormatter<DateTime> timeSuffixFormatter, IFileSuffixFormatter<int> sizeSuffixFormatter)
        {
            this.timeSuffixFormatter = timeSuffixFormatter;
            this.sizeSuffixFormatter = sizeSuffixFormatter;
        }

        public string FormatSuffix((DateTime, int) part) => throw new NotSupportedException();

        public (DateTime, int)? TryParseSuffix(string suffix)
        {
            var lastDashIndex = suffix.LastIndexOf('-');
            if (lastDashIndex < 0 || lastDashIndex == suffix.Length - 1)
                return null;

            var leftPart = suffix.Substring(0, lastDashIndex);
            var rightPart = suffix.Substring(lastDashIndex + 1);

            var date = timeSuffixFormatter.TryParseSuffix(leftPart);
            var part = sizeSuffixFormatter.TryParseSuffix(rightPart);

            return date == null || part == null ? null as (DateTime, int)? : (date.Value, part.Value);
        }
    }
}