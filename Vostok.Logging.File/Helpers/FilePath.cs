using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Vostok.Logging.File.Helpers
{
    internal class FilePath : IEquatable<FilePath>
    {
        private static readonly IEqualityComparer<string> PathComparer = ChoosePathComparer();

        private readonly int hash;

        public FilePath(string rawPath)
        {
            (PathWithoutExtension, Extension) = SeparateExtension(NormalizedPath = NormalizePath(rawPath));

            hash = PathComparer.GetHashCode(NormalizedPath);
        }

        private FilePath(string pathWithoutExtension, string extension)
        {
            NormalizedPath = (PathWithoutExtension = pathWithoutExtension) + (Extension = extension);

            hash = PathComparer.GetHashCode(NormalizedPath);
        }

        public string NormalizedPath { get; }

        public string PathWithoutExtension { get; }

        public string Extension { get; }

        public override string ToString() => NormalizedPath;

        public static FilePath operator+(FilePath current, string suffix) =>
            current == null ? new FilePath(suffix) : new FilePath(current.PathWithoutExtension + suffix, current.Extension);

        public static implicit operator FilePath(string rawPath) => new FilePath(rawPath);

        private static (string pathWithoutExtension, string extension) SeparateExtension(string path)
        {
            var extension = Path.GetExtension(path);

            if (string.IsNullOrEmpty(extension))
                return (path, "");

            return (path.Substring(0, path.Length - extension.Length), extension);
        }

        private static string NormalizePath(string path)
            => Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));

        #region Equality

        public static bool operator==(FilePath current, FilePath other) => Equals(current, other);

        public static bool operator!=(FilePath current, FilePath other) => !Equals(current, other);

        public bool Equals(FilePath other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return PathComparer.Equals(NormalizedPath, other.NormalizedPath);
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

        private static IEqualityComparer<string> ChoosePathComparer() =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        #endregion
    }
}