using System.Collections.Generic;
using System.Linq;

namespace System
{
    static public class __Enumerable
    {
        public static T[] Array<T>(this IEnumerable<T> enumerable)
        {
            var _array = enumerable as T[];
            return _array != null ? _array : enumerable.ToArray();
        }
    }
}