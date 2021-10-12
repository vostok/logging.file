using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Logging.File.Configuration;
using Vostok.Logging.File.EventsWriting;

namespace Vostok.Logging.File.Muxers
{
    internal class SynchronousSingleFileMuxer : ISingleFileMuxer
    {
        private readonly IEventsWriterProvider writerProvider;
        private volatile FileLogSettings settings;

        public SynchronousSingleFileMuxer(
            [NotNull] IEventsWriterProviderFactory writerProviderFactory,
            [NotNull] FileLogSettings settings)
        {
            writerProvider = writerProviderFactory.CreateProvider(settings.FilePath, () => this.settings);
        }

        public long EventsLost => 0;

        public void Dispose() => writerProvider.Dispose();

        public bool TryAdd(LogEventInfo info, bool fromOwner)
        {
            if (fromOwner)
                settings = info.Settings;

            var writer = writerProvider.ObtainWriterAsync(CancellationToken.None).GetAwaiter().GetResult();

            writer.WriteEvents(new[] {info}, 1);

            return true;
        }

        public Task FlushAsync() => Task.CompletedTask;

        public Task RefreshSettingsAsync()
        {
            writerProvider.DropCooldown();
            return Task.CompletedTask;
        }
    }
}