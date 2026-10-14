using System;
using System.Linq;
using UnityEngine;
using RavenNest.SDK;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The login screen, in place of the uGUI one. <see cref="LoginHandler"/> keeps the logic: saved
    /// and automatic logins, remembering the account, and the call to the server. This view only
    /// draws the form, hands the handler what was typed, and shows how it went.
    /// </summary>
    /// <remarks>
    /// The texts follow a change of language in place rather than through a rebuild of the tree,
    /// which would empty the fields while someone is typing in them.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class LoginScreenView : MonoBehaviour
    {
        /// <summary>The password page of the site the game talks to; the local one in development.</summary>
        private static string PasswordPage
        {
            get
            {
                var site = GameManager.Instance != null && GameManager.Instance.RavenNest != null
                    ? GameManager.Instance.RavenNest.WebsiteUrl : "https://www.ravenfall.stream";
                return site + "/password";
            }
        }

        [SerializeField] private LoginHandler handler;
        [Tooltip("Degrees per second the busy ring on the button turns.")]
        [SerializeField] private float spinnerSpeed = 360f;

        /// <summary>Raised when the form is sent, by the button or Enter.</summary>
        public event Action Submitted;

        /// <summary>Raised by the Twitch button: the handler starts the sign in through the website.</summary>
        public event Action TwitchRequested;

        /// <summary>Raised when the wait for the website is cancelled.</summary>
        public event Action TwitchCancelled;

        public string Username => username != null ? username.value : cachedUsername;
        public string Password => password != null ? password.value : cachedPassword;
        public bool RememberMe => remember;

        private VisualElement root;
        private VisualElement screen;
        private TextField username;
        private TextField password;
        private Button reveal;
        private VisualElement rememberBox;
        private Button submit;
        private Label submitText;
        private VisualElement spinner;
        private VisualElement error;
        private Label errorTitle;
        private Label errorText;
        private Button language;
        private Label languageText;
        private Button twitch;
        private VisualElement form;
        private VisualElement wait;
        private Label waitCode;
        private VisualElement waitSpinner;

        // the wait for the website, kept for when the tree is built again
        private string waitingCode;
        private string waitingUrl;
        private string noticeTitle;
        private string noticeText;

        private bool bound;
        private bool visible = true;
        private bool busy;
        private bool remember;
        private bool revealed;
        private float spin;

        // what the form held, for when the tree is built again
        private string cachedUsername = string.Empty;
        private string cachedPassword = string.Empty;
        private LoginFailure shownFailure;
        private bool missingShown;

        private void Awake()
        {
            if (!handler) handler = FindAnyObjectByType<LoginHandler>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            Languages.Changed += ApplyTexts;
            TryBind();
        }

        private void OnDisable()
        {
            Languages.Changed -= ApplyTexts;
            if (bound)
            {
                cachedUsername = username.value;
                cachedPassword = password.value;
            }
            bound = false;
        }

        private void Update()
        {
            // A live reload of the markup or styles replaces the tree; bind again to the new one.
            if (bound && screen.panel == null) OnDisable();

            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            if (!string.IsNullOrEmpty(waitingCode) && waitSpinner != null)
            {
                spin = (spin + spinnerSpeed * Time.unscaledDeltaTime) % 360f;
                waitSpinner.style.rotate = new Rotate(spin);
            }

            if (busy && spinner != null)
            {
                spin = (spin + spinnerSpeed * Time.unscaledDeltaTime) % 360f;
                spinner.style.rotate = new Rotate(spin);
            }
        }

        // ---- Called by LoginHandler ------------------------------------------------------

        public void Show()
        {
            visible = true;
            if (!bound) return;
            screen.style.display = DisplayStyle.Flex;
            FocusFirstEmpty();
        }

        public void Hide()
        {
            visible = false;
            if (bound) screen.style.display = DisplayStyle.None;
        }

        /// <summary>Fills the form, as a saved or automatic login does before it signs in.</summary>
        public void Fill(string user, string pass, bool rememberMe)
        {
            cachedUsername = user ?? string.Empty;
            cachedPassword = pass ?? string.Empty;
            remember = rememberMe;
            if (!bound) return;
            username.SetValueWithoutNotify(cachedUsername);
            password.SetValueWithoutNotify(cachedPassword);
            ApplyRemember();
        }

        public void SetBusy(bool value)
        {
            busy = value;
            if (!bound) return;
            screen.EnableInClassList("rf-login--busy", value);
            submit.SetEnabled(!value);
            twitch?.SetEnabled(!value);
            username.SetEnabled(!value);
            password.SetEnabled(!value);
            submitText.text = value ? Localization.UI_LOGIN_BUSY : Localization.UI_LOGIN_SUBMIT;
            if (value) ShowFailure(LoginFailure.None);
        }

        /// <summary>Shows why the login did not go through, or clears the message with None.</summary>
        public void ShowFailure(LoginFailure failure)
        {
            shownFailure = failure;
            missingShown = false;
            noticeTitle = null;
            noticeText = null;
            if (!bound) return;

            if (failure == LoginFailure.None)
            {
                error.style.display = DisplayStyle.None;
                return;
            }

            var unreachable = failure == LoginFailure.Unreachable;
            errorTitle.text = unreachable ? Localization.UI_LOGIN_UNREACHABLE_TITLE : Localization.UI_LOGIN_FAILED_TITLE;
            errorText.text = unreachable ? Localization.UI_LOGIN_UNREACHABLE : Localization.UI_LOGIN_FAILED;
            error.style.display = DisplayStyle.Flex;

            if (!unreachable)
            {
                // most people who get this have the name right and the password wrong
                password.schedule.Execute(() => { password.Focus(); password.SelectAll(); });
            }
        }

        /// <summary>A message of the handler's own in the same box, such as a Twitch sign in that ran out.</summary>
        public void ShowNotice(string title, string text)
        {
            shownFailure = LoginFailure.None;
            missingShown = false;
            noticeTitle = title;
            noticeText = text;
            if (!bound) return;
            errorTitle.text = title ?? string.Empty;
            errorText.text = text ?? string.Empty;
            error.style.display = DisplayStyle.Flex;
        }

        /// <summary>Swaps the form for the wait while the streamer approves in the browser.</summary>
        public void ShowTwitchWaiting(string displayCode, string url)
        {
            waitingCode = displayCode;
            waitingUrl = url;
            ShowFailure(LoginFailure.None);
            ApplyWaiting();
        }

        public void EndTwitchWaiting()
        {
            waitingCode = null;
            waitingUrl = null;
            ApplyWaiting();
        }

        private void ApplyWaiting()
        {
            if (!bound) return;
            var waiting = !string.IsNullOrEmpty(waitingCode);
            screen.EnableInClassList("rf-login--waiting", waiting);
            if (form != null) form.style.display = waiting ? DisplayStyle.None : DisplayStyle.Flex;
            if (wait != null) wait.style.display = waiting ? DisplayStyle.Flex : DisplayStyle.None;
            if (waitCode != null) waitCode.text = waitingCode ?? string.Empty;
            if (!waiting && visible) FocusFirstEmpty();
        }

        private void ShowMissing()
        {
            shownFailure = LoginFailure.None;
            missingShown = true;
            errorTitle.text = Localization.UI_LOGIN_MISSING;
            errorText.text = string.Empty;
            error.style.display = DisplayStyle.Flex;
            FocusFirstEmpty();
        }

        // ---- Binding ---------------------------------------------------------------------

        private void TryBind()
        {
            if (bound) return;

            var doc = GetComponent<UIDocument>();
            root = doc != null ? doc.rootVisualElement : null;
            screen = root?.Q<VisualElement>("login-root");
            if (screen == null) return;

            username = root.Q<TextField>("login-user");
            password = root.Q<TextField>("login-pass");
            reveal = root.Q<Button>("login-reveal");
            rememberBox = root.Q<VisualElement>("login-remember");
            submit = root.Q<Button>("login-submit");
            submitText = root.Q<Label>("login-submit-text");
            spinner = root.Q<VisualElement>("login-spinner");
            error = root.Q<VisualElement>("login-error");
            errorTitle = root.Q<Label>("login-error-title");
            errorText = root.Q<Label>("login-error-text");
            language = root.Q<Button>("login-language");
            languageText = root.Q<Label>("login-language-text");
            twitch = root.Q<Button>("login-twitch");
            form = root.Q<VisualElement>("login-form");
            wait = root.Q<VisualElement>("login-wait");
            waitCode = root.Q<Label>("login-wait-code");
            waitSpinner = root.Q<VisualElement>("login-wait-spinner");

            if (username == null || password == null || submit == null) return;

            password.isPasswordField = true;
            password.maskChar = '\u2022';
            username.SetValueWithoutNotify(cachedUsername);
            password.SetValueWithoutNotify(cachedPassword);

            // Hooked once per tree. A bind that got this far and then failed used to hook
            // everything again on the retry, so each click ran twice and the toggles flipped back.
            if (screen.userData != (object)this)
            {
                screen.userData = this;

                // Enter in the name moves on to the password; Enter in the password signs in
                username.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (!IsEnter(evt)) return;
                    if (string.IsNullOrEmpty(password.value)) password.Focus(); else Submit();
                    evt.StopPropagation();
                }, TrickleDown.TrickleDown);
                password.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (!IsEnter(evt)) return;
                    Submit();
                    evt.StopPropagation();
                }, TrickleDown.TrickleDown);

                // the row around each field draws the frame, so it lights up on the field's focus
                foreach (var field in new[] { username, password })
                {
                    var row = field.parent;
                    field.RegisterCallback<FocusInEvent>(_ => row.AddToClassList("rf-login__input--focus"));
                    field.RegisterCallback<FocusOutEvent>(_ => row.RemoveFromClassList("rf-login__input--focus"));
                }

                // typing again clears an old complaint
                username.RegisterValueChangedCallback(_ => ClearMissing());
                password.RegisterValueChangedCallback(_ => ClearMissing());

                submit.clicked += Submit;
                if (reveal != null) reveal.clicked += ToggleReveal;
                if (rememberBox != null)
                {
                    rememberBox.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => { remember = !remember; ApplyRemember(); });
                }

                var forgot = root.Q<Button>("login-forgot");
                if (forgot != null) forgot.clicked += () => Application.OpenURL(PasswordPage);
                var quit = root.Q<Button>("login-quit");
                if (quit != null) quit.clicked += () => Quit();
                if (language != null) language.clicked += NextLanguage;
                if (twitch != null) twitch.clicked += () => { if (!busy) TwitchRequested?.Invoke(); };
                var reopen = root.Q<Button>("login-wait-reopen");
                if (reopen != null) reopen.clicked += () => { if (!string.IsNullOrEmpty(waitingUrl)) Application.OpenURL(waitingUrl); };
                var cancel = root.Q<Button>("login-wait-cancel");
                if (cancel != null) cancel.clicked += () => TwitchCancelled?.Invoke();
            }

            var version = root.Q<Label>("login-version");
            if (version != null) version.text = string.Format(Localization.UI_LOGIN_VERSION, SafeVersion());

            bound = true;
            ApplyTexts();
            ApplyRemember();
            ApplyReveal();
            SetBusy(busy);
            RestoreMessage();
            ApplyWaiting();

            screen.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible) FocusFirstEmpty();
        }

        private void ApplyTexts()
        {
            if (!bound) return;

            SetText("login-title", Localization.UI_LOGIN_TITLE);
            SetText("login-lead", Localization.UI_LOGIN_LEAD);
            SetText("login-user-label", Localization.UI_LOGIN_USERNAME);
            SetText("login-pass-label", Localization.UI_LOGIN_PASSWORD);
            SetText("login-remember-text", Localization.UI_LOGIN_REMEMBER);
            SetText("login-no-password", Localization.UI_LOGIN_NO_PASSWORD);
            SetButton("login-forgot", Localization.UI_LOGIN_FORGOT);
            SetText("login-quit-text", Localization.UI_LOGIN_QUIT);

            submitText.text = busy ? Localization.UI_LOGIN_BUSY : Localization.UI_LOGIN_SUBMIT;
            if (languageText != null)
            {
                var current = Languages.Available.FirstOrDefault(a => a.Code == Languages.Current);
                languageText.text = current != null ? current.Name : "English";
            }
            // more than one language to choose between
            if (language != null) language.style.display = Languages.Available.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;

            SetText("login-twitch-text", Localization.UI_LOGIN_TWITCH);
            SetText("login-or-text", Localization.UI_LOGIN_OR);
            SetText("login-wait-title", Localization.UI_LOGIN_WAIT_TITLE);
            SetText("login-wait-text", Localization.UI_LOGIN_WAIT_TEXT);
            SetText("login-wait-status", Localization.UI_LOGIN_WAITING);
            SetButton("login-wait-reopen", Localization.UI_LOGIN_WAIT_REOPEN);
            SetButton("login-wait-cancel", Localization.UI_LOGIN_WAIT_CANCEL);

            ApplyReveal();
            RestoreMessage();
        }

        private void RestoreMessage()
        {
            if (missingShown) ShowMissing();
            else if (noticeTitle != null) ShowNotice(noticeTitle, noticeText);
            else ShowFailure(shownFailure);
        }

        private void SetText(string name, string text)
        {
            var label = root.Q<Label>(name);
            if (label != null) label.text = text;
        }

        private void SetButton(string name, string text)
        {
            var button = root.Q<Button>(name);
            if (button != null) button.text = text;
        }

        private void ApplyRemember()
        {
            rememberBox?.EnableInClassList("rf-login__check--on", remember);
        }

        private void ToggleReveal()
        {
            revealed = !revealed;
            ApplyReveal();
            password.Focus();
        }

        private void ApplyReveal()
        {
            if (password == null) return;
            password.isPasswordField = !revealed;
            if (reveal == null) return;
            // eye to show, struck through eye to hide again
            reveal.text = revealed ? "\uf070" : "\uf06e";
            reveal.tooltip = revealed ? Localization.UI_LOGIN_HIDE_PASSWORD : Localization.UI_LOGIN_SHOW_PASSWORD;
        }

        private void ClearMissing()
        {
            if (missingShown || shownFailure != LoginFailure.None || noticeTitle != null)
            {
                ShowFailure(LoginFailure.None);
            }
        }

        private void FocusFirstEmpty()
        {
            var target = string.IsNullOrEmpty(username.value) ? username : password;
            target.schedule.Execute(() => target.Focus());
        }

        private void Submit()
        {
            if (busy) return;
            if (string.IsNullOrWhiteSpace(username.value) || string.IsNullOrEmpty(password.value))
            {
                ShowMissing();
                return;
            }

            cachedUsername = username.value;
            cachedPassword = password.value;
            Submitted?.Invoke();
        }

        private void NextLanguage()
        {
            var list = Languages.Available;
            if (list.Count < 2) return;
            var index = 0;
            for (var i = 0; i < list.Count; ++i) if (list[i].Code == Languages.Current) index = i;
            Languages.Set(list[(index + 1) % list.Count].Code);
        }

        private void Quit()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.SaveStateAndShutdownGame(false);
            else Application.Quit();
        }

        private static string SafeVersion()
        {
            try { return Ravenfall.Version; }
            catch { return Application.version; }
        }

        private static bool IsEnter(KeyDownEvent evt)
        {
            return evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter;
        }
    }
}
