using System;
using JetBrains.Annotations;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.MuxersNew
{
    internal interface IMuxerRegistration : IDisposable
    {
        [NotNull] FilePath File { get; }
    }
}