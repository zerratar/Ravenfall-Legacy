using System;
using System.Globalization;
using System.Linq;

public static class Utility
{
    public static string GetDescriber(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        return new[] { 'a', 'i', 'o', 'u', 'e' }.Contains(Char.ToLower(name[0])) ? "an" : "a";
    }
    public static string FormatValue(decimal value)
    {
        if (value >= 1_000_000_000_000_000)
        {
            var quadrillion = value / 1_000_000_000_000_000.0m;
            return Math.Round(quadrillion, 1).ToString(CultureInfo.InvariantCulture) + "Q";
        }
        if (value >= 1_000_000_000_000)
        {
            var trillion = value / 1_000_000_000_000.0m;
            return Math.Round(trillion, 1).ToString(CultureInfo.InvariantCulture) + "T";
        }
        if (value >= 1_000_000_000)
        {
            var billion = value / 1_000_000_000.0m;
            return Math.Round(billion, 1).ToString(CultureInfo.InvariantCulture) + "B";
        }
        else if (value >= 1_000_000)
        {
            var mils = value / 1_000_000.0m;
            return Math.Round(mils, 1).ToString(CultureInfo.InvariantCulture) + "M";
        }
        else if (value > 1000)
        {
            var ks = value / 1000;
            return Math.Round(ks, 1).ToString(CultureInfo.InvariantCulture) + "K";
        }

        return ((long)Math.Round(value, 0)).ToString(CultureInfo.InvariantCulture);
    }

    public static T Random<T>()
        where T : struct, IConvertible
    {
        return Enum
            .GetValues(typeof(T)).Cast<T>()
            .OrderBy(x => UnityEngine.Random.value).First();
    }

    public static int Random(int min, int max)
    {
        return UnityEngine.Random.Range(min, max);
    }
}
