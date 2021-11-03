using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class IEnumerableExtensions
{
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

    public static IEnumerable<T> Except<T>(this IEnumerable<T> items, T except)
    {
        return items.Where(x => !x.Equals(except));
    }

    public static int RandomIndex<T>(this IEnumerable<T> items)
    {
        return UnityEngine.Random.Range(0, items.Count());
    }
    public static T Random<T>(this IEnumerable<T> items)
    {
        var selections = items.ToList();
        return selections[UnityEngine.Random.Range(0, selections.Count)];
    }

    public static IReadOnlyList<T> Randomize<T>(this IEnumerable<T> items)
    {
        return items.OrderBy(x => UnityEngine.Random.value).ToList();
    }
    public static T Weighted<T, T2>(this IEnumerable<T> items, Func<T, T2> weight)
       where T2 : struct
    {
        var selections = items.ToArray();

        foreach (var sel in selections)
        {
            var ran = UnityEngine.Random.value;
            var w = weight(sel);

            if (w is float f && ran <= f)
            {
                return sel;
            }

            if (w is int i && ran <= i)
            {
                return sel;
            }

            if (w is short s && ran <= s)
            {
                return sel;
            }

            if (w is decimal de && ran <= (double)de)
            {
                return sel;
            }

            if (w is byte b && ran <= b)
            {
                return sel;
            }

            if (w is double d && ran <= d)
            {
                return sel;
            }
        }

        return selections[UnityEngine.Random.Range(0, selections.Length)];
    }
    public static T RandomizedWeighted<T, T2>(this IEnumerable<T> items, Func<T, T2> weight) 
        where T2 : struct
    {
        var selections = items.Randomize();

        foreach (var sel in selections)
        {
            var ran = UnityEngine.Random.value;
            var w = weight(sel);

            if (w is float f && ran <= f)
            {
                return sel;
            }

            if (w is int i && ran <= i)
            {
                return sel;
            }

            if (w is short s && ran <= s)
            {
                return sel;
            }

            if (w is decimal de && ran <= (double)de)
            {
                return sel;
            }

            if (w is byte b && ran <= b)
            {
                return sel;
            }

            if (w is double d && ran <= d)
            {
                return sel;
            }
        }

        return selections[UnityEngine.Random.Range(0, selections.Count)];
    }
}