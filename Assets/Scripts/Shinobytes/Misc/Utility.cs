using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

public static class Utility
{
    private readonly static char[] DescriberCharacters = new[] { 'a', 'i', 'o', 'u', 'e' };
    private readonly static string[] ExpValuePostfix = new string[] { " ", "k", "M", "G", "T", "P", "E", "Z", "Y", "R", "Q" };
    private readonly static string[] AmountPostFix = new string[] { "", "K", "M", "B", "T", "Q" };
    public static string ReplaceLastOccurrence(string source, string find, string replace)
    {
        int place = source.LastIndexOf(find);

        if (place == -1)
            return source;

        return source.Remove(place, find.Length).Insert(place, replace);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsVocal(char c)
    {
        c = char.ToLower(c);
        return c == 'a' || c == 'i' || c == 'e' || c == 'u' || c == 'o';
    }
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

    public static string FormatTime(TimeSpan time)
    {
        string text;
        if (time.TotalHours < 1)
        {
            if (time.TotalMinutes > 1)
            {
                text = time.Minutes + "m " + time.Seconds + "s";
            }
            else
            {
                text = time.Seconds + "s";
            }
        }
        else
        {
            text = time.Hours + "h";
            if (time.Minutes > 1)
            {
                text += " " + time.Minutes + "m";
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
        return DescriberCharacters.Contains(Char.ToLower(name[0])) ? "an" : "a";
    }

    public static string FormatLongAmount(long amount)
    {
        var val = string.Format("{0:N0}", amount).Replace(" ", ",").Replace(' ', ',');

        return val; // still returns spaces?? oh well..
    }

    public static string FormatAmount(double value)
    {
        return FormatValue(value, AmountPostFix, "");
    }

    public static string FormatExp(double value)
    {
        return FormatValue(value, AmountPostFix, "");//ExpValuePostfix);
    }

    public static string FormatValue(double value, string[] postfix, string secondary = "Q")
    {
        var thousands = 0;
        while (value > 1000)
        {
            value = (value / 1000);
            thousands++;
        }

        if (thousands == 0)
        {
            return ((long)Math.Round(value, 1)).ToString(CultureInfo.InvariantCulture);
        }
        var pLen = postfix.Length - 1;
        var p0 = ((thousands - 1) % pLen) + 1;
        var q = thousands >= pLen ? secondary : "";
        return Math.Round(value, 1).ToString(CultureInfo.InvariantCulture) + postfix[0] + postfix[p0] + q;
    }

    public static string FormatValue(long num)
    {
        var str = num.ToString();
        if (str.Length <= 3) return str;
        for (var i = str.Length - 3; i >= 0; i -= 3)
            str = str.Insert(i, " ");
        return str;
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
