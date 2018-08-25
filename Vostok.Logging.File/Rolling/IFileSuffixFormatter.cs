namespace Vostok.Logging.File.Rolling
{
    internal interface IFileSuffixFormatter<TSuffix> where TSuffix : struct
    {
        string FormatSuffix(TSuffix suffix);

        TSuffix? TryParseSuffix(string suffix);
    }
}