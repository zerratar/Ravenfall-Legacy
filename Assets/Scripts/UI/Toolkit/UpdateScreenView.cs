using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// UI Toolkit implementation of the launcher and updater screen.
    ///
    /// <para>
    /// First screen migrated off uGUI. It was picked because it is the smallest and most isolated
    /// one in the project, so the real cost per screen could be learned somewhere cheap before
    /// committing to the larger panels. See docs/ui-toolkit-migration.md.
    /// </para>
    ///
    /// <para>
    /// It deliberately exposes the same outputs GameUpdater already drives - a status line, a
    /// version string and a progress value - so the updater does not care which UI is present.
    /// </para>
    ///
    /// <para>
    /// Values set here are held until the visual tree exists and then applied. Unity runs every
    /// Awake before any OnEnable, and UIDocument builds its tree in OnEnable, so GameUpdater
    /// setting the version from Awake would otherwise write into nothing and the label would stay
    /// at its placeholder with no error to show for it.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UpdateScreenView : MonoBehaviour, IUpdateScreen
    {
        private Label statusLabel;
        private Label versionLabel;
        private VisualElement progressRoot;
        private VisualElement progressFill;
        private Label progressLabel;

        private string pendingStatus;
        private string pendingVersion;
        private bool? pendingProgressVisible;
        private float? pendingProgress;

        private bool bound;

        private void OnEnable()
        {
            TryBind();
        }

        private void Update()
        {
            if (!bound)
            {
                TryBind();
            }
        }

        private void OnDisable()
        {
            bound = false;
        }

        private void TryBind()
        {
            if (bound)
            {
                return;
            }

            var doc = GetComponent<UIDocument>();
            var root = doc != null ? doc.rootVisualElement : null;
            if (root == null)
            {
                return;
            }

            statusLabel = root.Q<Label>("status-label");
            versionLabel = root.Q<Label>("version-label");
            progressRoot = root.Q<VisualElement>("progress-root");
            progressFill = root.Q<VisualElement>("progress-fill");
            progressLabel = root.Q<Label>("progress-label");
            bound = true;

            if (pendingStatus != null && statusLabel != null) statusLabel.text = pendingStatus;
            if (pendingVersion != null && versionLabel != null) versionLabel.text = pendingVersion;
            if (pendingProgressVisible.HasValue) ApplyProgressVisible(pendingProgressVisible.Value);
            if (pendingProgress.HasValue) ApplyProgress(pendingProgress.Value);
        }

        private void ApplyProgressVisible(bool value)
        {
            if (progressRoot != null)
            {
                progressRoot.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void ApplyProgress(float value)
        {
            var clamped = Mathf.Clamp01(value);
            if (progressFill != null)
            {
                progressFill.style.width = Length.Percent(clamped * 100f);
            }
            if (progressLabel != null)
            {
                progressLabel.text = Mathf.RoundToInt(clamped * 100f) + "%";
            }
        }

        /// <summary>The status line, from "Checking for updates." through to any error text.</summary>
        public string Status
        {
            set
            {
                pendingStatus = value;
                TryBind();
                if (bound && statusLabel != null) statusLabel.text = value;
            }
        }

        /// <summary>Version string shown under the logo.</summary>
        public string Version
        {
            set
            {
                pendingVersion = value;
                TryBind();
                if (bound && versionLabel != null) versionLabel.text = value;
            }
        }

        /// <summary>
        /// Whether the progress bar is shown at all. Hidden by default: a bar sitting at zero reads
        /// as stuck rather than idle.
        /// </summary>
        public bool ProgressVisible
        {
            set
            {
                pendingProgressVisible = value;
                TryBind();
                if (bound) ApplyProgressVisible(value);
            }
        }

        /// <summary>Progress in the range 0 to 1.</summary>
        public float Progress
        {
            set
            {
                pendingProgress = value;
                TryBind();
                if (bound) ApplyProgress(value);
            }
        }
    }
}
