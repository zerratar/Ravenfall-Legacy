using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The game menu (Esc or the cog), in place of the uGUI Game Menu. <see cref="GameMenuHandler"/>
    /// stays the entry point: it opens and closes this view, and the buttons call back into it, so
    /// logging out, exiting and the login screen work as before. Log out and exit ask for a second
    /// click, since one stray click used to end the stream's game.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameMenuWindow : MonoBehaviour
    {
        [SerializeField] private GameMenuHandler handler;
        [SerializeField] private GameManager gameManager;
        [Tooltip("How long log out and exit wait for the second click, in seconds.")]
        [SerializeField] private float confirmSeconds = 3f;

        public bool IsOpen => open;

        private VisualElement modal;
        private VisualElement panel;
        private Label tip;
        private Button logoutButton;
        private Label uptimeValue;

        private bool bound;
        private bool logsHooked;
        private bool open;
        private int openedFrame = -1;
        private float nextInfoRefresh;
        private int lastTip = -1;

        private readonly ConfirmButton logout = new ConfirmButton();
        private readonly ConfirmButton exit = new ConfirmButton();

        /// <summary>A button that asks twice: the first click arms it, a second within the time acts.</summary>
        private class ConfirmButton
        {
            public Button Button;
            public Label Text;
            public string Normal;
            public string Confirm;
            public float ArmedUntil;

            public bool Armed => Time.unscaledTime < ArmedUntil;

            public void Disarm()
            {
                ArmedUntil = 0f;
                if (Text != null) Text.text = Normal;
                Button?.RemoveFromClassList("rf-gamemenu__button--confirm");
            }

            public void Arm(float seconds)
            {
                ArmedUntil = Time.unscaledTime + seconds;
                Text.text = Confirm;
                Button.AddToClassList("rf-gamemenu__button--confirm");
            }
        }

        private void Awake()
        {
            if (!handler) handler = FindAnyObjectByType<GameMenuHandler>(FindObjectsInactive.Include);
            if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
        }

        private void OnEnable()
        {
            Languages.Changed += OnLanguageChanged;
            TryBind();
        }

        private void OnDisable()
        {
            Languages.Changed -= OnLanguageChanged;
            bound = false;
        }

        private void OnLanguageChanged()
        {
            if (!bound) return;
            var root = GetComponent<UIDocument>().rootVisualElement;
            BuildButtons(root);
            BuildInfo(root.Q<VisualElement>("gamemenu-info"));
            if (open) ShowNow();
        }

        private void Update()
        {
            // A live reload of the markup or styles replaces the tree; bind again to the new one.
            if (bound && modal.panel == null) bound = false;

            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            if (!open) return;

            // Not in the frame the menu opened in: Esc opened it, and the same key press must not close it.
            if (Input.GetKeyUp(KeyCode.Escape) && Time.frameCount != openedFrame)
            {
                handler.Hide();
                return;
            }

            if (logout.ArmedUntil > 0f && !logout.Armed) logout.Disarm();
            if (exit.ArmedUntil > 0f && !exit.Armed) exit.Disarm();

            if (Time.unscaledTime >= nextInfoRefresh && uptimeValue != null)
            {
                nextInfoRefresh = Time.unscaledTime + 1f;
                uptimeValue.text = Utility.FormatTime(TimeSpan.FromSeconds(Time.time));
            }
        }

        public void Open()
        {
            open = true;
            openedFrame = Time.frameCount;
            if (bound) ShowNow();
        }

        public void Close()
        {
            open = false;
            if (modal != null) modal.style.display = DisplayStyle.None;
        }

        private void TryBind()
        {
            if (bound) return;

            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null) return;

            modal = root.Q<VisualElement>("gamemenu-modal");
            if (modal == null) return;

            panel = root.Q<VisualElement>("gamemenu-panel");
            tip = root.Q<Label>("gamemenu-tip");

            // Clicking the dimmed backdrop, outside the panel, is the same as Continue.
            modal.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.target == modal) handler.Hide();
            });

            BuildButtons(root);
            BuildInfo(root.Q<VisualElement>("gamemenu-info"));

            bound = true;
            if (open) ShowNow();
            else modal.style.display = DisplayStyle.None;
        }

        /// <summary>The buttons and the log folder link, again after a change of language.</summary>
        private void BuildButtons(VisualElement root)
        {
            var buttons = root.Q<VisualElement>("gamemenu-buttons");
            buttons.Clear();
            AddButton(buttons, "\uf04b", Localization.UI_MENU_CONTINUE, "rf-gamemenu__button--primary", () => handler.Hide());
            AddButton(buttons, "\uf013", Localization.UI_MENU_SETTINGS, null, () => handler.ShowSettingsMenu());
            var divider = new VisualElement { pickingMode = PickingMode.Ignore };
            divider.AddToClassList("rf-gamemenu__divider");
            buttons.Add(divider);
            logoutButton = AddConfirmButton(buttons, "\uf2f5", Localization.UI_MENU_LOGOUT, Localization.UI_MENU_CONFIRM_LOGOUT, logout, () => handler.Logout());
            AddConfirmButton(buttons, "\uf011", Localization.UI_MENU_EXIT, Localization.UI_MENU_CONFIRM_EXIT, exit, () => handler.Exit());

            var logs = root.Q<Button>("gamemenu-logs");
            logs.text = "→ " + Localization.UI_MENU_LOG_FOLDER;
            if (!logsHooked)
            {
                logs.clicked += () => handler.OpenPlayerLogFolder();
                logsHooked = true;
            }
        }

        private void ShowNow()
        {
            logout.Disarm();
            exit.Disarm();
            if (logoutButton != null)
            {
                var signedIn = gameManager && gameManager.RavenNest != null && gameManager.RavenNest.Authenticated;
                logoutButton.style.display = signedIn ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // a different tip each time the menu opens
            var tips = Localization.UI_MENU_TIPS;
            if (tips.Length > 0 && tip != null)
            {
                var pick = UnityEngine.Random.Range(0, tips.Length);
                if (pick == lastTip && tips.Length > 1) pick = (pick + 1) % tips.Length;
                lastTip = pick;
                tip.text = tips[pick];
            }

            nextInfoRefresh = 0f;
            modal.style.display = DisplayStyle.Flex;
        }

        private static Button AddButton(VisualElement parent, string glyph, string text, string extraClass, Action clicked)
        {
            var button = new Button(clicked);
            button.AddToClassList("rf-gamemenu__button");
            if (extraClass != null) button.AddToClassList(extraClass);
            var icon = new Label(glyph) { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-gamemenu__icon");
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rf-gamemenu__text");
            button.Add(icon);
            button.Add(label);
            parent.Add(button);
            return button;
        }

        private Button AddConfirmButton(VisualElement parent, string glyph, string text, string confirm, ConfirmButton state, Action act)
        {
            var button = AddButton(parent, glyph, text, "rf-gamemenu__button--danger", null);
            state.Button = button;
            state.Text = button.Q<Label>(className: "rf-gamemenu__text");
            state.Normal = text;
            state.Confirm = confirm;
            button.clicked += () =>
            {
                if (state.Armed)
                {
                    state.Disarm();
                    act();
                }
                else
                {
                    state.Arm(confirmSeconds);
                }
            };
            return button;
        }

        private void BuildInfo(VisualElement info)
        {
            info.Clear();
            var keys = Localization.UI_MENU_INFO;
            var values = new[]
            {
                Ravenfall.Version,
                SystemInfo.graphicsDeviceType.ToString(),
                SystemInfo.graphicsMemorySize + " MB",
                SystemInfo.systemMemorySize + " MB",
                SystemInfo.operatingSystem,
                string.Empty,
            };

            for (var i = 0; i < keys.Length && i < values.Length; ++i)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("rf-gamemenu__info-row");
                var key = new Label(keys[i]) { pickingMode = PickingMode.Ignore };
                key.AddToClassList("rf-gamemenu__info-key");
                var value = new Label(values[i]) { pickingMode = PickingMode.Ignore };
                value.AddToClassList("rf-gamemenu__info-value");
                row.Add(key);
                row.Add(value);
                info.Add(row);
                if (i == keys.Length - 1) uptimeValue = value;
            }
        }
    }
}
