using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Shinobytes.Linq
{
    public static partial class LinqExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TSource> Concat<TSource>(this List<TSource> source, IEnumerable<TSource> items)
        {
            var res = new List<TSource>();
            res.AddRange(source);
            res.AddRange(items);
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyList<TSource> Concat<TSource>(this IReadOnlyList<TSource> source, IEnumerable<TSource> items)
        {
            var res = new List<TSource>();
            res.AddRange(source);
            res.AddRange(items);
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T MinBy<T>(this IReadOnlyList<T> source, Func<T, IComparable> selector)
        {
            if (source.Count == 0) return default(T);
            var min = source[0];
            var minValue = selector(min);
            for (var i = 1; i < source.Count; i++)
            {
                var item = source[i];
                var value = selector(item);
                if (value.CompareTo(minValue) < 0)
                {
                    min = item;
                    minValue = value;
                }
            }
            return min;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSource Highest<TSource, TCompare>(this IEnumerable<TSource> source, Func<TSource, TCompare> comp) where TCompare : IComparable
        {
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) return default(TSource);//throw Error.NoElements();
                TSource highest = e.Current;
                while (e.MoveNext())
                {
                    var a = comp(highest);
                    var b = comp(e.Current);
                    if (b.CompareTo(a) > 0)
                        highest = e.Current;
                }
                return highest;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSource Lowest<TSource, TCompare>(this IEnumerable<TSource> source, Func<TSource, TCompare> comp) where TCompare : IComparable
        {
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) return default(TSource);//throw Error.NoElements();
                TSource lowest = e.Current;
                while (e.MoveNext())
                {
                    var a = comp(lowest);
                    var b = comp(e.Current);
                    if (b.CompareTo(a) < 0)
                        lowest = e.Current;
                }
                return lowest;
            }
        }
        #region aggregate
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TSource Aggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> aggr)
        {
            //var result = default(T);
            //foreach (var i in src)
            //{
            //    result = aggr(result, i);
            //}
            //return result;

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext()) return default(TSource);//throw Error.NoElements();
                TSource result = e.Current;
                while (e.MoveNext()) result = aggr(result, e.Current);
                return result;
            }

        }
        #endregion

        #region distinct

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParse<T>(string value, out T v) where T : System.Enum
        {
            v = default(T);
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> src)
        {
            if (src is HashSet<T> set) return set;
            return new HashSet<T>(src);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> DistinctBy<T, T2>(this IEnumerable<T> src, Func<T, T2> predicate)
        {
            var set = new HashSet<T2>();
            foreach (var i in src)
            {
                var item = predicate(i);
                if (set.Add(item)) yield return i;
            }
        }


        #endregion

        #region sum

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sum(this IEnumerable<int> src)
        {
            int sum = 0;
            foreach (var i in src) checked { sum += i; }
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sum(this IEnumerable<int> src, Func<int, int> sumOf)
        {
            int sum = 0;
            foreach (var i in src) checked { sum += sumOf(i); }
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sum<T>(this IEnumerable<T> src, Func<T, int?> sumOf)
        {
            int sum = 0;
            foreach (var i in src) checked { sum += sumOf(i) ?? 0; }
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Sum<T>(this IEnumerable<T> src, Func<T, long?> sumOf)
        {
            long sum = 0;
            foreach (var i in src) checked { sum += sumOf(i) ?? 0; }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Sum<T>(this IEnumerable<T> src, Func<T, double?> sumOf)
        {
            double sum = 0;
            foreach (var i in src) checked { sum += sumOf(i) ?? 0; }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Sum<T>(this IEnumerable<T> src, Func<T, short?> sumOf)
        {
            short sum = 0;
            foreach (var i in src) checked { sum += sumOf(i) ?? 0; }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Sum<T>(this IEnumerable<T> src, Func<T, byte?> sumOf)
        {
            byte sum = 0;
            foreach (var i in src) checked { sum += sumOf(i) ?? 0; }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sum<T>(this IEnumerable<T> src, Func<T, int> sumOf)
        {
            int sum = 0;
            foreach (var i in src) checked { sum += sumOf(i); }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Sum<T>(this IEnumerable<T> src, Func<T, uint> sumOf)
        {
            uint sum = 0;
            foreach (var i in src) checked { sum += sumOf(i); }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Sum<T>(this IEnumerable<T> src, Func<T, ulong> sumOf)
        {
            ulong sum = 0;
            foreach (var i in src) checked { sum += sumOf(i); }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Sum<T>(this IEnumerable<T> src, Func<T, long> sumOf)
        {
            long sum = 0;
            foreach (var i in src) checked { sum += sumOf(i); }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Sum<T>(this IEnumerable<T> src, Func<T, double> sumOf)
        {
            double sum = 0;
            foreach (var i in src) checked { sum += sumOf(i); }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sum<T>(this IEnumerable<T> src, Func<T, float> sumOf)
        {
            float sum = 0;
            foreach (var i in src) checked { sum += sumOf(i); }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal Sum<T>(this IEnumerable<T> src, Func<T, decimal> sumOf)
        {
            decimal sum = 0;
            foreach (var i in src) checked { sum += sumOf(i); }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Sum<T>(this IEnumerable<T> src, Func<T, short> sumOf)
        {
            short sum = 0;
            foreach (var i in src) checked { sum += sumOf(i); }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Sum<T>(this IEnumerable<T> src, Func<T, ushort> sumOf)
        {
            ushort sum = 0;
            foreach (var i in src) checked { sum += sumOf(i); }
            return sum;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte Sum<T>(this IEnumerable<T> src, Func<T, sbyte> sumOf)
        {
            sbyte sum = 0;
            foreach (var i in src) checked { sum += sumOf(i); }
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Sum<T>(this IEnumerable<T> src, Func<T, byte> sumOf)
        {
            byte sum = 0;
            foreach (var i in src) checked { sum += sumOf(i); }
            return sum;
        }

        #endregion

        #region min max
        public static T2 Max<T, T2>(this IEnumerable<T> src, Func<T, T2> predicate) where T2 : IComparable
        {
            var items = src.AsList();
            if (items.Count == 0) return default(T2);
            if (items.Count == 1) return predicate(items[0]);
            T2 max = predicate(items[0]);
            for (var i = 1; i < items.Count; ++i)
            {
                var a = predicate(items[i]);
                if (a.CompareTo(max) > 0)
                {
                    max = a;
                }
            }
            return max;
        }

        public static T2 Min<T, T2>(this IEnumerable<T> src, Func<T, T2> predicate) where T2 : IComparable
        {
            var items = src.AsList();
            if (items.Count == 0) return default(T2);
            if (items.Count == 1) return predicate(items[0]);
            T2 min = predicate(items[0]);
            for (var i = 1; i < items.Count; ++i)
            {
                var a = predicate(items[i]);
                if (a.CompareTo(min) < 0)
                {
                    min = a;
                }
            }
            return min;
        }
        #endregion
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Count<T>(this IEnumerable<T> src, Func<T, bool> predicate = null)
        {
            if (src == null) return 0;
            if (predicate == null)
            {
                if (src is ICollection<T> collection) return collection.Count;
                if (src is T[] array) return array.Length;
                if (src is IReadOnlyCollection<T> roCollection) return roCollection.Count;
                if (src is IReadOnlyList<T> roList) return roList.Count;
            }

            var c = 0;
            foreach (var i in src) if (predicate == null || predicate(i)) ++c;
            return c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool All<T>(this IEnumerable<T> src, Func<T, bool> predicate)
        {
            foreach (var i in src)
            {
                if (!predicate(i)) return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ElementAt<T>(this IEnumerable<T> src, int index)
        {
            var j = 0;
            foreach (var i in src)
            {
                if (j++ == index)
                {
                    return i;
                }
            }
            return default(T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T First<T>(this IEnumerable<T> src)
        {
            var enumerator = src.GetEnumerator();
            return enumerator.Current;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static bool Contains<T>(this IEnumerable<T> src, T item)
        //{
        //    if (item == null) return false;
        //    foreach (var i in src)
        //    {
        //        if (i == item) return true;
        //    }
        //    return false;
        //}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(this IEnumerable<T> src, T item)
        {
            //if (item.Equals(default(T))) return false;
            foreach (var i in src)
            {
                if (object.Equals(i, item) || object.ReferenceEquals(i, item) || i.Equals(item))
                    return true;
            }

            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> WhereNotNull<T>(this List<T> src)
        {
            var items = new List<T>();
            foreach (var i in src) if (i != null) items.Add(i);
            return items;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> src)
        {
            var items = new List<T>();
            foreach (var i in src) if (i != null) items.Add(i);
            return items;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<TResult> SelectMany<TSource, TResult>(this IEnumerable<TSource> src, Func<TSource, IEnumerable<TResult>> predicate)
        {
            foreach (var a in src)
            {
                foreach (var b in predicate(a))
                {
                    yield return b;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, List<TValue>> GroupBy<TKey, TValue>(
            this IEnumerable<TValue> src,
            Func<TValue, TKey> keySelector)
        {
            var result = new Dictionary<TKey, List<TValue>>();
            foreach (var item in src)
            {
                var key = keySelector(item);
                if (!result.TryGetValue(key, out var list))
                {
                    result[key] = list = new List<TValue>();
                }
                list.Add(item);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> ToDictionary<TSource, TKey, TValue>(
                this IEnumerable<TSource> src,
                Func<TSource, TKey> keySelector,
                Func<TSource, TValue> valueSelector)
        {
            var result = new Dictionary<TKey, TValue>();
            foreach (var item in src)
            {
                var key = keySelector(item);
                var value = valueSelector(item);
                result[key] = value;
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
                this IEnumerable<TValue> src,
                Func<TValue, TKey> keySelector)
        {
            var result = new Dictionary<TKey, TValue>();
            foreach (var item in src)
            {
                var key = keySelector(item);
                var value = item;
                result[key] = value;
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any<T>(this IEnumerable<T> src, Func<T, bool> predicate)
        {
            foreach (var i in src)
            {
                if (predicate(i)) return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any<T>(this IEnumerable<T> src)
        {
            return src.AsList().Count > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Last<T>(this IEnumerable<T> src)
        {
            var collection = src.AsList();
            if (collection.Count == 0) return default(T);
            return collection[collection.Count - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
        {
            foreach (object obj in source)
            {
                if (obj is TResult res) yield return res;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<TResult> WhereOfType<TSource, TResult>(this IEnumerable<TSource> source, Func<TResult, bool> predicate)
        {
            foreach (object obj in source)
            {
                if (obj is TResult res && predicate(res))
                    yield return res;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<TResult> WhereOfType<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TResult> select,
            Func<TResult, bool> predicate)
        {
            foreach (var obj in source)
            {
                var a = select(obj);
                if (a != null && predicate(a)) yield return a;
            }
        }

        /// <summary>
        /// Gets a list of the enumeration with the least allocation possible.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> AsList<T>(this IEnumerable<T> items)
        {
            if (items is List<T> list) return list;
            return items.ToList();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> AsList<T>(this IEnumerable<T> items, Func<T, bool> predicateWhere)
        {
            var result = new List<T>();
            foreach (var item in items)
            {
                if (predicateWhere(item))
                {
                    result.Add(item);
                }
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T2> SelectWhere<T, T2>(this IEnumerable<T> items, Func<T, bool> predicateWhere, Func<T, T2> select)
        {
            var result = new List<T2>();
            foreach (var item in items)
            {
                if (predicateWhere(item))
                {
                    result.Add(select(item));
                }
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Skip<T>(this T[] src, int skip)
        {
            var result = new T[src.Length - skip];
            System.Array.Copy(src, skip, result, 0, result.Length);
            //for (int i = skip; i < src.Length; ++i)
            //{
            //    result[i - skip] = src[i];
            //}
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Skip<T>(this ICollection<T> src, int skip)
        {
            var result = new T[src.Count - skip];
            var index = 0;
            foreach (var item in src)
            {
                if (index >= skip)
                {
                    result[index - skip] = item;
                }
                index++;
            }

            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> Slice<T>(this T[] src, int skip, int take)
        {
            var result = new List<T>();
            for (int i = skip, j = 0; i < src.Length && j < take; ++i, ++j)
            {
                result.Add(src[i]);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> Slice<T>(this IReadOnlyList<T> src, int skip, int take)
        {
            var result = new List<T>();
            for (int i = skip, j = 0; i < src.Count && j < take; ++i, ++j)
            {
                result.Add(src[i]);
            }
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> Slice<T>(this IEnumerable<T> src, int skip, int take)
        {
            var result = new List<T>();
            var j = 0;
            foreach (var item in src)
            {
                if (result.Count >= take)
                {
                    break;
                }
                if (skip <= j)
                {
                    result.Add(item);
                }
                ++j;
            }
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> SliceAs<T, T2>(this IReadOnlyList<T2> src, int skip, int take, Func<T2, T> select)
        {
            var result = new List<T>();
            for (int i = skip, j = 0; i < src.Count && j < take; ++i, ++j)
            {
                result.Add(select(src[i]));
            }
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] SelectAsArray<T, T2>(this IReadOnlyList<T2> src, Func<T2, T> select)
        {
            var result = new T[src.Count];
            for (var i = 0; i < src.Count; ++i)
            {
                result[i] = select(src[i]);
            }
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyList<T> SelectAsReadOnly<T, T2>(this IReadOnlyList<T2> src, Func<T2, T> select)
        {
            var result = new List<T>(src.Count);
            for (var i = 0; i < src.Count; ++i)
            {
                result.Add(select(src[i]));
            }
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> items)
        {
            if (items is T[] array) return array;
            if (items is IReadOnlyList<T> list) return list;
            return items.AsList();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ForEach<T>(this IEnumerable<T> items, Action<T> forEach)
        {
            foreach (var item in items)
            {
                forEach(item);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> Select<TSrc, T>(this IEnumerable<TSrc> src, Func<TSrc, T> select)
        {
            foreach (var i in src)
            {
                yield return select(i);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> Select<TSrc, T>(this IEnumerable<TSrc> src, Func<TSrc, int, T> select)
        {
            var index = 0;
            foreach (var i in src)
            {
                yield return select(i, index++);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> Where<T>(this IEnumerable<T> src, Func<T, bool> res)
        {
            foreach (var i in src)
            {
                if (res(i)) yield return i;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable IsNot<TType>(this IEnumerable src)
        {
            foreach (var i in src)
            {
                if (!(i is TType)) yield return i;
            }
        }

        /// <summary>
        /// Makes a copy of the current list with elements that is not of provided type.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <typeparam name="TType"></typeparam>
        /// <param name="src"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TResult> IsNot<TResult, TType>(this List<TResult> src)
        {
            var newList = new List<TResult>();

            for (int j = 0; j < src.Count; j++)
            {
                TResult i = src[j];
                if (!(i is TType))
                {
                    newList.Add(i);
                }
            }
            return newList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<TResult> IsNot<TResult, TType>(this TResult[] src)
        {
            for (int j = 0; j < src.Length; j++)
            {
                TResult i = src[j];
                if (!(i is TType))
                {
                    yield return i;
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FirstOrDefault<T>(this T[] src, Func<T, bool> res = null)
        {
            if (src == null || src.Length == 0)
                return default(T);

            if (res == null)
            {
                return src[0];
            }

            for (var i = 0; i < src.Length; i++)
            {
                var item = src[i];
                if (res == null || res(item)) return item;
            }

            return default(T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FirstOrDefault<T>(this IReadOnlyList<T> src, Func<T, bool> res = null)
        {
            if (src == null || src.Count == 0)
                return default(T);

            if (res == null)
            {
                return src[0];
            }

            for (var i = 0; i < src.Count; i++)
            {
                var item = src[i];
                if (res == null || res(item)) return item;
            }

            return default(T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FirstOrDefault<T>(this IEnumerable<T> src, Func<T, bool> res = null)
        {
            foreach (var i in src)
            {
                if (res == null || res(i)) return i;
            }

            return default(T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOut[] SelectArray<TIn, TOut>(this IReadOnlyList<TIn> collection, Func<TIn, TOut> select)
        {
            var result = new TOut[collection.Count];
            for (var i = 0; i < result.Length; ++i)
            {
                result[i] = select(collection[i]);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this ICollection<T> collection)
        {
            if (collection is T[] arr) return arr;
            var items = new T[collection.Count];
            collection.CopyTo(items, 0);
            return items;
        }

        /// <summary>
        /// EEEW! We need to enumerate the list twice and then make a copy. Don't use IEnumerable!!!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(this IEnumerable<T> collection)
        {
            var c = collection.Count();
            var res = new T[c];
            var i = 0;
            foreach (var item in collection)
            {
                res[i++] = item;
            }
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> ToList<T>(this IEnumerable<T> src)
        {
            if (src is List<T> list) return list;
            if (src is IReadOnlyList<T> readOnlyList) return new List<T>(readOnlyList);
            if (src is ICollection<T> collection) return new List<T>(collection);
            if (src is T[] array) return new List<T>(array);
            if (src is ISet<T> set) return new List<T>(set);

            var res = new List<T>();
            foreach (var i in src)
            {
                res.Add(i);
            }

            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<TSource> OrderBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return new OrderedEnumerable<TSource, TKey>(source, keySelector, null, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<TSource> OrderByDescending<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            return new OrderedEnumerable<TSource, TKey>(source, keySelector, null, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<KeyValuePair<TKey, TValue>> ToKeyValuePair<TKey, TValue>(this IEnumerable<TValue> values, Func<TValue, TKey> keySelector, Func<TValue, TValue> valueSelector)
        {
            return values.Select(x => new KeyValuePair<TKey, TValue>(keySelector(x), valueSelector(x)));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyList<double> Delta(this IList<double> newValue, IReadOnlyList<double> oldValue)
        {
            if (oldValue == null)
            {
                return new List<double>(newValue.Count);
            }
            if (newValue.Count != oldValue.Count)
            {
                return new List<double>(newValue.Count);
            }

            return newValue.Select((x, i) => x - oldValue[i]).ToList();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> Except<T>(this IEnumerable<T> items, T except)
        {
            return items.Where(x => !x.Equals(except));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<T> Except<T>(this IEnumerable<T> items, IEnumerable<T> except)
        {
            foreach (var a in items)
            {
                foreach (var b in except)
                {
                    if (!a.Equals(b))
                        yield return a;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> TakeRandom<T>(this IReadOnlyList<T> items, int count)
        {
            if (items == null || items.Count == 0)
            {
                return default;
            }
            var result = new List<T>();
            while (result.Count < count)
            {
                result.Add(items[UnityEngine.Random.Range(0, items.Count)]);
            }

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Random<T>(this T[] items)
        {
            if (items == null || items.Length == 0)
            {
                return default;
            }

            return items[UnityEngine.Random.Range(0, items.Length)];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T PickRandom<T>(params T[] items)
        {
            if (items == null || items.Length == 0)
            {
                return default;
            }

            return items[UnityEngine.Random.Range(0, items.Length)];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Random<T>(this IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
            {
                return default;
            }

            return items[UnityEngine.Random.Range(0, items.Count)];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Random<T>(this IEnumerable<T> items)
        {
            var selections = items?.AsList();
            if (selections == null || selections.Count == 0)
            {
                return default;
            }

            return selections[UnityEngine.Random.Range(0, selections.Count)];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Weighted<T>(this IReadOnlyList<T> items, Func<T, double> weight)
        {
            var selections = items;
            var totalWeight = selections.Sum(weight);
            var randomWeight = UnityEngine.Random.value * totalWeight;
            var weightSum = 0d;

            for (int i = 0; i < selections.Count; i++)
            {
                weightSum += weight(selections[i]);
                if (randomWeight < weightSum)
                {
                    return selections[i];
                }
            }

            return selections[UnityEngine.Random.Range(0, selections.Count)];
        }

        public abstract class OrderedEnumerable<TElement> : IEnumerable<TElement>
        {
            internal IEnumerable<TElement> source;
            public IEnumerator<TElement> GetEnumerator()
            {
                Buffer<TElement> buffer = new Buffer<TElement>(source);
                if (buffer.count > 0)
                {
                    EnumerableSorter<TElement> sorter = GetEnumerableSorter(null);
                    int[] map = sorter.Sort(buffer.items, buffer.count);
                    sorter = null;
                    for (int i = 0; i < buffer.count; i++) yield return buffer.items[map[i]];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public abstract EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement> next);
        }

        public class OrderedEnumerable<TElement, TKey> : OrderedEnumerable<TElement>
        {
            internal OrderedEnumerable<TElement> parent;
            internal Func<TElement, TKey> keySelector;
            internal IComparer<TKey> comparer;
            internal bool descending;

            public OrderedEnumerable(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
            {
                this.source = source;
                this.parent = null;
                this.keySelector = keySelector;
                this.comparer = comparer != null ? comparer : Comparer<TKey>.Default;
                this.descending = descending;
            }

            public override EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement> next)
            {
                EnumerableSorter<TElement> sorter = new EnumerableSorter<TElement, TKey>(keySelector, comparer, descending, next);
                if (parent != null) sorter = parent.GetEnumerableSorter(sorter);
                return sorter;
            }
        }

    }
}