using System.Collections.Generic;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal class ExtensionStrippingStrategy : IRollingStrategy
    {
        private readonly IRollingStrategy strategy;

        public ExtensionStrippingStrategy(IRollingStrategy strategy)
        {
            this.strategy = strategy;
        }

        public IEnumerable<string> DiscoverExistingFiles(string basePath)
        {
            throw new System.NotImplementedException();
        }

        public string GetCurrentFile(string basePath)
        {
            throw new System.NotImplementedException();
        }
    }
}