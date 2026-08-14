using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Draws the on screen announcements for arena, raid and dungeon.
    ///
    /// <para>
    /// Replaces seven pre-rendered PNGs that had their English wording painted in. Those could not
    /// be translated, and each one had to be redrawn by hand whenever the style changed, which is
    /// most of why these announcements drifted apart from each other over the years. One template
    /// plus one stylesheet means changing the look changes all of them at once.
    /// </para>
    ///
    /// <para>
    /// Wording comes from <see cref="Localization"/> so the later localization pass has a single
    /// place to work. Chat commands inside a message are wrapped in colour markup so they stand out
    /// the way the red text in the original images did.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class NotificationScreenView : MonoBehaviour
    {
        private VisualElement card;
        private Label title;
        private Label line1;
        private Label line2;
        private bool bound;

        private void OnEnable() => TryBind();

        private void Update()
        {
            if (!bound)
            {
                TryBind();
            }
        }

        private void OnDisable() => bound = false;

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

            card = root.Q<VisualElement>("notify-card");
            title = root.Q<Label>("notify-title");
            line1 = root.Q<Label>("notify-line1");
            line2 = root.Q<Label>("notify-line2");
            bound = true;
            Hide();
        }

        /// <summary>
        /// Shows an announcement. Empty lines are collapsed rather than left as blank space.
        /// </summary>
        /// <param name="titleText">Headline, for example "A boss appeared!!".</param>
        /// <param name="firstLine">Usually the join instruction.</param>
        /// <param name="secondLine">Optional extra line.</param>
        /// <param name="rewardStyle">Colours the headline with the accent, used for wins and draws.</param>
        public void Show(string titleText, string firstLine = null, string secondLine = null, bool rewardStyle = false)
        {
            TryBind();
            if (!bound)
            {
                return;
            }

            SetLine(title, titleText);
            SetLine(line1, firstLine);
            SetLine(line2, secondLine);

            title.EnableInClassList("rf-notify__title--reward", rewardStyle);
            card.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            TryBind();
            if (card != null)
            {
                card.style.display = DisplayStyle.None;
            }
        }

        private static void SetLine(Label label, string value)
        {
            if (label == null)
            {
                return;
            }

            var empty = string.IsNullOrEmpty(value);
            label.text = empty ? string.Empty : value;
            label.style.display = empty ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>
        /// Substitutes a chat command into a message and highlights it, which is what the red text
        /// in the original images was doing. Kept here so every announcement highlights commands
        /// the same way rather than each caller inventing its own markup.
        /// </summary>
        public static string WithCommand(string message, string command)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            var highlighted = "<b><color=#E8A33D>" + command + "</color></b>";
            return message.Replace("{command}", highlighted);
        }
    }
}
