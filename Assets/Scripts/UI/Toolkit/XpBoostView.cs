using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The XP boost pill at the top of the status column: multiplier, time left, a thin drain bar,
    /// and who to thank for it.
    /// </summary>
    /// <remarks>
    /// Driven through BATimer, which GameManager updates twice a second. Like RaidHudView, calls
    /// that arrive before the UIDocument has built its tree are kept and applied on bind, and labels
    /// are only rewritten when what they show changes.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class XpBoostView : MonoBehaviour
    {
        private VisualElement pill;
        private Label label;
        private Label time;
        private VisualElement fill;
        private Label thanks;
        private bool bound;

        private bool active;
        private float multiplier;
        private float secondsLeft;
        private float secondsTotal;
        private string subscriber;
        private bool byUser;

        private float shownMultiplier = float.NaN;
        private int shownSeconds = int.MinValue;
        private float shownFill = -1f;
        private string shownThanks;

        private void OnEnable()
        {
            TryBind();
        }

        private void OnDisable()
        {
            bound = false;
        }

        private void Update()
        {
            if (!bound)
            {
                TryBind();
            }
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

            pill = root.Q<VisualElement>("xp-boost");
            label = root.Q<Label>("xp-boost-label");
            time = root.Q<Label>("xp-boost-time");
            fill = root.Q<VisualElement>("xp-boost-fill");
            thanks = root.Q<Label>("xp-boost-thanks");

            if (pill == null)
            {
                return;
            }

            bound = true;

            shownMultiplier = float.NaN;
            shownSeconds = int.MinValue;
            shownFill = -1f;
            shownThanks = null;

            Apply();
        }

        // ---- Called by BATimer -----------------------------------------------------------

        public void SetActive(bool value)
        {
            if (active == value)
            {
                return;
            }

            active = value;
            if (bound)
            {
                Apply();
            }
        }

        public void SetBoost(float value, float left, float total)
        {
            multiplier = value;
            secondsLeft = left;
            secondsTotal = total;
            if (bound)
            {
                Apply();
            }
        }

        /// <param name="name">Who started the boost, or the name of the event that did.</param>
        /// <param name="isUser">True for a viewer, who gets thanked by name; false for an event name.</param>
        public void SetSubscriber(string name, bool isUser)
        {
            subscriber = name;
            byUser = isUser;
            if (bound)
            {
                Apply();
            }
        }

        // ---- Applying state ------------------------------------------------------------------

        private void Apply()
        {
            var display = active ? DisplayStyle.Flex : DisplayStyle.None;
            pill.style.display = display;

            if (!active)
            {
                if (thanks != null) thanks.style.display = DisplayStyle.None;
                return;
            }

            if (label != null && multiplier != shownMultiplier)
            {
                shownMultiplier = multiplier;
                label.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_XP_BOOST,
                    multiplier.ToString("0.##", CultureInfo.InvariantCulture));
            }

            var whole = Mathf.Max(0, Mathf.CeilToInt(secondsLeft));
            if (time != null && whole != shownSeconds)
            {
                shownSeconds = whole;
                time.text = HudFormat.Duration(whole);
            }

            var fraction = secondsTotal > 0f ? Mathf.Clamp01(secondsLeft / secondsTotal) : 0f;
            if (fill != null && Mathf.Abs(fraction - shownFill) >= 0.001f)
            {
                shownFill = fraction;
                fill.style.width = new Length(fraction * 100f, LengthUnit.Percent);
            }

            if (thanks != null)
            {
                var text = string.IsNullOrEmpty(subscriber)
                    ? null
                    : byUser
                        ? string.Format(CultureInfo.InvariantCulture, Localization.UI_XP_BOOST_THANKS, subscriber)
                        : subscriber;

                if (text != shownThanks)
                {
                    shownThanks = text;
                    thanks.text = text ?? string.Empty;
                }

                thanks.style.display = text != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
