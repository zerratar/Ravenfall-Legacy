using System;
using System.Collections.Generic;

namespace Shinobytes.Linq
{
    public static partial class LinqExtensions
    {
        public class EnumerableSorter<TElement, TKey> : EnumerableSorter<TElement>
        {
            internal Func<TElement, TKey> keySelector;
            internal IComparer<TKey> comparer;
            internal bool descending;
            internal EnumerableSorter<TElement> next;
            internal TKey[] keys;

            public EnumerableSorter(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending, EnumerableSorter<TElement> next)
            {
                this.keySelector = keySelector;
                this.comparer = comparer;
                this.descending = descending;
                this.next = next;
            }

            public override void ComputeKeys(TElement[] elements, int count)
            {
                keys = new TKey[count];
                for (int i = 0; i < count; i++) keys[i] = keySelector(elements[i]);
                if (next != null) next.ComputeKeys(elements, count);
            }

            public override int CompareKeys(int index1, int index2)
            {
                int c = comparer.Compare(keys[index1], keys[index2]);
                if (c == 0)
                {
                    if (next == null) return index1 - index2;
                    return next.CompareKeys(index1, index2);
                }
                return descending ? -c : c;
            }
        }

    }
}