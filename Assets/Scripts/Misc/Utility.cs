using System;
using System.Globalization;
using System.Linq;

public static class Utility
{
    public static string FormatValue(decimal value)
    {
        if (value >= 1000_000)
        {
            var mils = value / 1000000.0m;
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
