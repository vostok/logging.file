using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Muxers
{
    internal interface ISingleFileMuxerFactory
    {
        ISingleFileMuxer Create(FileLogSettings settings);
    }
}