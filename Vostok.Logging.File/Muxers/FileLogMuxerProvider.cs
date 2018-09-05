using System;
using System.Threading;
using Vostok.Logging.File.Configuration;

namespace Vostok.Logging.File.Muxers
{
    internal class FileLogMuxerProvider : IFileLogMuxerProvider
    {
        private readonly ISingleFileMuxerFactory singleFileMuxerFactory;
        private readonly Lazy<FileLogMuxer> muxer;

        public FileLogMuxerProvider(ISingleFileMuxerFactory singleFileMuxerFactory)
        {
            this.singleFileMuxerFactory = singleFileMuxerFactory;
            muxer = new Lazy<FileLogMuxer>(
                () => CreateMuxer(),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public IFileLogMuxer ObtainMuxer() => muxer.Value;

        // TODO(iloktionov): this is a band-aid compilation fix.
        private FileLogMuxer CreateMuxer() =>
            new FileLogMuxer(new FileLogSettings().EventsBufferCapacity, singleFileMuxerFactory);
    }
}