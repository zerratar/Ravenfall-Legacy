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
    /// It deliberately exposes the same three outputs GameUpdater already drives - a status line, a
    /// version string and a progress value - so the updater does not care which UI is present.
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

        private bool queried;

        private void OnEnable()
        {
            Query();
        }

        private void Query()
        {
            if (queried)
            {
                return;
            }

            var doc = GetComponent<UIDocument>();
            if (doc == null || doc.rootVisualElement == null)
            {
                // rootVisualElement is null until the document has been laid out; try again next
                // time something is set on us.
                return;
            }

            var root = doc.rootVisualElement;
            statusLabel = root.Q<Label>("status-label");
            versionLabel = root.Q<Label>("version-label");
            progressRoot = root.Q<VisualElement>("progress-root");
            progressFill = root.Q<VisualElement>("progress-fill");
            progressLabel = root.Q<Label>("progress-label");
            queried = true;
        }

        /// <summary>The status line, from "Checking for updates." through to any error text.</summary>
        public string Status
        {
            set
            {
                Query();
                if (statusLabel != null)
                {
                    statusLabel.text = value;
                }
            }
        }

        /// <summary>Version string shown under the title.</summary>
        public string Version
        {
            set
            {
                Query();
                if (versionLabel != null)
                {
                    versionLabel.text = value;
                }
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
                Query();
                if (progressRoot != null)
                {
                    progressRoot.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        /// <summary>Progress in the range 0 to 1.</summary>
        public float Progress
        {
            set
            {
                Query();
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
        }
    }
}
