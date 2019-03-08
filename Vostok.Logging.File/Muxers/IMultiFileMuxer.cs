using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Muxers
{
    internal interface IMultiFileMuxer
    {
        IMuxerRegistration Register(
            [NotNull] FilePath file,
            [NotNull] FileLogSettings settings,
            [NotNull] WeakReference initiator);

        bool TryAdd(
            [NotNull] FilePath file,
            [NotNull] LogEventInfo info,
            [NotNull] WeakReference initiator);

        Task FlushAsync([NotNull] FilePath file);
    }
}