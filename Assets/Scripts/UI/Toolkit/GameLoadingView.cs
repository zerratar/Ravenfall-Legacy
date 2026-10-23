using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The few seconds between signing in and the game being ready: the session starting, the
    /// game server connecting, items and the village arriving. Until then the village bar is empty
    /// and the camera does not move, which looked like the game had frozen after login. A card in
    /// the middle of the screen lists the steps, ticks them off as they finish and fades away.
    /// </summary>
    /// <remarks>
    /// Each step reads the same state the rest of the game waits on, so the list cannot say ready
    /// while something still waits. A step that takes unusually long gets a note saying so.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class GameLoadingView : MonoBehaviour
    {
        [Tooltip("Seconds between checks.")]
        [SerializeField] private float refreshInterval = 0.1f;
        [Tooltip("Seconds on one step before the card says it is taking longer than usual.")]
        [SerializeField] private float slowSeconds = 20f;
        [Tooltip("Seconds the finished list stays up before it fades.")]
        [SerializeField] private float lingerSeconds = 0.6f;
        [Tooltip("Degrees per second the ring turns.")]
        [SerializeField] private float spinnerSpeed = 360f;

        private enum Step { Session, Server, Items, Village }
        private static readonly Step[] Steps = { Step.Session, Step.Server, Step.Items, Step.Village };

        private VisualElement root;
        private VisualElement card;
        private VisualElement spinner;
        private Label title;
        private Label slow;
        private readonly VisualElement[] rows = new VisualElement[4];
        private readonly Label[] marks = new Label[4];
        private readonly Label[] texts = new Label[4];

        private bool bound;
        private bool shown;
        private bool finished;
        private float nextRefresh;
        private float doneAt = -1f;
        private int currentStep = -1;
        private float stepSince;
        private float spin;
        private bool wasAuthenticated;

        private void OnEnable()
        {
            Languages.Changed += ApplyTexts;
            TryBind();
        }

        private void OnDisable()
        {
            Languages.Changed -= ApplyTexts;
            bound = false;
        }

        private void Update()
        {
            if (bound && card.panel == null) bound = false;
            if (!bound)
            {
                TryBind();
                if (!bound) return;
            }

            if (shown && spinner != null)
            {
                spin = (spin + spinnerSpeed * Time.unscaledDeltaTime) % 360f;
                spinner.style.rotate = new Rotate(spin);
            }

            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + refreshInterval;
            Refresh();
        }

        private void Refresh()
        {
            var gm = GameManager.Instance;
            var nest = gm != null ? gm.RavenNest : null;
            var authenticated = nest != null && nest.Authenticated;

            // a log out and a new sign in: the list starts over
            if (!authenticated)
            {
                wasAuthenticated = false;
                finished = false;
                doneAt = -1f;
                SetShown(false);
                return;
            }

            if (!wasAuthenticated)
            {
                wasAuthenticated = true;
                currentStep = -1;
            }

            // an out of date client never gets a session: the update notice says so instead
            if (finished || nest.BadClientVersion)
            {
                SetShown(false);
                return;
            }

            var first = -1;
            for (var i = 0; i < Steps.Length; ++i)
            {
                var done = IsDone(Steps[i], gm, nest);
                ApplyRow(i, done ? RowState.Done : first < 0 ? RowState.Active : RowState.Waiting);
                if (!done && first < 0) first = i;
            }

            if (first != currentStep)
            {
                currentStep = first;
                stepSince = Time.unscaledTime;
            }

            if (first < 0)
            {
                // all done: a moment with every step ticked, then away
                if (doneAt < 0f) doneAt = Time.unscaledTime;
                card.AddToClassList("rf-loading--done");
                if (title != null) title.text = Localization.UI_LOADING_READY;
                if (slow != null) slow.style.display = DisplayStyle.None;
                if (Time.unscaledTime - doneAt >= lingerSeconds)
                {
                    finished = true;
                    SetShown(false);
                }
                return;
            }

            doneAt = -1f;
            card.RemoveFromClassList("rf-loading--done");
            if (title != null) title.text = Localization.UI_LOADING_TITLE;
            if (slow != null)
            {
                slow.style.display = Time.unscaledTime - stepSince >= slowSeconds ? DisplayStyle.Flex : DisplayStyle.None;
            }
            SetShown(true);
        }

        private static bool IsDone(Step step, GameManager gm, RavenNest.SDK.RavenNestClient nest)
        {
            switch (step)
            {
                case Step.Session: return nest.SessionStarted;
                // ServerReady is the v2 connection, or the old event socket; on v2 the old socket never connects
                case Step.Server: return nest.SessionStarted && nest.ServerReady;
                case Step.Items: return gm.IsLoaded;
                case Step.Village: return gm.Village != null && gm.Village.IsLoaded;
                default: return true;
            }
        }

        private enum RowState { Waiting, Active, Done }

        private void ApplyRow(int i, RowState state)
        {
            var row = rows[i];
            if (row == null) return;
            row.EnableInClassList("rf-loading__step--done", state == RowState.Done);
            row.EnableInClassList("rf-loading__step--active", state == RowState.Active);
            // a tick when done, a dot while it waits; the active one shows the ring beside it
            if (marks[i] != null) marks[i].text = state == RowState.Done ? "\uf00c" : state == RowState.Active ? "\uf141" : "\uf111";
        }

        private void SetShown(bool value)
        {
            if (shown == value) return;
            shown = value;
            card.EnableInClassList("rf-loading--shown", value);
            // opacity transitions in and out; picking stays off either way, it never blocks a click
        }

        private void TryBind()
        {
            if (bound) return;
            var doc = GetComponent<UIDocument>();
            root = doc != null ? doc.rootVisualElement : null;
            card = root?.Q<VisualElement>("loading-card");
            if (card == null) return;

            spinner = root.Q<VisualElement>("loading-spinner");
            title = root.Q<Label>("loading-title");
            slow = root.Q<Label>("loading-slow");
            for (var i = 0; i < Steps.Length; ++i)
            {
                rows[i] = root.Q<VisualElement>("loading-step-" + i);
                marks[i] = rows[i]?.Q<Label>(className: "rf-loading__mark");
                texts[i] = rows[i]?.Q<Label>(className: "rf-loading__text");
            }

            bound = true;
            shown = false;
            card.RemoveFromClassList("rf-loading--shown");
            ApplyTexts();
            nextRefresh = 0f;
        }

        private void ApplyTexts()
        {
            if (!bound) return;
            if (title != null) title.text = Localization.UI_LOADING_TITLE;
            if (slow != null) slow.text = Localization.UI_LOADING_SLOW;
            var names = new[] { Localization.UI_LOADING_SESSION, Localization.UI_LOADING_SERVER, Localization.UI_LOADING_ITEMS, Localization.UI_LOADING_VILLAGE };
            for (var i = 0; i < texts.Length; ++i)
            {
                if (texts[i] != null) texts[i].text = names[i];
            }
        }
    }
}
