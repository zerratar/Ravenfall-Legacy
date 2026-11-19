using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Ravebridge's introduction (the user, 2026-11-03): the first time the streamer plays on Ravebridge, a window (IntroWindow) in the
    /// manner of a modern game's tutorial, over a dimmed screen: page by page a picture from the game (drifting slowly,
    /// as a looped clip would move), a title, a few lines and the keys, how hero play works (moving and fighting, the
    /// skills, the people and their quests, the map and the gear, chat fighting along). Back and Next, the dots, the
    /// arrow keys, Enter and Esc. Once per streamer on this PC; the Ravebridge panel's ? opens it any time, and the
    /// settings (Interface, "Introductions") show it, and the rising from the sea, again.
    /// While it is open the hero does not move or fight (HeroController asks <see cref="IsOpen"/>).
    /// </summary>
    public static class RavebridgeIntro
    {
        private static readonly IntroWindow.Page[] Pages =
        {
            new IntroWindow.Page { Picture = "intro-1", Title = () => Localization.UI_RB_INTRO_1_TITLE, Text = () => Localization.UI_RB_INTRO_1_TEXT, Keys = new (string, Func<string>)[0] },
            new IntroWindow.Page { Picture = "intro-2", Title = () => Localization.UI_RB_INTRO_2_TITLE, Text = () => Localization.UI_RB_INTRO_2_TEXT,
                Keys = new (string, Func<string>)[] { ("WASD", () => Localization.UI_RB_INTRO_KEY_WALK), ("Click", () => Localization.UI_RB_INTRO_KEY_ATTACK), ("Space", () => Localization.UI_RB_INTRO_KEY_DODGE), ("V", () => Localization.UI_RB_INTRO_KEY_CAMERA) } },
            new IntroWindow.Page { Picture = "intro-3", Title = () => Localization.UI_RB_INTRO_3_TITLE, Text = () => Localization.UI_RB_INTRO_3_TEXT,
                Keys = new (string, Func<string>)[] { ("1-4", () => Localization.UI_RB_INTRO_KEY_SKILLS), ("Q", () => Localization.UI_RB_INTRO_KEY_POTION), ("Tab", () => Localization.UI_RB_INTRO_KEY_STYLE) } },
            new IntroWindow.Page { Picture = "intro-4", Title = () => Localization.UI_RB_INTRO_4_TITLE, Text = () => Localization.UI_RB_INTRO_4_TEXT,
                Keys = new (string, Func<string>)[] { ("E", () => Localization.UI_RB_INTRO_KEY_TALK) } },
            new IntroWindow.Page { Picture = "intro-5", Title = () => Localization.UI_RB_INTRO_5_TITLE, Text = () => Localization.UI_RB_INTRO_5_TEXT,
                Keys = new (string, Func<string>)[] { ("M", () => Localization.UI_RB_INTRO_KEY_MAP), ("I", () => Localization.UI_RB_INTRO_KEY_BAG), ("C", () => Localization.UI_RB_INTRO_KEY_CHARACTER) } },
            new IntroWindow.Page { Picture = "intro-6", Title = () => Localization.UI_RB_INTRO_6_TITLE, Text = () => Localization.UI_RB_INTRO_6_TEXT, Keys = new (string, Func<string>)[0] },
        };

        private static IntroWindow window;

        public static bool IsOpen => window != null && window.IsOpen;

        /// <summary>The streamer has seen it (kept on this PC, per streamer).</summary>
        private static string SeenKey
        {
            get
            {
                var game = GameManager.Instance;
                return "ravebridge.intro." + (game && game.RavenNest != null ? game.RavenNest.UserId.ToString() : "local");
            }
        }

        /// <summary>
        /// Just started playing (RavebridgePlay): the first time, it opens a moment later, once the hero is in control;
        /// not in the middle of a fight (a storm starts its waves at once), then it waits for another time.
        /// </summary>
        public static void ShowFirstTime()
        {
            if (PlayerPrefs.GetInt(SeenKey, 0) == 1 || IsOpen) return;
            var host = IntroWindow.Host();
            host?.schedule.Execute(() =>
            {
                if (!HeroController.Active || WaveRun.Current || IsOpen || PlayerPrefs.GetInt(SeenKey, 0) == 1) return;
                Open();
            }).StartingIn(1200);
        }

        /// <summary>Shows the introduction and the rising from the sea again next time (the settings).</summary>
        public static void ForgetSeen()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            PlayerPrefs.Save();
        }

        public static void Open()
        {
            if (IsOpen) return;
            window = IntroWindow.Open("ravebridge-intro", Localization.UI_RB_INTRO_EYEBROW, "HeroUI/", Pages);
            if (window == null) return;
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
        }

        public static void Close() => window?.Close();
    }
}
