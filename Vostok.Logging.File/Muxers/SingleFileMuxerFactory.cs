using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Muxers
{
    internal class SingleFileMuxerFactory : ISingleFileMuxerFactory
    {
        private readonly IEventsWriterProviderFactory writerProviderFactory;

        public SingleFileMuxerFactory(IEventsWriterProviderFactory writerProviderFactory) =>
            this.writerProviderFactory = writerProviderFactory;

        public ISingleFileMuxer CreateMuxer(object owner, FilePath filePath, FileLogSettings settings) =>
            new SingleFileMuxer(owner, filePath, settings, writerProviderFactory);
    }
}