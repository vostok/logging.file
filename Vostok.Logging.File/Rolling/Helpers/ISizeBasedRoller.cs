namespace Vostok.Logging.File.Rolling.Helpers
{
    internal interface ISizeBasedRoller
    {
        bool ShouldRollOver(string currentFilePath);
    }
}
