using System.Collections.Generic;
using System.Linq;

namespace MsSqlCloneDb.Lib.Common
{
    public static class EnumerableExtensions
    {
        public static IReadOnlyCollection<TItem> ToReadOnlyCollection<TItem>(this IEnumerable<TItem> enumerable)
        {
            return enumerable.ToList();
        }

        public static IReadOnlyList<TItem> ToReadOnlyList<TItem>(this IEnumerable<TItem> enumerable)
        {
            return enumerable.ToList();
        }
    }
}