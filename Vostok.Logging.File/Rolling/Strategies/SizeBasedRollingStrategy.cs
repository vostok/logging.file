using System.Collections.Generic;
using System.Linq;
using Vostok.Logging.File.Helpers;
using Vostok.Logging.File.Rolling.Helpers;
using Vostok.Logging.File.Rolling.Suffixes;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal class SizeBasedRollingStrategy : IRollingStrategy
    {
        private readonly ISizeBasedRoller sizeBasedRoller;
        private readonly IFileSystem fileSystem;
        private readonly IFileSuffixFormatter<int> suffixFormatter;

        public SizeBasedRollingStrategy(IFileSystem fileSystem, IFileSuffixFormatter<int> suffixFormatter, ISizeBasedRoller sizeBasedRoller)
        {
            this.sizeBasedRoller = sizeBasedRoller;
            this.fileSystem = fileSystem;
            this.suffixFormatter = suffixFormatter;
        }

        public IEnumerable<FilePath> DiscoverExistingFiles(FilePath basePath) =>
            RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter).Select(file => file.path);

        public FilePath GetCurrentFile(FilePath basePath)
        {
            var filesWithSuffix = RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter);

            var part = 1;
            var lastFile = filesWithSuffix.LastOrDefault();
            if (lastFile.suffix != null)
            {
                part = lastFile.suffix.Value;
                if (sizeBasedRoller.ShouldRollOver(lastFile.path))
                    part++;
            }

            return RollingStrategyHelper.AddSuffix(basePath, suffixFormatter.FormatSuffix(part), false);
        }
    }
}