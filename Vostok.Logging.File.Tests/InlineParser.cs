namespace Vostok.Logging.File.Tests
{
    internal delegate bool TryParseDelegate<T>(string value, out T result);

    internal class InlineParser<T> : IInlineParser
    {
        private readonly TryParseDelegate<T> tryParse;

        public InlineParser(TryParseDelegate<T> tryParse)
        {
            this.tryParse = tryParse;
        }

        public bool TryParse(string value, out object result)
        {
            result = null;

            if (tryParse(value, out var innerResult))
            {
                result = innerResult;
                return true;
            }

            return false;
        }
    }
}