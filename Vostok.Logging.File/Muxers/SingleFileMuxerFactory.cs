using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;

namespace Vostok.Logging.File.Muxers
{
    internal class SingleFileMuxerFactory : ISingleFileMuxerFactory
    {
        public ISingleFileMuxer Create(FileLogSettings settings) =>
            settings.WriteSynchronously
                ? (ISingleFileMuxer)new SynchronousSingleFileMuxer(new EventsWriterProviderFactory(), settings)
                : new SingleFileMuxer(new EventsWriterProviderFactory(), new SingleFileWorker(), settings);
    }
}