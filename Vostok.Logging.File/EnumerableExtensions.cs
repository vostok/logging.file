using System.Collections.Generic;

namespace Vostok.Logging.File
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> enumerable, T newItem)
        {
            foreach (var item in enumerable)
                yield return item;
            yield return newItem;
        }
    }
}