namespace Vostok.Logging.File.Rolling.Suffixes
{
    internal class SizeBasedSuffixFormatter : IFileSuffixFormatter<int>
    {
        public string FormatSuffix(int part) => part.ToString();

        public int? TryParseSuffix(string suffix)
            => int.TryParse(suffix, out var part) && part >= 0 ? part : null as int?;
    }
}