using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Rolling.Helpers
{
    internal interface ISizeBasedRoller
    {
        bool ShouldRollOver(FilePath currentFilePath);
    }
}