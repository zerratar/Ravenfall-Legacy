using System;
using System.Collections.Generic;

namespace Shinobytes.Linq
{
    public static class Enums
    {
        public static IEnumerable<T> GetValues<T>() where T : System.Enum
        {
            foreach (T item in Enum.GetValues(typeof(T)))
            {
                yield return item;
            }
        }
    }
}