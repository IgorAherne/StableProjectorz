using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Bigtree.Algorithm.Extensions
{
    public static partial class IListExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            var rng = new Random();
            var count = list.Count;
            while (count > 1)
            {
                count--;
                var k = rng.Next(count + 1);
                var value = list[k];
                list[k] = list[count];
                list[count] = value;
            }
        }
    }
}
