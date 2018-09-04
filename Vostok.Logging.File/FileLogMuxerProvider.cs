using System;
using System.Threading;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File
{
    internal class FileLogMuxerProvider : IFileLogMuxerProvider
    {
        private readonly ISingleFileMuxerFactory singleFileMuxerFactory;
        private readonly Lazy<FileLogMuxer> muxer;

        private FileLogGlobalSettings muxerSettings = new FileLogGlobalSettings();

        public FileLogMuxerProvider(ISingleFileMuxerFactory singleFileMuxerFactory)
        {
            this.singleFileMuxerFactory = singleFileMuxerFactory;
            muxer = new Lazy<FileLogMuxer>(
                () => CreateMuxer(muxerSettings),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public void UpdateSettings(FileLogGlobalSettings newSettings) => 
            muxerSettings = SettingsValidator.ValidateGlobalSettings(newSettings);

        public IFileLogMuxer ObtainMuxer() => muxer.Value;

        private FileLogMuxer CreateMuxer(FileLogGlobalSettings settings) => 
            new FileLogMuxer(settings.EventsTemporaryBufferCapacity, singleFileMuxerFactory);
    }
}