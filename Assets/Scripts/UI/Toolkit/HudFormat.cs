using System.Globalization;

namespace Shinobytes.UI
{
    /// <summary>
    /// Number and time formatting shared by the HUD, so every window writes values the same way.
    /// </summary>
    /// <remarks>
    /// Always invariant culture. The game runs on machines set to Swedish and other locales, where
    /// the default formatting would print decimal commas and space separated thousands in the middle
    /// of English text.
    /// </remarks>
    public static class HudFormat
    {
        private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Q" };

        /// <summary>
        /// Three significant figures with a magnitude suffix: 735, 2.94K, 62.1K, 842K, 1.76M.
        /// </summary>
        /// <remarks>
        /// Three figures is the point where a number stops being something you read and becomes
        /// something you glance at, which is what an overlay number has to be. Utility.FormatValue
        /// rounds to one decimal instead, so 1,761,656 becomes 1.8M and two very different rates can
        /// print the same.
        /// </remarks>
        public static string Compact(double value)
        {
            var negative = value < 0;
            if (negative) value = -value;

            var tier = 0;
            while (value >= 1000d && tier < Suffixes.Length - 1)
            {
                value /= 1000d;
                tier++;
            }

            // Rounding 999.6K up gives 1000K; move it into the next tier instead.
            if (tier > 0 && tier < Suffixes.Length - 1 && System.Math.Round(value, value < 10 ? 2 : value < 100 ? 1 : 0) >= 1000d)
            {
                value /= 1000d;
                tier++;
            }

            string text;
            if (tier == 0)
            {
                text = ((long)System.Math.Round(value)).ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                var format = value < 10 ? "0.##" : value < 100 ? "0.#" : "0";
                text = value.ToString(format, CultureInfo.InvariantCulture) + Suffixes[tier];
            }

            return negative ? "-" + text : text;
        }

        /// <summary>Thousands separated with commas: 7,560,000.</summary>
        public static string Grouped(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>A percentage with one decimal: 54.9%.</summary>
        public static string Percent(float fraction)
        {
            return (fraction * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>Minutes and seconds, as a clock: 2:48.</summary>
        public static string Clock(int seconds)
        {
            if (seconds < 0) seconds = 0;
            return (seconds / 60).ToString(CultureInfo.InvariantCulture) + ":"
                + (seconds % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// A duration in its two largest units: 8h 41m, 14m 05s, 42s.
        /// </summary>
        public static string Duration(int seconds)
        {
            if (seconds < 0) seconds = 0;
            if (seconds >= 3600)
            {
                return string.Format(CultureInfo.InvariantCulture, Localization.UI_TIME_HOURS,
                    seconds / 3600, (seconds / 60 % 60).ToString("00", CultureInfo.InvariantCulture));
            }

            if (seconds >= 60)
            {
                return string.Format(CultureInfo.InvariantCulture, Localization.UI_TIME_MINUTES,
                    seconds / 60, (seconds % 60).ToString("00", CultureInfo.InvariantCulture));
            }

            return string.Format(CultureInfo.InvariantCulture, Localization.UI_TIME_SECONDS, seconds);
        }

        /// <summary>
        /// How long until the next level, in the largest units that still say something: "4h 54m",
        /// "3d 4h", "6 weeks". Beyond a year it gives up on numbers, as the old panel did.
        /// </summary>
        /// <returns>The text, and through <paramref name="tone"/> how worrying it is: 0 normal,
        /// 1 slow, 2 hopeless.</returns>
        public static string TimeToLevel(System.TimeSpan left, out int tone)
        {
            tone = 0;
            if (left.TotalSeconds <= 0)
            {
                tone = 2;
                return Localization.UI_TIME_UNKNOWN;
            }

            var days = left.TotalDays;
            if (days >= 365d * 10000d) { tone = 2; return Localization.UI_TIME_NEVER; }
            if (days >= 365d * 1000d) { tone = 2; return Localization.UI_TIME_UNREASONABLE; }
            if (days >= 365d) { tone = 2; return Localization.UI_TIME_TOO_LONG; }
            if (days > 21d) { tone = 1; return string.Format(CultureInfo.InvariantCulture, Localization.UI_TIME_WEEKS, (int)(days / 7d)); }
            if (left.Days > 0) { tone = 1; return string.Format(CultureInfo.InvariantCulture, Localization.UI_TIME_DAYS, left.Days, left.Hours); }

            return Duration((int)left.TotalSeconds);
        }

        /// <summary>Mirrors --rf-text, used when a player has no valid colour of their own.</summary>
        private static readonly UnityEngine.Color DefaultNameColor = new UnityEngine.Color(1f, 244f / 255f, 214f / 255f);

        /// <summary>
        /// Twitch lets people pick any name colour, and plenty pick dark blues and purples that
        /// vanish against a dark panel once the stream encoder has had its way. Anything darker than
        /// this relative luminance is lifted toward white until it reaches it, which keeps the hue
        /// recognisable while making it readable.
        /// </summary>
        private const float MinNameLuminance = 0.45f;

        /// <summary>
        /// Turns a Twitch name colour into one that reads on the dark panels.
        /// </summary>
        public static UnityEngine.Color ReadableNameColor(string hex)
        {
            if (string.IsNullOrEmpty(hex) || !UnityEngine.ColorUtility.TryParseHtmlString(hex, out var color))
            {
                return DefaultNameColor;
            }

            var luminance = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
            if (luminance >= MinNameLuminance)
            {
                return color;
            }

            // Lerping toward white by this amount lands the luminance exactly on the minimum, which
            // keeps as much of the original hue as possible.
            var t = (MinNameLuminance - luminance) / (1f - luminance);
            return UnityEngine.Color.Lerp(color, UnityEngine.Color.white, t);
        }
    }
}
