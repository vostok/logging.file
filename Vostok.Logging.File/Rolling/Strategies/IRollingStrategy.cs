using System.Collections.Generic;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal interface IRollingStrategy
    {
        IEnumerable<string> DiscoverExistingFiles(string basePath);

        string GetCurrentFile(string basePath);
    }
}