using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Says when the game cannot hear chat, in place of the uGUI bot and Twitch icons that sat over
    /// the bottom right corner. Bottom left, out of the way of the windows and the corner menu, and
    /// only there while something is wrong: the bot is offline or still connecting, or it is
    /// connected but has not joined the channel. Hovering explains it; clicking opens the bot
    /// settings, where the connection is set.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ConnectionStatusView : MonoBehaviour
    {
        [Tooltip("Seconds between checks of the bot state.")]
        [SerializeField] private float refreshInterval = 0.25f;
        [Tooltip("How long the bot may be connected without joining the channel before that is shown.")]
        [SerializeField] private float joinGraceSeconds = 4f;
        [Tooltip("How long the server connection may be down before it is shown, so a blip does not flash it.")]
        [SerializeField] private float serverGraceSeconds = 2f;

        private enum Status { Fine, BotOffline, BotConnecting, NotInChat }
        private enum Server { Fine, Lost, OutOfDate }

        private VisualElement serverPill;
        private Label serverIcon;
        private Label serverTitle;
        private Label serverNote;
        private float serverDownSince = -1f;
        private Server shownServer = (Server)(-1);

        private VisualElement root;
        private VisualElement strip;
        private VisualElement chip;
        private Label icon;
        private Label text;
        private HudTooltip tooltip;

        private bool bound;
        private float nextRefresh;
        private float readySince = -1f;
        private Status shown = (Status)(-1);

        private void OnEnable()
        {
            // the players-to-restore pill and window share this document, next to these warnings
            if (!GetComponent<PlayerRestoreView>()) gameObject.AddComponent<PlayerRestoreView>();
            // and the stream quest's bar and window at the top center
            if (!GetComponent<StreamQuestView>()) gameObject.AddComponent<StreamQuestView>();
            Languages.Changed += Redraw;
            TryBind();
        }

        private void OnDisable()
        {
            Languages.Changed -= Redraw;
            bound = false;
        }

        private void Update()
        {
            if (bound && strip.panel == null) bound = false;
            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshInterval;

            var status = Current();
            if (status != shown) Apply(status);

            var server = CurrentServer();
            if (server != shownServer) ApplyServer(server);
        }

        private Status Current()
        {
            var gm = GameManager.Instance;
            var bot = gm != null ? gm.RavenBotController : null;
            if (bot == null || bot.State == BotState.NotSet || bot.State == BotState.Disconnected)
            {
                readySince = -1f;
                return Status.BotOffline;
            }

            if (bot.State == BotState.Connected)
            {
                readySince = -1f;
                return Status.BotConnecting;
            }

            if (bot.HasJoinedChannel)
            {
                return Status.Fine;
            }

            // joining takes a moment after the bot is ready; no warning for the usual few seconds
            if (readySince < 0f) readySince = Time.unscaledTime;
            return Time.unscaledTime - readySince < joinGraceSeconds ? Status.Fine : Status.NotInChat;
        }

        /// <summary>The game's own link to the server, once signed in: the same checks the old banner made.</summary>
        private Server CurrentServer()
        {
            var gm = GameManager.Instance;
            var nest = gm != null ? gm.RavenNest : null;
            if (nest == null || !nest.Authenticated)
            {
                serverDownSince = -1f;
                return Server.Fine;
            }

            if (!nest.SessionStarted && nest.BadClientVersion)
            {
                return Server.OutOfDate;
            }

            var connected = nest.Connected;
            if (!nest.SessionStarted || connected)
            {
                serverDownSince = -1f;
                return Server.Fine;
            }

            if (serverDownSince < 0f) serverDownSince = Time.unscaledTime;
            return Time.unscaledTime - serverDownSince < serverGraceSeconds ? Server.Fine : Server.Lost;
        }

        private void ApplyServer(Server server)
        {
            shownServer = server;
            if (serverPill == null) return;
            serverPill.style.display = server == Server.Fine ? DisplayStyle.None : DisplayStyle.Flex;
            serverPill.EnableInClassList("rf-conn-server--outdated", server == Server.OutOfDate);
            // a plug pulled out for a lost link, a download arrow for an update
            serverIcon.text = server == Server.OutOfDate ? "" : "";
            serverTitle.text = server == Server.OutOfDate ? Localization.UI_CONN_OUTDATED : Localization.UI_CONN_SERVER_LOST;
            serverNote.text = server == Server.OutOfDate ? Localization.UI_CONN_OUTDATED_NOTE : Localization.UI_CONN_SERVER_LOST_NOTE;
        }

        private void TryBind()
        {
            if (bound) return;
            var doc = GetComponent<UIDocument>();
            root = doc != null ? doc.rootVisualElement : null;
            strip = root?.Q<VisualElement>("conn-root");
            chip = root?.Q<VisualElement>("conn-chip");
            icon = root?.Q<Label>("conn-icon");
            text = root?.Q<Label>("conn-text");
            if (strip == null || chip == null || icon == null || text == null) return;
            serverPill = root.Q<VisualElement>("conn-server");
            serverIcon = root.Q<Label>("conn-server-icon");
            serverTitle = root.Q<Label>("conn-server-title");
            serverNote = root.Q<Label>("conn-server-note");

            if (chip.userData != (object)this)
            {
                chip.userData = this;
                chip.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => OpenSettings());
                tooltip = HudTooltip.Create(root.Q<VisualElement>("conn-layer") ?? root);
                tooltip?.Attach(chip, FillTooltip);
            }

            bound = true;
            shown = (Status)(-1);
            shownServer = (Server)(-1);
            nextRefresh = 0f;
        }

        private void Apply(Status status)
        {
            shown = status;
            strip.style.display = status == Status.Fine ? DisplayStyle.None : DisplayStyle.Flex;
            chip.EnableInClassList("rf-conn__chip--offline", status == Status.BotOffline);
            chip.EnableInClassList("rf-conn__chip--connecting", status == Status.BotConnecting);
            chip.EnableInClassList("rf-conn__chip--chat", status == Status.NotInChat);

            // the robot for the bot, the Twitch glitch for the channel
            icon.text = status == Status.NotInChat ? "\uf1e8" : "\uf544";
            icon.EnableInClassList("rf-icon--brand", status == Status.NotInChat);
            text.text = Title(status);
            if (status == Status.Fine) tooltip?.Hide();
        }

        private void Redraw()
        {
            if (!bound) return;
            Apply(shown < 0 ? Current() : shown);
            ApplyServer(shownServer < 0 ? CurrentServer() : shownServer);
        }

        private static string Title(Status status)
        {
            switch (status)
            {
                case Status.BotOffline: return Localization.UI_CONN_BOT_OFFLINE;
                case Status.BotConnecting: return Localization.UI_CONN_BOT_CONNECTING;
                case Status.NotInChat: return Localization.UI_CONN_NOT_IN_CHAT;
                default: return string.Empty;
            }
        }

        private void FillTooltip(HudTooltip t)
        {
            if (shown == Status.Fine) return;
            t.SetTitle(Title(shown));
            switch (shown)
            {
                case Status.BotOffline:
                    t.SetNote(Localization.UI_CONN_BOT_OFFLINE_NOTE);
                    break;
                case Status.BotConnecting:
                    t.SetNote(Localization.UI_CONN_BOT_CONNECTING_NOTE);
                    break;
                case Status.NotInChat:
                    var gm = GameManager.Instance;
                    var signedIn = gm != null && gm.RavenNest != null && gm.RavenNest.Authenticated;
                    t.SetNote(signedIn ? Localization.UI_CONN_NOT_IN_CHAT_NOTE : Localization.UI_CONN_NOT_IN_CHAT_SIGN_IN_NOTE);
                    break;
            }
        }

        private static void OpenSettings()
        {
            var settings = SettingsWindow.Instance;
            if (settings != null) settings.OpenTab(SettingsWindow.BotTab);
        }
    }
}
