using System.Collections.Generic;
using System.Globalization;
using RavenNest.Combat;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The tower panel in the top centre status column, under the raid and dungeon panels and one of their family: the
    /// floor and part the stream is on, the part's objectives with their progress (kills, gathering, hidden things, and
    /// the guardian that waits behind them), the boss countdown and fight (time left, the boss's health), the town
    /// between parts, how many are inside and what to type next. While the stream watches a player who trains on a
    /// cleared part by themselves, the panel shows that part instead (docs/tower-worlds-proposal.md section 13): its
    /// name, how many train there, the way back to the climb, and a boss attempt called meanwhile. Its Map button (or
    /// !tower map) opens the floor map (<see cref="TowerMapView"/>) as a section of the panel.
    /// </summary>
    /// <remarks>
    /// Before this, nothing on screen showed the climb: viewers only learned the floor, the quests or a countdown by
    /// typing !tower info (docs/tower-design.md, section 2: "The quest tracker shows them").
    ///
    /// <para>
    /// The panel asks <see cref="TowerManager"/> four times a second rather than being pushed to, and rewrites a label
    /// only when its text changes. Its elements are made in code and added to the status column (TopStatus.uxml's
    /// top-column), so the column stacks it with whatever else is showing; when the document builds a new tree (a
    /// change of language) the panel is made again.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class TowerHudView : MonoBehaviour
    {
        private const float RefreshSeconds = 0.25f;
        // a part's counted objectives (two kinds of kills and a gather or a find, today) and its guardian
        private const int MaxQuests = 6;

        /// <summary>Seconds the map stays on screen when chat asks for it (!tower map), and between two such asks.</summary>
        public const float MapChatSeconds = 20f, MapChatCooldown = 60f;

        private static TowerHudView instance;

        /// <summary>The panel's own widths (ravenfall-hud.uss): as narrow as the raid panel, as wide as its longest line, wider with the map.</summary>
        private const float MinWidth = 780f, MaxWidth = 1240f, MapMinWidth = 1180f;

        private VisualElement hud, column;
        private Label title, floorLabel, timer, clock, barText, people, phase, join;
        private VisualElement liveDot, bar, fill, quests, joinPill;
        private Button mapButton, metersButton, actionButton;
        // the camera's way to the tower and back (WorldTowerCamera), and its short word when nobody is in the tower
        private Button cameraButton;
        private Label cameraText;
        private VisualElement cameraWord;
        private Label cameraWordText;
        private string cameraWords;
        private float cameraWordUntil = -1f;
        private Label actionText, mapText, metersText;
        // squeezed between the windows at the screen's sides (the players list, the player details): the header's
        // buttons are their icons, and the chat hint gives way to the streamer's button
        private bool compact;
        private bool joinWanted;
        private const float CompactWidth = 1000f;
        // what the action button does now: 0 nothing, 1 challenge the guardian, 2 start the fight now
        private int action;
        private TowerMapView map;
        // the map: open (the button), or shown until a time (chat); the floor it shows, 0 for the one being watched
        private bool mapOpen;
        private float mapUntil = -1f;
        private int mapFloor;
        private static float mapChatNext;
        private readonly List<(VisualElement row, Label text, Label count)> questRows = new List<(VisualElement, Label, Label)>();
        private float nextRefresh;
        private bool shown;
        private float shownFill = -1f;

        /// <summary>Adds the panel to the status column's document (the one the raid panel is on), once.</summary>
        public static bool Ensure()
        {
            var raid = FindAnyObjectByType<RaidHudView>();
            if (!raid) return false;
            if (!raid.GetComponent<TowerHudView>()) raid.gameObject.AddComponent<TowerHudView>();
            return true;
        }

        /// <summary>Seconds until chat can ask for the map again, 0 when it can.</summary>
        public static int MapWaitSeconds => Mathf.Max(0, Mathf.CeilToInt(mapChatNext - Time.unscaledTime));

        /// <summary>!tower map: the map of a floor on screen for a while. False when the panel is not up.</summary>
        public static bool ShowMapFor(int floor, float seconds)
        {
            if (!instance || instance.hud == null || !instance.shown) return false;
            instance.mapFloor = floor;
            instance.mapUntil = Time.unscaledTime + seconds;
            instance.nextRefresh = 0f;
            mapChatNext = Time.unscaledTime + MapChatCooldown;
            return true;
        }

        /// <summary>Where the panel lies on its UI panel (with the map when that is open), and that panel: the meters window keeps off it.</summary>
        public static Rect PanelBounds => instance && instance.hud != null ? instance.hud.worldBound : default;

        public static IPanel PanelOwner => instance && instance.hud != null ? instance.hud.panel : null;

        /// <summary>The map is open (the tests).</summary>
        public static bool MapShown => instance && instance.map != null && instance.map.Root.style.display == DisplayStyle.Flex;

        /// <summary>Opens or closes the map as its button does.</summary>
        public static void ToggleMap()
        {
            if (!instance) return;
            instance.mapOpen = !(instance.mapOpen || instance.mapUntil > Time.unscaledTime);
            instance.mapUntil = -1f;
            instance.mapFloor = 0;
            instance.nextRefresh = 0f;
        }

        private void OnAction()
        {
            var tower = TowerManager.Instance;
            if (!tower) return;
            // as the streamer's !tower boss and !tower boss now
            if (action == 1) tower.CallBoss(null, true, out _);
            else if (action == 2) tower.StartBossNow();
            nextRefresh = 0f;
        }

        private void SetAction(int what)
        {
            if (what == action) return;
            action = what;
            Show(actionButton, what != 0);
            if (what != 0) Set(actionText, what == 1 ? Localization.UI_TOWER_CHALLENGE : Localization.UI_TOWER_START_NOW);
            ShowJoin(joinWanted);
        }

        // the chat hint ("!tower boss"): not beside the streamer's button when there is no room for both and the line
        private void ShowJoin(bool show)
        {
            joinWanted = show;
            Show(joinPill, show && !(compact && action != 0));
        }

        private void SetCompact(bool value, bool clock)
        {
            // the panel's name gives its room to the clock when both do not fit
            Show(title, !(value && clock));
            if (value == compact) return;
            compact = value;
            Show(mapText, !value);
            Show(metersText, !value);
            mapButton.EnableInClassList("rf-tower__mapbtn--icon", value);
            metersButton.EnableInClassList("rf-tower__mapbtn--icon", value);
            Show(cameraText, !value);
            cameraButton.EnableInClassList("rf-tower__mapbtn--icon", value);
            ShowJoin(joinWanted);
        }

        /// <summary>A short word in the status column for a few seconds: the tower camera key pressed with nobody in the tower (the panel is not up then).</summary>
        public static void Word(string text, float seconds = 3f)
        {
            if (!instance) return;
            instance.cameraWords = text;
            instance.cameraWordUntil = Time.unscaledTime + seconds;
            instance.nextRefresh = 0f;
        }

        private void RefreshWord()
        {
            var show = cameraWordUntil > 0f && Time.unscaledTime < cameraWordUntil;
            if (!show && cameraWord == null) return;
            if (cameraWord == null || cameraWord.panel == null)
            {
                cameraWord = Box("rf-raid", "rf-raid--tower");
                cameraWord.name = "tower-camera-word";
                // as wide as its words, in the middle of the column
                cameraWord.style.width = StyleKeyword.Auto;
                cameraWord.style.minWidth = StyleKeyword.Auto;
                cameraWord.style.alignSelf = Align.Center;
                cameraWordText = Text("rf-raid__title");
                cameraWord.Add(cameraWordText);
                column.Add(cameraWord);
            }
            Set(cameraWordText, cameraWords ?? "");
            Show(cameraWord, show);
            if (!show) cameraWordUntil = -1f;
        }

        private void OnEnable() => instance = this;

        private void OnDisable()
        {
            if (instance == this) instance = null;
        }

        // The title of a lost attempt ("Defeated", why, what the guardian had left): shown as a clear's title is, but
        // short and over the game as it goes on; it waits while a cutscene plays.
        private VisualElement notice;
        private Label noticeTitle, noticeLine, noticeLeft;
        private float noticeUntil = -1f;
        private bool noticePending;
        private const float NoticeSeconds = 7f;

        /// <summary>A lost boss attempt, said on screen: the title, the reason under it, and what the guardian had left.</summary>
        public static void ShowDefeat(string title, string reason, string left)
        {
            if (!instance || instance.hud == null || instance.hud.panel == null) return;
            var view = instance;
            if (view.notice == null)
            {
                view.notice = new VisualElement { pickingMode = PickingMode.Ignore };
                view.notice.AddToClassList("rf-tower-notice");
                view.noticeTitle = new Label { pickingMode = PickingMode.Ignore };
                view.noticeTitle.AddToClassList("rf-tower-notice__title");
                view.noticeLine = new Label { pickingMode = PickingMode.Ignore };
                view.noticeLine.AddToClassList("rf-tower-notice__line");
                view.noticeLeft = new Label { pickingMode = PickingMode.Ignore };
                view.noticeLeft.AddToClassList("rf-tower-notice__line");
                view.noticeLeft.AddToClassList("rf-tower-notice__line--dim");
                view.notice.Add(view.noticeTitle);
                view.notice.Add(view.noticeLine);
                view.notice.Add(view.noticeLeft);
                view.notice.style.display = DisplayStyle.None;
                view.GetComponent<UIDocument>().rootVisualElement.Add(view.notice);
            }
            view.noticeTitle.text = title;
            view.noticeLine.text = reason;
            view.noticeLeft.text = left;
            view.noticePending = true;
        }

        private void UpdateNotice()
        {
            if (notice == null) return;
            // never over a cutscene or the Ravebridge reveal: it waits for them
            var busy = TowerCutscene.Playing || RavebridgeReveal.Playing;
            if (noticePending && !busy)
            {
                noticePending = false;
                noticeUntil = Time.unscaledTime + NoticeSeconds;
                notice.style.display = DisplayStyle.Flex;
                notice.RemoveFromClassList("rf-tower-notice--shown");
                notice.schedule.Execute(() => notice.AddToClassList("rf-tower-notice--shown")).StartingIn(30);
            }
            if (noticeUntil < 0f) return;
            if (busy || Time.unscaledTime >= noticeUntil)
            {
                noticeUntil = -1f;
                notice.RemoveFromClassList("rf-tower-notice--shown");
                notice.style.display = DisplayStyle.None;
            }
            else if (noticeUntil - Time.unscaledTime < 0.8f) notice.RemoveFromClassList("rf-tower-notice--shown");
        }

        private void Update()
        {
            UpdateNotice();
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + RefreshSeconds;
            if (hud == null || hud.panel == null)
            {
                if (!Build()) return;
            }
            RefreshWord();
            Refresh();
        }

        private bool Build()
        {
            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            column = root != null ? root.Q<VisualElement>("top-column") : null;
            if (column == null) return false;

            hud = Box("rf-raid", "rf-raid--tower");
            hud.name = "tower-hud";
            hud.style.display = DisplayStyle.None;
            shown = false;
            shownFill = -1f;

            var top = Box("rf-raid__top");
            title = Text("rf-raid__title");
            floorLabel = Text("rf-raid__level");
            liveDot = Box("rf-live-dot");
            clock = Text("rf-icon", "rf-raid__clock");
            clock.text = "";
            timer = Text("rf-raid__timer");
            top.Add(title);
            top.Add(floorLabel);
            top.Add(Box("rf-raid__spacer"));
            top.Add(liveDot);
            top.Add(clock);
            top.Add(timer);
            // the one thing on the panel that takes a click: the floor map
            mapButton = new Button(ToggleMap) { text = "" };
            mapButton.AddToClassList("rf-tower__mapbtn");
            var mapIcon = Text("rf-icon", "rf-tower__mapbtn-icon");
            mapIcon.text = "\uf279";
            mapText = Text("rf-tower__mapbtn-text");
            mapText.text = Localization.UI_TOWER_MAP;
            mapButton.tooltip = Localization.UI_TOWER_MAP;
            mapButton.Add(mapIcon);
            mapButton.Add(mapText);
            top.Add(mapButton);
            // and the meters of the part being watched (the dungeon's meters window): shown on a part, not in the town
            metersButton = new Button(DungeonMetersWindow.ToggleStretch) { text = "" };
            metersButton.AddToClassList("rf-tower__mapbtn");
            var metersIcon = Text("rf-icon", "rf-tower__mapbtn-icon");
            metersIcon.text = "\uf080";
            metersText = Text("rf-tower__mapbtn-text");
            metersText.text = Localization.UI_TOWER_METERS;
            metersButton.tooltip = Localization.UI_TOWER_METERS;
            metersButton.Add(metersIcon);
            metersButton.Add(metersText);
            top.Add(metersButton);
            // and the camera's way to the tower and back out to the world (WorldTowerCamera; its key does the same)
            cameraButton = new Button(() => WorldTowerCamera.Toggle()) { text = "" };
            cameraButton.AddToClassList("rf-tower__mapbtn");
            var cameraIcon = Text("rf-icon", "rf-tower__mapbtn-icon");
            cameraIcon.text = "\uf03d";
            cameraText = Text("rf-tower__mapbtn-text");
            cameraButton.tooltip = Localization.UI_KEY_TOWER_CAMERA;
            cameraButton.Add(cameraIcon);
            cameraButton.Add(cameraText);
            top.Add(cameraButton);
            hud.Add(top);

            bar = Box("rf-raid__bar");
            fill = Box("rf-raid__fill");
            barText = Text("rf-raid__hp");
            bar.Add(fill);
            bar.Add(barText);
            hud.Add(bar);

            quests = Box("rf-tower__quests");
            questRows.Clear();
            for (var i = 0; i < MaxQuests; i++)
            {
                var row = Box("rf-tower__quest");
                var check = Text("rf-icon", "rf-tower__check");
                check.text = "";
                var text = Text("rf-raid__raiders", "rf-tower__quest-text");
                var count = Text("rf-raid__level", "rf-tower__quest-count");
                row.Add(check);
                row.Add(text);
                row.Add(count);
                quests.Add(row);
                questRows.Add((row, text, count));
            }
            hud.Add(quests);

            var bottom = Box("rf-raid__bottom");
            var left = Box("rf-raid__side");
            var peopleIcon = Text("rf-icon", "rf-raid__people");
            peopleIcon.text = "";
            people = Text("rf-raid__raiders");
            left.Add(peopleIcon);
            left.Add(people);
            phase = Text("rf-raid__percent");
            var right = Box("rf-raid__side", "rf-raid__side--end");
            joinPill = Box("rf-raid__join-pill");
            var chat = Text("rf-icon", "rf-raid__chat");
            chat.text = "";
            join = Text("rf-raid__join");
            joinPill.Add(chat);
            joinPill.Add(join);
            // the streamer's own way to what chat does with !tower boss: challenge the guardian, and cut the
            // countdown short
            actionButton = new Button(OnAction) { text = "" };
            actionButton.AddToClassList("rf-tower__actbtn");
            actionText = Text("rf-tower__actbtn-text");
            actionButton.Add(actionText);
            Show(actionButton, false);
            right.Add(actionButton);
            right.Add(joinPill);
            bottom.Add(left);
            bottom.Add(phase);
            bottom.Add(right);
            hud.Add(bottom);

            map = new TowerMapView(step =>
            {
                var tower = TowerManager.Instance;
                if (!tower || map == null) return;
                mapFloor = Mathf.Clamp(map.Floor + step, 1, TowerWords.Floor(tower.StreamFloor));
                nextRefresh = 0f;
            });
            map.Root.style.display = DisplayStyle.None;
            hud.Add(map.Root);

            column.Add(hud);
            return true;
        }

        private static VisualElement Box(params string[] classes)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var c in classes) e.AddToClassList(c);
            return e;
        }

        private static Label Text(params string[] classes)
        {
            var l = new Label { pickingMode = PickingMode.Ignore };
            foreach (var c in classes) l.AddToClassList(c);
            return l;
        }

        private static void Set(Label label, string text)
        {
            if (label != null && label.text != text) label.text = text;
        }

        private static void Show(VisualElement e, bool show)
        {
            if (e != null) e.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetFill(float share)
        {
            share = Mathf.Clamp01(share);
            if (Mathf.Abs(share - shownFill) < 0.002f) return;
            shownFill = share;
            fill.style.width = new Length(share * 100f, LengthUnit.Percent);
        }

        private static string Clock(float seconds)
        {
            var s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (s / 60).ToString(CultureInfo.InvariantCulture) + ":" + (s % 60).ToString("00", CultureInfo.InvariantCulture);
        }

        // the map, when it is open: the floor asked for, else the one the watched player is on, else the stream's
        private void RefreshMap(TowerManager tower, TowerManager.Area watched, bool fighting)
        {
            if (map == null) return;
            if (mapUntil > 0f && Time.unscaledTime >= mapUntil)
            {
                mapUntil = -1f;
                if (!mapOpen) mapFloor = 0;
            }
            // a boss fight has the panel to itself
            var show = (mapOpen || mapUntil > 0f) && !fighting;
            var shownNow = map.Root.style.display == DisplayStyle.Flex;
            if (show != shownNow)
            {
                map.Root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                hud.EnableInClassList("rf-raid--tower-map", show);
                mapButton.EnableInClassList("rf-tower__mapbtn--on", show);
                if (show) map.Forget();
            }
            if (!show) return;
            var floor = mapFloor;
            if (floor <= 0)
            {
                var player = tower.Watched;
                var town = player && player.towerHandler != null && player.towerHandler.InTown && TowerTown.Loaded ? TowerTown.Loaded.Town : null;
                floor = watched != null ? TowerWords.Floor(watched.Step) : town != null ? town.Band : TowerWords.Floor(tower.StreamFloor);
            }
            map.Refresh(tower, Mathf.Clamp(floor, 1, TowerWords.Floor(tower.StreamFloor)));
        }

        private void Refresh()
        {
            var tower = TowerManager.Instance;
            var onParts = tower ? tower.FightingPlayers().Count : 0;
            var inTown = tower ? tower.TownPlayers().Count : 0;
            var active = tower && tower.State != null && (onParts + inTown > 0 || tower.Phase == TowerManager.TowerPhase.Countdown || tower.Phase == TowerManager.TowerPhase.Fight);
            if (active != shown)
            {
                shown = active;
                Show(hud, active);
                // the column back in the middle of the screen when the panel goes
                if (!active) TopColumnFit.Reset(column);
            }
            if (!active) return;

            var phaseNow = tower.Phase;
            var fighting = phaseNow == TowerManager.TowerPhase.Fight;
            var countdown = phaseNow == TowerManager.TowerPhase.Countdown;
            var stream = Tower.Get(tower.StreamFloor);
            var here = tower.Definition ?? stream;
            // the part the watched player is on: a cleared part they train on by themselves is shown in place of the climb
            // (with the tower's group camera nobody is watched: the part the camera stands at)
            var watched = tower.AreaOf(tower.Watched) ?? tower.CameraArea;
            var trip = watched != null && !tower.IsClimb(watched);
            // the streamer's buttons: challenge the guardian when it is open, start the fight during its countdown
            // (not while its intro plays); on the climb's view only
            SetAction(trip ? 0
                : countdown ? (tower.PhaseSecondsLeft > 0.5f && !TowerCutscene.Playing ? 2 : 0)
                : !fighting && tower.State.BossOpen && !tower.AllCleared && tower.RetrySecondsLeft <= 0f && tower.PushArea != null && tower.CountOn(tower.PushArea) > 0 ? 1 : 0);
            // the part's meters: on a part, and not during a boss fight (the fight opens them itself)
            Show(metersButton, watched != null && !fighting);
            metersButton.EnableInClassList("rf-tower__mapbtn--on", DungeonMetersWindow.StretchOpen);
            // the camera button says where its press goes: out to the world from the tower's group camera (lit while
            // that is on), else to the group camera (from the world, or from a player followed in the tower)
            var groupCamera = WorldTowerCamera.Group;
            Set(cameraText, string.Format(groupCamera ? Localization.UI_TOWER_CAMERA_OUT : Localization.UI_TOWER_CAMERA_IN, Keybinds.Label(Keybinds.Action.TowerCamera)));
            cameraButton.EnableInClassList("rf-tower__mapbtn--on", groupCamera);
            var onClimb = tower.Players().Count;
            // everyone between parts: the town, and the part that waits
            var resting = !trip && onClimb == 0 && inTown > 0 && !fighting && !countdown;

            RefreshMap(tower, watched, fighting && !trip);

            // never under a window at the screen's side (the player details covered the panel's right edge): the
            // column stands in the room between them, and the panel is no wider than that room
            var free = TopColumnFit.Apply(column);
            if (free < MaxWidth)
            {
                hud.style.maxWidth = free;
                hud.style.minWidth = Mathf.Min(map != null && map.Root.style.display == DisplayStyle.Flex ? MapMinWidth : MinWidth, free);
            }
            else
            {
                hud.style.maxWidth = StyleKeyword.Null;
                hud.style.minWidth = StyleKeyword.Null;
            }
            SetCompact(free < CompactWidth, fighting || countdown);

            // a vote that is open: what it is for and when it passes, and the command that adds a voice
            string voteText = null, voteCommand = null;
            if (!fighting && !countdown && tower.TryGetOpenVote(out var voteKey, out var voteSeconds, out var voteMissing, out _))
            {
                if (voteKey == "boss") { voteText = string.Format(Localization.UI_TOWER_VOTE_BOSS, voteSeconds); voteCommand = "!tower boss"; }
                else if (voteKey == "climb") { voteText = string.Format(Localization.UI_TOWER_VOTE_CLIMB, voteSeconds); voteCommand = "!tower climb"; }
                else if (voteKey == "close") { voteText = string.Format(Localization.UI_TOWER_VOTE_CLOSE, voteSeconds, voteMissing); voteCommand = "!tower close"; }
                else if (voteKey.StartsWith("part ") && int.TryParse(voteKey.Substring(5), NumberStyles.Integer, CultureInfo.InvariantCulture, out var voteStep))
                {
                    voteText = string.Format(Localization.UI_TOWER_VOTE_FLOOR, voteSeconds, TowerWords.Floor(voteStep), TowerWords.Part(voteStep));
                    voteCommand = "!tower all " + Tower.Short(voteStep);
                }
            }

            Set(title, Localization.UI_TOWER_TITLE);
            // lost and regrouping: the guardian still stands, and the panel says so in the fight's red
            var loss = !trip && !fighting && !countdown && tower.LastLoss != null && tower.LastLoss.Step == tower.StreamFloor && tower.RetrySecondsLeft > 0f ? tower.LastLoss : null;
            hud.EnableInClassList("rf-raid--tower-fight", (fighting && !trip) || loss != null);

            if (trip)
            {
                // a cleared part someone trains on: its name, who is there, and the way to the climb; a boss attempt
                // called on the climb's part is said here too, so the players here hear of it
                var part = watched.Definition;
                Set(floorLabel, part != null ? string.Format(Localization.UI_TOWER_FLOOR, part.Floor, part.Part, part.Name) : "");
                Set(people, tower.CountOn(watched).ToString(CultureInfo.InvariantCulture));
                Show(bar, false);
                Show(quests, false);
                Show(liveDot, countdown);
                Show(clock, countdown);
                Set(timer, countdown ? Clock(tower.PhaseSecondsLeft) : "");
                ShowJoin(true);
                var route = watched.Floor ? watched.Floor.Route : null;
                var rooms = route && route.Valid ? route.FightingRooms : 0;
                if (countdown)
                {
                    Set(phase, string.Format(Localization.UI_TOWER_TRIP_BOSS, TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor)));
                    Set(join, "!tower");
                }
                else if (voteText != null)
                {
                    Set(phase, voteText);
                    Set(join, voteCommand);
                }
                else
                {
                    // a part of rooms: how far along its route they are, until the last room is reached
                    Set(phase, rooms > 1 && route.Active < route.Hold
                        ? string.Format(Localization.UI_TOWER_ROOM, Mathf.Min(route.Active + 1, rooms), rooms)
                        : string.Format(Localization.UI_TOWER_TRAINING, TowerWords.Floor(tower.StreamFloor), TowerWords.Part(tower.StreamFloor)));
                    // with one part open at a time (potato mode) the way back is everyone's
                    Set(join, tower.AreaLimit <= 1 ? "!tower all" : "!tower");
                }
                return;
            }

            Set(people, (onParts + inTown).ToString(CultureInfo.InvariantCulture));

            if (resting)
            {
                var town = tower.CurrentTown;
                Set(floorLabel, town != null ? town.Name : "");
                Show(bar, false);
                Show(quests, false);
                Show(liveDot, false);
                Show(clock, false);
                Set(timer, "");
                if (voteText != null)
                {
                    Set(phase, voteText);
                    ShowJoin(true);
                    Set(join, voteCommand);
                }
                else if (tower.AllCleared || stream == null)
                {
                    Set(phase, Localization.UI_TOWER_ALL_CLEARED);
                    ShowJoin(false);
                }
                else
                {
                    Set(phase, string.Format(Localization.UI_TOWER_NEXT_FLOOR, stream.Floor, stream.Part, stream.Name));
                    ShowJoin(true);
                    Set(join, "!tower climb");
                }
                return;
            }

            Set(floorLabel, here != null ? string.Format(Localization.UI_TOWER_FLOOR, here.Floor, here.Part, here.Name) : "");
            Show(liveDot, fighting || countdown);
            Show(clock, fighting || countdown);
            Set(timer, fighting || countdown ? Clock(tower.PhaseSecondsLeft) : "");

            if (fighting)
            {
                // the boss's health in the bar, as in a raid
                var boss = tower.PushFloor ? tower.PushFloor.Boss : null;
                var max = boss && boss.Stats != null ? boss.Stats.Health.MaxValue : 0;
                var hp = boss && boss.Stats != null ? Mathf.Max(0, boss.Stats.Health.CurrentValue) : 0;
                Show(bar, max > 0);
                Show(quests, false);
                SetFill(max > 0 ? hp / (float)max : 0f);
                Set(barText, HudFormat.Grouped(hp) + " / " + HudFormat.Grouped(max));
                Set(phase, here != null && here.Boss != null ? here.Boss.Name : "");
                ShowJoin(false);
                // who still stands: the knocked out sit the rest of the fight out
                if (tower.FightCount(out var standing, out var went))
                    Set(people, standing >= went ? string.Format(Localization.UI_TOWER_STANDING, standing) : string.Format(Localization.UI_TOWER_STANDING_RECOVERING, standing, went - standing));
                return;
            }

            // the part's objectives: one row each (kills, gathering, hidden things), then the guardian that waits behind
            // them; the bar is how far they are together, each counting the same whatever its numbers
            var done = 0;
            var total = 0;
            var share = 0f;
            var rows = 0;
            if (stream != null && !tower.AllCleared)
            {
                foreach (var q in stream.Objectives)
                {
                    if (rows >= questRows.Count) break;
                    var row = questRows[rows++];
                    Show(row.row, true);
                    if (!q.Counted)
                    {
                        // the guardian: named, with no count
                        Set(row.text, string.Format(stream.IsFloorBoss ? Localization.UI_TOWER_ROW_FLOOR_BOSS : Localization.UI_TOWER_ROW_MINI_BOSS, stream.Boss.Name));
                        Set(row.count, "");
                        row.row.EnableInClassList("rf-tower__quest--done", false);
                        row.row.EnableInClassList("rf-tower__quest--guardian", true);
                        continue;
                    }
                    var count = Mathf.Clamp(tower.Progress(q.Id), 0, q.Amount);
                    total++;
                    if (count >= q.Amount) done++;
                    share += q.Amount > 0 ? count / (float)q.Amount : 1f;
                    Set(row.text, q.Text);
                    Set(row.count, HudFormat.Grouped(count) + " / " + HudFormat.Grouped(q.Amount));
                    row.row.EnableInClassList("rf-tower__quest--done", count >= q.Amount);
                    row.row.EnableInClassList("rf-tower__quest--guardian", false);
                }
            }
            for (var i = rows; i < questRows.Count; i++) Show(questRows[i].row, false);
            // the rows while there is something left to do; all done, the bar and the boss line say it. With the camera
            // out of the tower (the world side, a raid's or a dungeon's camera) the bar alone: the full list stood under
            // the raid's or dungeon's panel over the middle of the picture, on the fight it watches
            Show(quests, rows > 0 && done < total && tower.CameraInTower);
            Show(bar, total > 0);
            if (loss != null)
            {
                // what the guardian had left, and why the attempt ended ("Objectives 2 / 2" beside a regroup timer read as a win)
                SetFill(loss.PercentLeft / 100f);
                Set(barText, string.Format(Localization.UI_TOWER_LOST_BAR, loss.Reason, loss.Boss, loss.PercentLeft));
            }
            else
            {
                SetFill(total > 0 ? share / total : 0f);
                Set(barText, total > 0 && done >= total ? Localization.UI_TOWER_QUESTS_DONE : string.Format(Localization.UI_TOWER_QUESTS, done, total));
            }

            var bossName = here != null && here.Boss != null ? here.Boss.Name : "";
            if (countdown)
            {
                Set(phase, string.Format(Localization.UI_TOWER_BOSS_STARTS, bossName));
                ShowJoin(true);
                Set(join, "!tower");
            }
            else if (voteText != null)
            {
                Set(phase, voteText);
                ShowJoin(true);
                Set(join, voteCommand);
            }
            else if (tower.AllCleared)
            {
                Set(phase, Localization.UI_TOWER_ALL_CLEARED);
                ShowJoin(false);
            }
            else if (tower.State.BossOpen)
            {
                var wait = tower.RetrySecondsLeft;
                Set(phase, wait > 0f ? (loss != null ? string.Format(Localization.UI_TOWER_STILL_STANDS, loss.Boss, Clock(wait)) : string.Format(Localization.UI_TOWER_REGROUP, Mathf.CeilToInt(wait)))
                    : string.Format(here != null && here.IsFloorBoss ? Localization.UI_TOWER_FLOOR_BOSS_READY : Localization.UI_TOWER_BOSS_READY, bossName));
                // no "!tower boss" while the group regroups: it cannot be called yet
                ShowJoin(wait <= 0f);
                Set(join, "!tower boss");
            }
            else
            {
                // a floor of rooms: how far along the route the group is
                var route = tower.PushFloor ? tower.PushFloor.Route : null;
                var rooms = route && route.Valid ? route.FightingRooms : 0;
                Set(phase, rooms > 1 ? string.Format(Localization.UI_TOWER_ROOM, Mathf.Min(route.Active + 1, rooms), rooms) : "");
                ShowJoin(true);
                Set(join, "!tower");
            }
        }
    }
}
