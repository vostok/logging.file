using System;

namespace Vostok.Logging.File.Tests
{
    internal static class ObjectExtensions
    {
        public static T2 Transform<T1, T2>(this T1 value, Func<T1, T2> transformation) => transformation(value);
    }
}