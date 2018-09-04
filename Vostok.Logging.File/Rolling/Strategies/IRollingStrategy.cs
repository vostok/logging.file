using System.Collections.Generic;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal interface IRollingStrategy
    {
        /// <summary>
        /// Returned files must be sorted oldest to newest.
        /// </summary>
        IEnumerable<string> DiscoverExistingFiles(string basePath);

        string GetCurrentFile(string basePath);
    }
}