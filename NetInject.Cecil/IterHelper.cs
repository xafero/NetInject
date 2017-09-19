using System.Collections.Generic;

namespace NetInject.Cecil
{
    internal static class IterHelper
    {
        public static IEnumerable<T> Yield<T>(T[,] array)
        {
            for (var x = 0; x < array.GetLength(0); x++)
            for (var y = 0; y < array.GetLength(1); y++)
                yield return array[x, y];
        }
    }
}