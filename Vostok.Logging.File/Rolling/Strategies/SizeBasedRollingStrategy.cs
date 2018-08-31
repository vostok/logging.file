using System.Collections.Generic;
using System.Linq;
using Vostok.Logging.File.Rolling.SuffixFormatters;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal class SizeBasedRollingStrategy : IRollingStrategy
    {
        private readonly ISizeBasedRoller sizeBasedRoller;
        private readonly IFileNameTuner fileNameTuner;
        private readonly IFileSystem fileSystem;
        private readonly IFileSuffixFormatter<int> suffixFormatter;

        public SizeBasedRollingStrategy(IFileSystem fileSystem, IFileSuffixFormatter<int> suffixFormatter, ISizeBasedRoller sizeBasedRoller, IFileNameTuner fileNameTuner)
        {
            this.sizeBasedRoller = sizeBasedRoller;
            this.fileNameTuner = fileNameTuner;
            this.fileSystem = fileSystem;
            this.suffixFormatter = suffixFormatter;
        }

        public IEnumerable<string> DiscoverExistingFiles(string basePath) =>
            RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter, fileNameTuner).Select(file => fileNameTuner.RestoreExtension(file.path));

        public string GetCurrentFile(string basePath)
        {
            var filesWithSuffix = RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter, fileNameTuner);

            var part = 1;
            var lastFile = filesWithSuffix.LastOrDefault();
            if (lastFile.suffix != null)
            {
                part = lastFile.suffix.Value;
                if (sizeBasedRoller.ShouldRollOver(fileNameTuner.RestoreExtension(lastFile.path)))
                    part++;
            }

            return fileNameTuner.RestoreExtension(fileNameTuner.RemoveExtension(basePath) + suffixFormatter.FormatSuffix(part));
        }
    }
}