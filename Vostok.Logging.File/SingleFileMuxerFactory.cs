using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;

namespace Vostok.Logging.File
{
    internal class SingleFileMuxerFactory : ISingleFileMuxerFactory
    {
        private readonly IEventsWriterProviderFactory writerProviderFactory;

        public SingleFileMuxerFactory(IEventsWriterProviderFactory writerProviderFactory)
        {
            this.writerProviderFactory = writerProviderFactory;
        }

        public ISingleFileMuxer CreateMuxer(object owner, FilePath filePath, FileLogSettings settings)
        {
            return new SingleFileMuxer(owner, filePath, settings, writerProviderFactory);
        }
    }
}