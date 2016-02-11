using System.Collections.Generic;
using System.Linq;

namespace WorkerHost.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> list, int partsSize)
        {
            return list.Select((item, index) => new { index, item })
                       .GroupBy(x => x.index / partsSize)
                       .Select(x => x.Select(y => y.item));
        }
    }
}
