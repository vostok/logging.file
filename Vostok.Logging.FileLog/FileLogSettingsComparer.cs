using System.Collections.Generic;
using Vostok.Logging.FileLog.Configuration;

namespace Vostok.Logging.FileLog
{
    internal class FileLogSettingsComparer : IEqualityComparer<FileLogSettings>
    {
        public bool Equals(FileLogSettings x, FileLogSettings y)
        {
            return x?.FilePath == y?.FilePath &&
                   Equals(x?.RollingStrategy, y?.RollingStrategy) &&
                   Equals(x?.Encoding, y?.Encoding) &&
                   x?.FileOpenMode == y?.FileOpenMode &&
                   x?.EventsQueueCapacity == y?.EventsQueueCapacity;
        }

        public int GetHashCode(FileLogSettings obj)
        {
            unchecked
            {
                var hashCode = obj.FilePath?.GetHashCode() ?? 0;
                hashCode = (hashCode * 397) ^ (int)obj.FileOpenMode;
                hashCode = (hashCode * 397) ^ (obj.RollingStrategy?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (obj.Encoding?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ obj.EventsQueueCapacity;
                return hashCode;
            }
        }
    }
}