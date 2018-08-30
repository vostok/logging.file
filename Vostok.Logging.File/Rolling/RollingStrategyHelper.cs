using System;
using System.Collections.Generic;
using System.Linq;

namespace Vostok.Logging.File.Rolling
{
    internal static class RollingStrategyHelper
    {
        public static string GetCurrentFileBySize(string basePath, IFileSystem fileSystem, IFileSuffixFormatter<int> sizeSuffixFormatter, ISizeBasedRoller sizeBasedRoller)
        {
            var allFiles = fileSystem.GetFilesByPrefix(basePath);

            var filesWithSuffix = allFiles.Select(path => (path, date: sizeSuffixFormatter.TryParseSuffix(path.Substring(basePath.Length))));

            var part = 1;
            var lastFile = filesWithSuffix.LastOrDefault();
            if (lastFile.date != null)
            {
                part = lastFile.date.Value;
                if (sizeBasedRoller.ShouldRollOver(lastFile.path))
                    part++;
            }

            return basePath + sizeSuffixFormatter.FormatSuffix(part);
        }

        // TODO(krait): support file extension
        public static IEnumerable<string> DiscoverExistingFiles<TSuffix>(string basePath, IFileSystem fileSystem, Func<string, TSuffix?> suffixParser)
            where TSuffix : struct
        {
            var allFiles = fileSystem.GetFilesByPrefix(basePath);

            var filesWithSuffix = allFiles.Select(path => (path, suffix: suffixParser(path.Substring(basePath.Length))));

            return filesWithSuffix.Where(file => file.suffix != null).OrderBy(file => file.suffix).Select(file => file.path);
        }
    }
}