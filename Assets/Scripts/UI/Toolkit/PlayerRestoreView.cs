using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Asks the streamer about the players the server remembers in the stream that the state file did not have
    /// (ServerPlayerRestore). The window opens by itself when there are some, folds into a pill above the connection
    /// warnings after <see cref="AutoHideSeconds"/> (a streamer who is away is not left with it on stream), and the
    /// pill brings it back. It never holds anything up: the state file's players are restored before it opens.
    /// </summary>
    /// <remarks>Shares the connection warnings' document; ConnectionStatusView adds it.</remarks>
    [RequireComponent(typeof(UIDocument))]
    public class PlayerRestoreView : MonoBehaviour
    {
        private const float AutoHideSeconds = 30f;
        private const int NamesShown = 8;

        private VisualElement pill;
        private Label pillText;
        private VisualElement window;
        private Label title;
        private Label body;
        private Label names;
        private Toggle always;
        private Button yes;
        private Button later;
        private Button decline;
        private VisualElement timerFill;
        private bool bound;

        private int shownVersion = -1;
        private bool windowOpen;
        private float openedAt;

        private void OnEnable()
        {
            bound = false;
        }

        private void TryBind()
        {
            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            pill = root?.Q<VisualElement>("restore-pill");
            window = root?.Q<VisualElement>("restore-window");
            if (pill == null || window == null)
            {
                return;
            }

            pillText = root.Q<Label>("restore-pill-text");
            title = root.Q<Label>("restore-title");
            body = root.Q<Label>("restore-body");
            names = root.Q<Label>("restore-names");
            always = root.Q<Toggle>("restore-always");
            yes = root.Q<Button>("restore-yes");
            later = root.Q<Button>("restore-later");
            decline = root.Q<Button>("restore-decline");
            timerFill = root.Q<VisualElement>("restore-timer-fill");

            if (title != null) title.text = Localization.UI_RESTORE_TITLE;
            pill.tooltip = Localization.UI_RESTORE_PILL_HINT;
            pill.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => Open());

            if (always != null)
            {
                always.text = Localization.UI_RESTORE_ALWAYS;
                always.SetValueWithoutNotify(SettingsApplier.AlwaysRestoreFromServer);
            }

            if (later != null)
            {
                later.text = Localization.UI_RESTORE_LATER;
                later.clicked += Fold;
            }

            if (decline != null)
            {
                decline.text = Localization.UI_RESTORE_DECLINE;
                decline.clicked += () =>
                {
                    var game = GameManager.Instance;
                    if (game) game.ServerRestore.Decline(game);
                    Close();
                };
            }

            if (yes != null)
            {
                yes.clicked += () =>
                {
                    var game = GameManager.Instance;
                    if (!game) return;
                    if (always != null && always.value != SettingsApplier.AlwaysRestoreFromServer)
                    {
                        SettingsApplier.SetAlwaysRestoreFromServer(always.value);
                        SettingsApplier.Save();
                    }

                    game.ServerRestore.RestorePending(game);
                    Close();
                };
            }

            pill.style.display = DisplayStyle.None;
            window.style.display = DisplayStyle.None;
            windowOpen = false;
            shownVersion = -1;
            bound = true;
        }

        private void Update()
        {
            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            var game = GameManager.Instance;
            var restore = game ? game.ServerRestore : null;
            var count = restore != null && !restore.Restoring ? restore.Pending.Count : 0;

            if (count == 0)
            {
                if (windowOpen) Close();
                SetDisplay(pill, false);
                shownVersion = restore?.Version ?? -1;
                return;
            }

            // a new set of players to restore: ask
            if (restore.Version != shownVersion)
            {
                shownVersion = restore.Version;
                Fill(restore);
                Open();
            }

            SetDisplay(pill, !windowOpen);

            if (windowOpen)
            {
                var left = Mathf.Clamp01(1f - (Time.unscaledTime - openedAt) / AutoHideSeconds);
                if (timerFill != null) timerFill.style.width = new Length(left * 100f, LengthUnit.Percent);
                if (left <= 0f) Fold();
            }
        }

        private void Fill(ServerPlayerRestore restore)
        {
            var count = restore.Pending.Count;
            if (body != null) body.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_RESTORE_BODY, count);
            if (yes != null) yes.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_RESTORE_BUTTON, count);
            if (pillText != null) pillText.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_RESTORE_PILL, count);
            if (names != null)
            {
                var shown = restore.Pending.Take(NamesShown).Select(x => x.User?.Username ?? "?");
                var text = string.Join(", ", shown);
                if (count > NamesShown) text += " " + string.Format(CultureInfo.InvariantCulture, Localization.UI_RESTORE_MORE, count - NamesShown);
                names.text = text;
            }
        }

        private void Open()
        {
            var restore = GameManager.Instance ? GameManager.Instance.ServerRestore : null;
            if (restore == null || restore.Pending.Count == 0) return;
            Fill(restore);
            if (always != null) always.SetValueWithoutNotify(SettingsApplier.AlwaysRestoreFromServer);
            windowOpen = true;
            openedAt = Time.unscaledTime;
            SetDisplay(window, true);
            SetDisplay(pill, false);
        }

        /// <summary>Into the pill: the players wait.</summary>
        private void Fold()
        {
            windowOpen = false;
            SetDisplay(window, false);
        }

        private void Close()
        {
            windowOpen = false;
            SetDisplay(window, false);
            SetDisplay(pill, false);
        }

        private static void SetDisplay(VisualElement element, bool visible)
        {
            if (element == null) return;
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (element.style.display != display) element.style.display = display;
        }
    }
}
