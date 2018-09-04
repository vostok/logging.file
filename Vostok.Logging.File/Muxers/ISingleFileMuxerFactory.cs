using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Muxers
{
    internal interface ISingleFileMuxerFactory
    {
        ISingleFileMuxer CreateMuxer(object owner, FilePath filePath, FileLogSettings settings);
    }
}
