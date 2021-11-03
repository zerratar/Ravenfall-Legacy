using System;
using System.Globalization;
using System.Linq;
using System.Text;

public static class Utility
{
    public static string AddSpacesToSentence(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        bool lastIsUpper = char.IsUpper(text, 0);
        bool lastIsLetter = char.IsLetter(text, 0);
        StringBuilder title = new StringBuilder();
        title.Append(text[0]);
        for (int i = 1; i < text.Length; i++)
        {
            bool currIsUpper = char.IsUpper(text, i);
            bool currIsLetter = char.IsLetter(text, i);
            if (currIsUpper && !lastIsUpper && lastIsLetter)
            {
                title.Append(" ");
            }

            // if current is a number and previous is a letter we space it (ie: Rotation2D = Rotation 2D)
            if (lastIsLetter && char.IsNumber(text, i))
            {
                title.Append(" ");
            }

            // if previous is upper, current is upper and the next two following are lower then we space it (ie: UVDistortion = UV Distortion)
            if (i < text.Length - 1)
            {
                bool nextIsLower = char.IsLower(text, i + 1) && char.IsLetter(text, i + 1);
                bool lastIsLower = i < text.Length - 2 ? char.IsLower(text, i + 2) && char.IsLetter(text, i + 2) : false;
                if (lastIsUpper && currIsUpper && currIsLetter && nextIsLower && lastIsLower)
                {
                    title.Append(" ");
                }
            }
            lastIsUpper = currIsUpper;
            lastIsLetter = currIsLetter;
            title.Append(text[i]);
        }
        return title.ToString();
    }

    public static string FormatDayTime(double hours, bool extras = true)
    {
        string text;
        if (hours < 1)
        {
            var minutes = hours * 60d;
            if (minutes > 1)
            {
                var seconds = (int)((minutes - (long)minutes) * 60d);
                text = (int)minutes + "m" + (extras ? " " + seconds + "s" : "+");
            }
            else
            {
                var seconds = minutes * 60d;
                text = (int)seconds + "s";
            }
        }
        else
        {
            var minutes = (int)((hours - (long)hours) * 60d);
            var days = hours / 24d;
            var years = days / 365d;
            text = "";
            if (years >= 1)
            {
                text += (int)years + "yrs ";
                days = days % 365;
            }
            if (days >= 1)
            {
                if (years >= 1) return text;
                text += (int)days + "d ";
                hours = hours % 24;
            }
            if (hours > 0)
            {
                if (days > 1) return text;
                text += (int)hours + "h";
            }
            if (minutes > 1)
            {
                text += extras ? " " + minutes + "m" : "+";
            }
        }
        return text;
    }
    public static string FormatTime(double hours, bool extras = true)
    {
        string text;
        if (hours < 1)
        {
            var minutes = hours * 60d;
            if (minutes > 1)
            {
                var seconds = (int)((minutes - (long)minutes) * 60d);
                text = (int)minutes + "m" + (extras ? " " + seconds + "s" : "+");
            }
            else
            {
                var seconds = minutes * 60d;
                text = (int)seconds + "s";
            }
        }
        else
        {
            var minutes = (int)((hours - (long)hours) * 60d);
            text = (int)hours + "h";
            if (minutes > 1)
            {
                text += extras ? " " + minutes + "m" : "+";
            }
        }
        return text;
    }

    public static string GetDescriber(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        return new[] { 'a', 'i', 'o', 'u', 'e' }.Contains(Char.ToLower(name[0])) ? "an" : "a";
    }
    public static string FormatValue(double value)
    {
        if (value >= 1_000_000_000_000_000)
        {
            var quadrillion = value / 1_000_000_000_000_000.0d;
            return Math.Round(quadrillion, 1).ToString(CultureInfo.InvariantCulture) + "Q";
        }
        if (value >= 1_000_000_000_000)
        {
            var trillion = value / 1_000_000_000_000.0d;
            return Math.Round(trillion, 1).ToString(CultureInfo.InvariantCulture) + "T";
        }
        if (value >= 1_000_000_000)
        {
            var billion = value / 1_000_000_000.0d;
            return Math.Round(billion, 1).ToString(CultureInfo.InvariantCulture) + "B";
        }
        else if (value >= 1_000_000)
        {
            var mils = value / 1_000_000.0d;
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
