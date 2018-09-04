using System.Collections.Generic;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal interface IRollingStrategy
    {
        /// <summary>
        /// Returned files must be sorted oldest to newest.
        /// </summary>
        IEnumerable<FilePath> DiscoverExistingFiles(FilePath basePath);

        FilePath GetCurrentFile(FilePath basePath);
    }
}