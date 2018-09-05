using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.MuxersNew
{
    internal interface ISingleFileMuxerFactoryNew
    {
        ISingleFileMuxerNew Create(FileLogSettings settings);
    }
}