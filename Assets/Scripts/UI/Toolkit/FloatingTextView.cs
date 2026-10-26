using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Combat numbers over characters: damage dealt, damage taken, healing and misses.
    /// </summary>
    /// <remarks>
    /// Replaces a world space canvas per number, each with a comic "pow" burst behind the text. Here
    /// every number is a pooled label on one screen space layer, projected from the world each
    /// frame, so a raid with a hundred numbers in the air costs a hundred style writes rather than a
    /// hundred canvases.
    ///
    /// <para>
    /// A number says what happened through colour and size rather than decoration:
    /// </para>
    /// <list type="bullet">
    /// <item>White for damage dealt, gold for a hit that stands out.</item>
    /// <item>Red for damage a player takes.</item>
    /// <item>Green with a plus for healing.</item>
    /// <item>A small grey "Miss" for a hit that did nothing, instead of a 0.</item>
    /// </list>
    /// <para>
    /// Size is relative to the biggest recent hit on the same target, so the hits that matter stand
    /// out at any scale: a raid boss taking 449 and 821 reads the same way as a goblin taking 3 and 12.
    /// Each number pops in slightly tilted, settles straight, drifts up on a gentle arc and fades.
    /// </para>
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class FloatingTextView : MonoBehaviour
    {
        [Tooltip("Seconds a number stays on screen.")]
        [SerializeField] private float lifetime = 1.15f;

        [Tooltip("Pixels a number rises over its lifetime.")]
        [SerializeField] private float rise = 70f;

        [Tooltip("Font size for the smallest and the biggest hits.")]
        [SerializeField] private Vector2 fontSizeRange = new Vector2(24f, 50f);

        [Tooltip("Camera distance at which numbers are drawn at full size; further away they shrink.")]
        [SerializeField] private float referenceDistance = 18f;

        [Tooltip("How quickly a target's biggest recent hit is forgotten, per second.")]
        [SerializeField] private float peakDecay = 0.04f;

        private const int MaxActive = 160;

        private enum Kind { Dealt, Taken, Heal, Miss }

        private sealed class Number
        {
            public Label Label;
            public Transform Target;
            public float Height;
            public float Age;
            public float Life;
            public float OffsetX;
            public float DriftX;
            public float Tilt;
            public float Size;
            public int Value;
            public Kind Kind;
            public bool Merged;
            public EntityId TargetId;
        }

        private sealed class TargetInfo
        {
            public float Height;
            public bool IsPlayer;
            public float Peak;
            public float PeakTime;
            public Number Merged;
            public Number Miss;
        }

        private readonly List<Number> active = new List<Number>();
        private readonly Stack<Number> pool = new Stack<Number>();
        private readonly Dictionary<EntityId, TargetInfo> targets = new Dictionary<EntityId, TargetInfo>();

        private VisualElement layer;
        private Camera cam;

        private void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            layer = doc != null ? doc.rootVisualElement?.Q<VisualElement>("floating-layer") : null;
        }

        private void OnDisable()
        {
            for (var i = 0; i < active.Count; ++i)
            {
                active[i].Label.RemoveFromHierarchy();
            }

            active.Clear();
            pool.Clear();
            targets.Clear();
            layer = null;
        }

        /// <summary>
        /// Shows a number over <paramref name="target"/>. Same signature as DamageCounterManager.Add,
        /// which forwards here.
        /// </summary>
        /// <param name="merge">Adds to the number already over the target instead of starting a new
        /// one, as bosses do so a stream of hits reads as one climbing total.</param>
        public void Add(Transform target, int amount, bool isHeal, bool merge)
        {
            if (!target || layer == null)
            {
                return;
            }

            var id = target.GetEntityId();
            var info = GetTarget(target, id);
            amount = Mathf.Abs(amount);

            var kind = isHeal ? Kind.Heal : amount == 0 ? Kind.Miss : info.IsPlayer ? Kind.Taken : Kind.Dealt;

            if (merge && kind == Kind.Dealt && info.Merged != null && info.Merged.Target == target && info.Merged.Age < info.Merged.Life * 0.5f)
            {
                var n = info.Merged;
                n.Value += amount;
                n.Age = 0f;
                n.Size = SizeFor(info, n.Value);
                Write(n);
                return;
            }

            // many players missing the same target read as one Miss, not a pile of them on top of each other
            var miss = info.Miss;
            if (kind == Kind.Miss && miss != null && miss.Kind == Kind.Miss && miss.Target == target && miss.Age < miss.Life * 0.6f)
            {
                miss.Age = 0f;
                return;
            }

            if (active.Count >= MaxActive)
            {
                Release(0);
            }

            var number = Take();
            number.Target = target;
            number.TargetId = id;
            number.Height = info.Height;
            number.Age = 0f;
            number.Life = lifetime * (kind == Kind.Miss ? 0.75f : 1f);
            number.Value = amount;
            number.Kind = kind;
            number.Merged = merge && kind == Kind.Dealt;
            number.OffsetX = Random.Range(-26f, 26f);
            number.DriftX = Random.Range(-34f, 34f);
            number.Tilt = Random.Range(7f, 13f) * (Random.value < 0.5f ? -1f : 1f);
            number.Size = kind == Kind.Miss ? fontSizeRange.x * 0.85f : SizeFor(info, amount);

            Write(number);
            layer.Add(number.Label);
            active.Add(number);

            if (number.Merged)
            {
                info.Merged = number;
            }

            if (kind == Kind.Miss)
            {
                info.Miss = number;
            }
        }

        private TargetInfo GetTarget(Transform target, EntityId id)
        {
            if (!targets.TryGetValue(id, out var info))
            {
                // Enemies come and go all session. Forgetting every target now and then only resets
                // their recent peaks, which rebuild within a few hits.
                if (targets.Count > 2000)
                {
                    targets.Clear();
                    for (var i = 0; i < active.Count; ++i) active[i].Merged = false;
                }

                var collider = target.GetComponent<CapsuleCollider>();
                info = new TargetInfo
                {
                    Height = collider ? collider.height * target.localScale.y : 2.5f,
                    IsPlayer = target.GetComponent<PlayerController>() != null,
                };
                targets[id] = info;
            }

            return info;
        }

        /// <summary>
        /// Size from how this hit compares to the biggest recent one on the same target. The peak
        /// decays, so a target that has only been taking small hits for a while lets them grow again.
        /// </summary>
        private float SizeFor(TargetInfo info, int amount)
        {
            var now = Time.unscaledTime;
            info.Peak *= Mathf.Pow(1f - peakDecay, now - info.PeakTime);
            info.PeakTime = now;
            if (amount > info.Peak)
            {
                info.Peak = amount;
            }

            var relative = info.Peak > 0f ? amount / info.Peak : 1f;
            return Mathf.Lerp(fontSizeRange.x, fontSizeRange.y, Mathf.Sqrt(relative));
        }

        private void Write(Number n)
        {
            var label = n.Label;
            label.text = n.Kind == Kind.Miss ? Localization.UI_FLOAT_MISS
                : n.Kind == Kind.Heal ? "+" + n.Value.ToString(CultureInfo.InvariantCulture)
                : n.Value.ToString(CultureInfo.InvariantCulture);

            label.style.fontSize = n.Size;
            label.EnableInClassList("rf-float--taken", n.Kind == Kind.Taken);
            label.EnableInClassList("rf-float--heal", n.Kind == Kind.Heal);
            label.EnableInClassList("rf-float--miss", n.Kind == Kind.Miss);
            label.EnableInClassList("rf-float--big", n.Kind == Kind.Dealt && n.Size >= Mathf.Lerp(fontSizeRange.x, fontSizeRange.y, 0.85f));
        }

        private Number Take()
        {
            if (pool.Count > 0)
            {
                return pool.Pop();
            }

            var label = new Label { pickingMode = PickingMode.Ignore };
            label.AddToClassList("rf-float");
            return new Number { Label = label };
        }

        private void Release(int index)
        {
            var n = active[index];
            active.RemoveAt(index);
            n.Label.RemoveFromHierarchy();
            n.Target = null;

            if (targets.TryGetValue(n.TargetId, out var info) && info.Merged == n)
            {
                info.Merged = null;
            }

            pool.Push(n);
        }

        private void LateUpdate()
        {
            if (layer == null || active.Count == 0)
            {
                return;
            }

            if (!cam || !cam.isActiveAndEnabled)
            {
                cam = Camera.main;
                if (!cam)
                {
                    return;
                }
            }

            var panel = layer.panel;
            if (panel == null)
            {
                return;
            }

            var dt = Time.unscaledDeltaTime;
            var camPos = cam.transform.position;

            for (var i = active.Count - 1; i >= 0; --i)
            {
                var n = active[i];
                n.Age += dt;
                if (!n.Target || n.Age >= n.Life)
                {
                    Release(i);
                    continue;
                }

                var world = n.Target.position + Vector3.up * n.Height;
                var viewport = cam.WorldToViewportPoint(world);
                if (viewport.z <= 0f)
                {
                    n.Label.style.visibility = Visibility.Hidden;
                    continue;
                }

                n.Label.style.visibility = Visibility.Visible;

                var t = n.Age / n.Life;

                // Pop: starts large and snaps down with a little overshoot.
                var pop = n.Age < 0.14f ? Mathf.LerpUnclamped(1.55f, 1f, EaseOutBack(n.Age / 0.14f)) : 1f;

                // Tilt settles to upright over the first quarter second.
                var tilt = Mathf.Lerp(n.Tilt, 0f, Mathf.Clamp01(n.Age / 0.25f));

                // Rise fast then slow, drifting sideways on an arc.
                var ease = 1f - (1f - t) * (1f - t);
                var x = n.OffsetX + n.DriftX * ease;
                var y = -rise * ease;
                if (n.Kind == Kind.Taken)
                {
                    // Damage taken sinks and slides away instead of rising, so it never reads as a
                    // hit the player landed.
                    y = rise * 0.35f * ease;
                }

                var distance = Vector3.Distance(camPos, world);
                var depth = Mathf.Clamp(referenceDistance / Mathf.Max(distance, 0.01f), 0.55f, 1.15f);

                var point = RuntimePanelUtils.CameraTransformWorldToPanel(panel, world, cam);
                var s = pop * depth;

                n.Label.style.translate = new Translate(point.x + x, point.y + y);
                n.Label.style.scale = new Scale(new Vector2(s, s));
                n.Label.style.rotate = new Rotate(tilt);
                n.Label.style.opacity = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            }
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            x -= 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }
}
