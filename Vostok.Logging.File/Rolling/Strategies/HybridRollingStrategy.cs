using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Logging.File.Rolling.SuffixFormatters;

namespace Vostok.Logging.File.Rolling.Strategies
{
    internal class HybridRollingStrategy : IRollingStrategy
    {
        private readonly IFileSystem fileSystem;
        private readonly IRollingStrategy timeRollingStrategy;
        private readonly IRollingStrategy sizeRollingStrategy;
        private readonly IFileSuffixFormatter<(DateTime, int)> suffixFormatter;
        private readonly IFileNameTuner fileNameTuner;

        public HybridRollingStrategy(IFileSystem fileSystem, IRollingStrategy timeRollingStrategy, IRollingStrategy sizeRollingStrategy, IFileSuffixFormatter<(DateTime, int)> suffixFormatter, IFileNameTuner fileNameTuner)
        {
            this.fileSystem = fileSystem;
            this.timeRollingStrategy = timeRollingStrategy;
            this.sizeRollingStrategy = sizeRollingStrategy;
            this.suffixFormatter = suffixFormatter;
            this.fileNameTuner = fileNameTuner;
        }

        public IEnumerable<string> DiscoverExistingFiles(string basePath) => 
            RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter, fileNameTuner).Select(file => fileNameTuner.RestoreExtension(file.path));

        public string GetCurrentFile(string basePath)
        {
            var timeBasedPrefix = timeRollingStrategy.GetCurrentFile(basePath);

            return sizeRollingStrategy.GetCurrentFile(timeBasedPrefix);
        }
    }
}