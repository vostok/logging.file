using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.Helpers;

namespace Vostok.Logging.File.Muxers
{
    internal interface IMultiFileMuxer
    {
        long EventsLost { get; }

        IMuxerRegistration Register(
            [NotNull] FilePath file,
            [NotNull] FileLogSettings settings,
            [NotNull] object initiator);

        bool TryAdd(
            [NotNull] FilePath file,
            [NotNull] LogEventInfo info,
            [NotNull] object initiator);

        Task FlushAsync([NotNull] FilePath file);

        Task FlushAsync();
    }
}