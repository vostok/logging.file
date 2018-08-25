namespace Vostok.Logging.File.Rolling
{
    internal class SizeBasedRollingStrategy : IRollingStrategy
    {
        private readonly string basePath;
        private readonly ISizeBasedRoller sizeBasedRoller;
        private readonly IFileSystem fileSystem;
        private readonly IFileSuffixFormatter<int> suffixFormatter;

        public SizeBasedRollingStrategy(string basePath, IFileSystem fileSystem, IFileSuffixFormatter<int> suffixFormatter, ISizeBasedRoller sizeBasedRoller)
        {
            this.basePath = basePath;
            this.sizeBasedRoller = sizeBasedRoller;
            this.fileSystem = fileSystem;
            this.suffixFormatter = suffixFormatter;
        }

        public string[] DiscoverExistingFiles() => RollingStrategyHelper.DiscoverExistingFiles(basePath, fileSystem, suffixFormatter.TryParseSuffix);

        public string GetCurrentFile() => RollingStrategyHelper.GetCurrentFileBySize(basePath, fileSystem, suffixFormatter, sizeBasedRoller);
    }
}