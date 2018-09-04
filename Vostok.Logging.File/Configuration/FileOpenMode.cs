using JetBrains.Annotations;

#pragma warning disable 1591

namespace Vostok.Logging.File.Configuration
{
    [PublicAPI]
    public enum FileOpenMode
    {
        Append,
        Rewrite
    }
}