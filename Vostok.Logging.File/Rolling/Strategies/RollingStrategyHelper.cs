using System.Collections.Generic;
using System.Linq;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal static class RollingStrategyHelper
    {
        /// <summary>
        /// Returns files in order oldest to newest (ordered by parsed suffix).
        /// </summary>
        public static IEnumerable<(FilePath path, TSuffix? suffix)> DiscoverExistingFiles<TSuffix>(FilePath basePath, IFileSystem fileSystem, IFileSuffixFormatter<TSuffix> suffixFormatter)
            where TSuffix : struct
        {
            var allFiles = fileSystem.GetFilesByPrefix(basePath);

            var filesWithSuffix = allFiles.Select(path => (path, suffix: suffixFormatter.TryParseSuffix(path.PathWithoutExtension.Substring(basePath.PathWithoutExtension.Length))));

            return filesWithSuffix.Where(file => file.suffix != null).OrderBy(file => file.suffix);
        }
    }
}