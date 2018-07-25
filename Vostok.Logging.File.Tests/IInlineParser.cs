namespace Vostok.Logging.File.Tests
{
    internal interface IInlineParser
    {
        bool TryParse(string value, out object result);
    }
}