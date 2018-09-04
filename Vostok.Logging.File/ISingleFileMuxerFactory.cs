using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File
{
    internal interface ISingleFileMuxerFactory
    {
        ISingleFileMuxer CreateMuxer(object owner, FilePath filePath, FileLogSettings settings);
    }
}