using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// After a change of language, rebuilds every UI Toolkit window that draws its labels once when it
    /// binds. Switching a window's object off and on makes its UIDocument build the tree again and the
    /// view bind to it, which is what a reload does for them, so they come back in the new language
    /// with their own state (open, collapsed, where they were dragged to) intact. The settings window
    /// and the game menu redraw themselves and are left alone; rebuilding the settings window from
    /// inside its own language row would pull the tree out from under the click.
    /// </summary>
    public sealed class LanguageRebuild : MonoBehaviour
    {
        private static LanguageRebuild instance;
        private bool pending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (instance) return;
            var go = new GameObject("Language Rebuild") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            instance = go.AddComponent<LanguageRebuild>();
            Languages.Changed += () => { if (instance) instance.pending = true; };
        }

        private void Update()
        {
            // a frame later, so the click that changed the language has finished
            if (!pending) return;
            pending = false;

            foreach (var doc in FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (doc.GetComponent<SettingsWindow>() || doc.GetComponent<GameMenuWindow>()) continue;
                var go = doc.gameObject;
                go.SetActive(false);
                go.SetActive(true);
            }
        }
    }
}
