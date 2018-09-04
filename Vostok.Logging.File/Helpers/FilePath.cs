using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Vostok.Logging.File.Helpers
{
    internal class FilePath : IEquatable<FilePath>
    {
        public static readonly StringComparison PathComparison = ChoosePathComparison();

        private readonly int hash;

        public FilePath(string rawPath)
        {
            NormalizedPath = Path.GetFullPath(rawPath);
            hash = NormalizedPath.ToLowerInvariant().GetHashCode();
        }

        public string NormalizedPath { get; }

        public bool Equals(FilePath other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(NormalizedPath, other.NormalizedPath, PathComparison);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj is FilePath other && Equals(other);
        }

        public override int GetHashCode() => hash;

        private static StringComparison ChoosePathComparison() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    }
}