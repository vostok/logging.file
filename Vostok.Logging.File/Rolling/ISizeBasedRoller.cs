namespace Vostok.Logging.File.Rolling
{
    internal interface ISizeBasedRoller
    {
        bool ShouldRollOver(string currentFilePath);
    }
}