namespace Vostok.Logging.File.Muxers
{
    internal interface IFileLogMuxerProvider
    {
        IFileLogMuxer ObtainMuxer();
    }
}