using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;

namespace Vostok.Logging.File.MuxersNew
{
    internal class SingleFileMuxerFactoryNew : ISingleFileMuxerFactoryNew
    {
        public ISingleFileMuxerNew Create(FileLogSettings settings) =>
            new SingleFileMuxerNew(new EventsWriterProviderFactory(), new SingleFileWorker(), settings);
    }
}
