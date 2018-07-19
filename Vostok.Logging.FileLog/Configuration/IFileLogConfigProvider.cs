namespace Vostok.Logging.FileLog.Configuration
{
    internal interface IFileLogConfigProvider<out TSettings>
        where TSettings : new()
    {
        TSettings Settings { get; }
    }
}