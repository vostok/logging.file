namespace Vostok.Logging.FileLog.Tests
{
    internal interface IInlineParser
    {
        bool TryParse(string value, out object result);
    }
}