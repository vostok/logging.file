namespace Vostok.Logging.File.Rolling.Strategies
{
    internal interface IRollingStrategyProvider
    {
        IRollingStrategy ObtainStrategy();
    }
}
