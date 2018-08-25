namespace Vostok.Logging.File.Rolling
{
    internal class DisabledRollingStrategy : IRollingStrategy
    {
        private readonly string basePath;
        private readonly IFileSystem fileSystem;

        public DisabledRollingStrategy(string basePath, IFileSystem fileSystem)
        {
            this.basePath = basePath;
            this.fileSystem = fileSystem;
        }

        public string[] DiscoverExistingFiles() => fileSystem.GetFilesByPrefix(basePath);

        public string GetCurrentFile() => basePath;
    }
}