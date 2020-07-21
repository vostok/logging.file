using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal static class RollingStrategyHelper
    {
        public const string SuffixPlaceholder = "{RollingSuffix}";

        private static readonly char[] SuffixDashEliminators = {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '.', '-'};

        public static FilePath AddSuffix(FilePath basePath, string suffix, bool keepPlaceholder, char suffixEliminator = '-')
        {
            var placeholderIndex = FindPlaceholderIndex(basePath.NormalizedPath);
            var symbolBeforeInsert = placeholderIndex > 0 ? basePath.NormalizedPath[placeholderIndex - 1] : basePath.PathWithoutExtension.Last();
            if (placeholderIndex != 0 && !SuffixDashEliminators.Contains(symbolBeforeInsert) && symbolBeforeInsert != suffixEliminator)
                suffix = suffixEliminator + suffix;

            if (placeholderIndex < 0)
                return basePath + suffix;

            return new FilePath(basePath.NormalizedPath.Replace(SuffixPlaceholder, keepPlaceholder ? suffix + SuffixPlaceholder : suffix));
        }

        /// <summary>
        /// Returns files in order oldest to newest (ordered by parsed suffix).
        /// </summary>
        public static IEnumerable<(FilePath path, TSuffix? suffix)> DiscoverExistingFiles<TSuffix>(FilePath basePath, IFileSystem fileSystem, IFileSuffixFormatter<TSuffix> suffixFormatter, char suffixEliminator = '-')
            where TSuffix : struct
        {
            var allFiles = fileSystem.GetFilesByPrefix(GetPrefixForDiscovery(basePath));

            var filesWithSuffix = allFiles.Select(path => (path, suffix: suffixFormatter.TryParseSuffix(ExtractSuffixValue(path, basePath, suffixEliminator))));

            return filesWithSuffix.Where(file => file.suffix != null).OrderBy(file => file.suffix);
        }

        private static FilePath GetPrefixForDiscovery(FilePath basePath)
        {
            var placeholderIndex = FindPlaceholderIndex(basePath.NormalizedPath);
            if (placeholderIndex < 0)
                return basePath;

            return new FilePath(basePath.NormalizedPath.Substring(0, placeholderIndex));
        }

        private static string ExtractSuffixValue(FilePath path, FilePath basePath, char suffixEliminator = '-')
        {
            var placeholderIndex = FindPlaceholderIndex(basePath.PathWithoutExtension);
            if (placeholderIndex < 0)
                return path.PathWithoutExtension.Substring(basePath.PathWithoutExtension.Length).TrimStart(suffixEliminator);

            var basePathTrailerLength = basePath.PathWithoutExtension.Length - placeholderIndex - SuffixPlaceholder.Length;
            var pathSuffixLength = path.PathWithoutExtension.Length - placeholderIndex - basePathTrailerLength;
            if (pathSuffixLength > 0 && placeholderIndex + pathSuffixLength <= path.PathWithoutExtension.Length)
                return path.PathWithoutExtension.Substring(placeholderIndex, pathSuffixLength).TrimStart(suffixEliminator);

            return string.Empty;
        }

        private static int FindPlaceholderIndex(string path)
            => path.IndexOf(SuffixPlaceholder, StringComparison.Ordinal);
    }
}
