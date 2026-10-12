using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// One scale for every piece of 2D interface: the UI Toolkit HUD and the legacy uGUI canvases.
    /// </summary>
    /// <remarks>
    /// Streams run at very different sizes, from 720p captures where the HUD needs to be larger to be
    /// read at all, to 1440p screens where the default leaves too much of the world covered. Before
    /// this the only size control was a player list slider.
    ///
    /// <para>
    /// UI Toolkit panels are scaled through <see cref="PanelSettings.scale"/>. Screen space canvases
    /// that scale with screen size get a smaller reference resolution, which enlarges them by the
    /// same factor; constant pixel size canvases get their scale factor multiplied. World space
    /// canvases, like name tags, are left alone. The original values are remembered the first time
    /// each is seen, so the scale is always applied to the original rather than compounding.
    /// </para>
    ///
    /// <para>
    /// The panel settings are an asset, so in the editor a change made in Play mode would otherwise
    /// be saved into it. The original scale is put back when the application quits, which in the
    /// editor is when Play mode ends.
    /// </para>
    /// </remarks>
    public static class UiScale
    {
        public const float Min = 0.6f;
        public const float Max = 1.6f;
        public const float Step = 0.05f;

        private static readonly Dictionary<PanelSettings, float> panelBase = new Dictionary<PanelSettings, float>();
        private static readonly Dictionary<CanvasScaler, Vector2> referenceBase = new Dictionary<CanvasScaler, Vector2>();
        private static readonly Dictionary<CanvasScaler, float> factorBase = new Dictionary<CanvasScaler, float>();

        /// <summary>The current scale, 1 for the default size.</summary>
        public static float Value { get; private set; } = 1f;

        /// <summary>Raised after the scale changes, with the new value.</summary>
        public static event Action<float> Changed;

        /// <summary>Applies the saved scale. Called once the scene and settings are loaded.</summary>
        public static void Load()
        {
            Set(PlayerSettings.Instance.UIScale.GetValueOrDefault(1f), save: false);
        }

        public static void Increase() => Set(Value + Step);

        public static void Decrease() => Set(Value - Step);

        /// <param name="value">Clamped to <see cref="Min"/>..<see cref="Max"/> and snapped to <see cref="Step"/>.</param>
        /// <param name="save">Whether to store the value in the player settings.</param>
        public static void Set(float value, bool save = true)
        {
            value = Mathf.Round(Mathf.Clamp(value, Min, Max) / Step) * Step;
            Value = value;
            Apply();

            if (save)
            {
                PlayerSettings.Instance.UIScale = value;
                PlayerSettings.Save();
            }

            Changed?.Invoke(value);
        }

        /// <summary>
        /// Applies the current scale to every panel and canvas in the loaded scenes. Safe to call
        /// again after new UI is loaded.
        /// </summary>
        public static void Apply()
        {
            foreach (var doc in UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var settings = doc.panelSettings;
                if (!settings)
                {
                    continue;
                }

                if (!panelBase.TryGetValue(settings, out var baseScale))
                {
                    baseScale = settings.scale;
                    panelBase[settings] = baseScale;
                }

                settings.scale = baseScale * Value;
            }

            foreach (var scaler in UnityEngine.Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var canvas = scaler.GetComponent<Canvas>();
                if (!canvas || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
                {
                    continue;
                }

                if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    if (!referenceBase.TryGetValue(scaler, out var reference))
                    {
                        reference = scaler.referenceResolution;
                        referenceBase[scaler] = reference;
                    }

                    scaler.referenceResolution = reference / Value;
                }
                else if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
                {
                    if (!factorBase.TryGetValue(scaler, out var factor))
                    {
                        factor = scaler.scaleFactor;
                        factorBase[scaler] = factor;
                    }

                    scaler.scaleFactor = factor * Value;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            panelBase.Clear();
            referenceBase.Clear();
            factorBase.Clear();
            Value = 1f;
            Application.quitting -= RestoreAssets;
            Application.quitting += RestoreAssets;
        }

        /// <summary>Puts the panel settings assets back to their original scale.</summary>
        private static void RestoreAssets()
        {
            foreach (var pair in panelBase)
            {
                if (pair.Key)
                {
                    pair.Key.scale = pair.Value;
                }
            }
        }
    }
}
