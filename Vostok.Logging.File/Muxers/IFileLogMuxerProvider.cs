using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Muxers
{
    internal interface IFileLogMuxerProvider
    {
        void UpdateSettings(FileLogGlobalSettings newSettings);

        IFileLogMuxer ObtainMuxer();
    }
}
