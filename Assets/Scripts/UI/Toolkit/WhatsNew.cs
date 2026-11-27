using UnityEngine;

namespace Shinobytes.UI
{
    /// <summary>
    /// What is new in this version (the user, 2026-11-17: in game, on the website and on Discord, not in chat): the first
    /// time a streamer runs a v0.9.7 build, an IntroWindow explains the new combat in a few pages (health and armour,
    /// elements, combat skills, battle pets, gems, the tower), with the chat commands in gold. It waits until the game has
    /// loaded and nothing else is on screen (a cutscene, another introduction), so it never stacks on them. Once per
    /// streamer per version on this PC; the settings (Interface, "What's new") open it again.
    /// </summary>
    public sealed class WhatsNew : MonoBehaviour
    {
        /// <summary>The version these pages are about: a new version gets new pages and a new key.</summary>
        public const string Version = "0.9.7";

        /// <summary>Seconds after the game has loaded and signed in before it may open.</summary>
        private const float Settle = 8f;

        private static readonly IntroWindow.Page[] Pages =
        {
            new IntroWindow.Page { Picture = "whatsnew-1", Title = () => Localization.UI_WHATSNEW_1_TITLE, Text = () => Localization.UI_WHATSNEW_1_TEXT },
            new IntroWindow.Page { Picture = "whatsnew-2", Title = () => Localization.UI_WHATSNEW_2_TITLE, Text = () => Localization.UI_WHATSNEW_2_TEXT },
            new IntroWindow.Page { Picture = "whatsnew-3", Title = () => Localization.UI_WHATSNEW_3_TITLE, Text = () => Localization.UI_WHATSNEW_3_TEXT },
            new IntroWindow.Page { Picture = "whatsnew-4", Title = () => Localization.UI_WHATSNEW_4_TITLE, Text = () => Localization.UI_WHATSNEW_4_TEXT },
            new IntroWindow.Page { Picture = "whatsnew-5", Title = () => Localization.UI_WHATSNEW_5_TITLE, Text = () => Localization.UI_WHATSNEW_5_TEXT },
            new IntroWindow.Page { Picture = "whatsnew-6", Title = () => Localization.UI_WHATSNEW_6_TITLE, Text = () => Localization.UI_WHATSNEW_6_TEXT },
            new IntroWindow.Page { Picture = "whatsnew-7", Title = () => Localization.UI_WHATSNEW_7_TITLE, Text = () => Localization.UI_WHATSNEW_7_TEXT },
            new IntroWindow.Page { Picture = "whatsnew-8", Title = () => Localization.UI_WHATSNEW_8_TITLE, Text = () => Localization.UI_WHATSNEW_8_TEXT },
            new IntroWindow.Page { Picture = "whatsnew-9", Title = () => Localization.UI_WHATSNEW_9_TITLE, Text = () => Localization.UI_WHATSNEW_9_TEXT },
        };

        private static IntroWindow window;
        private float readySince = -1f;
        private float nextCheck;

        public static bool IsOpen => window != null && window.IsOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (!Application.isPlaying) return;
            var go = new GameObject("What's New");
            DontDestroyOnLoad(go);
            go.AddComponent<WhatsNew>();
        }

        private static string SeenKey
        {
            get
            {
                var game = GameManager.Instance;
                return "whatsnew." + Version + "." + (game && game.RavenNest != null ? game.RavenNest.UserId.ToString() : "local");
            }
        }

        public static void Open()
        {
            if (IsOpen) return;
            window = IntroWindow.Open("whats-new", string.Format(Localization.UI_WHATSNEW_EYEBROW, Version), "WhatsNew/", Pages);
            if (window == null) return;
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
        }

        public static void ForgetSeen()
        {
            PlayerPrefs.DeleteKey(SeenKey);
            PlayerPrefs.Save();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + 1f;
            if (!Calm())
            {
                readySince = -1f;
                return;
            }
            if (readySince < 0f) readySince = Time.unscaledTime;
            if (Time.unscaledTime - readySince < Settle) return;
            if (PlayerPrefs.GetInt(SeenKey, 0) == 1)
            {
                // seen: nothing more to do this session
                enabled = false;
                return;
            }
            Open();
            if (IsOpen) enabled = false;
        }

        /// <summary>Signed in, loaded, and nothing else on the screen. (The pages come with the build, so no version check.)</summary>
        private static bool Calm()
        {
            var game = GameManager.Instance;
            if (!game || !game.IsLoaded || game.RavenNest == null || !game.RavenNest.SessionStarted) return false;
            if (IntroWindow.Current != null && IntroWindow.Current.IsOpen) return false;
            if (TowerCutscene.Playing || RavebridgeReveal.Playing || HeroController.Active) return false;
            if (game.Camera && !game.Camera.CinematicFree) return false;
            return true;
        }
    }
}
