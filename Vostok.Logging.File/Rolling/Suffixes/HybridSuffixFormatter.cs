using System;

namespace Vostok.Logging.File.Rolling.Suffixes
{
    internal class HybridSuffixFormatter : IFileSuffixFormatter<(DateTime, int)>
    {
        private readonly IFileSuffixFormatter<DateTime> timeSuffixFormatter;
        private readonly IFileSuffixFormatter<int> sizeSuffixFormatter;
        private readonly Func<char> suffixSeparatorProvider;

        public HybridSuffixFormatter(IFileSuffixFormatter<DateTime> timeSuffixFormatter, IFileSuffixFormatter<int> sizeSuffixFormatter, Func<char> suffixSeparatorProvider)
        {
            this.timeSuffixFormatter = timeSuffixFormatter;
            this.sizeSuffixFormatter = sizeSuffixFormatter;
            this.suffixSeparatorProvider = suffixSeparatorProvider;
        }

        public string FormatSuffix((DateTime, int) part) => throw new NotSupportedException();

        public (DateTime, int)? TryParseSuffix(string suffix)
        {
            var lastSeparatorIndex = suffix.LastIndexOf(suffixSeparatorProvider());
            if (lastSeparatorIndex < 0 || lastSeparatorIndex == suffix.Length - 1)
                return null;

            var leftPart = suffix.Substring(0, lastSeparatorIndex);
            var rightPart = suffix.Substring(lastSeparatorIndex + 1);

            var date = timeSuffixFormatter.TryParseSuffix(leftPart);
            var part = sizeSuffixFormatter.TryParseSuffix(rightPart);

            return date == null || part == null ? null as (DateTime, int)? : (date.Value, part.Value);
        }
    }
}