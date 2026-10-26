using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The round minimap in hero mode's top right corner (docs/mockups/hero-mode-hud.jpg): an orthographic camera straight
    /// above the hero draws the ground into a small texture a few times a second, north up, and the hero (an arrow), the
    /// viewers on the island (green) and the enemies (red) are dots laid over it, so they move smoothly between pictures.
    /// </summary>
    public sealed class HeroMinimap
    {
        private const int TextureSize = 256;
        private const float Radius = 48f;        // metres from the centre to the edge
        private const float Above = 80f;         // metres the camera hangs over the hero
        private const float RenderEvery = 0.25f;
        private const float FindEvery = 0.5f;

        public VisualElement Root { get; }

        private readonly VisualElement map;
        private readonly VisualElement dots;
        private readonly VisualElement arrow;
        private readonly List<VisualElement> pool = new List<VisualElement>();
        private readonly List<EnemyController> enemies = new List<EnemyController>();
        private Camera cam;
        private RenderTexture texture;
        private float nextRender, nextFind;

        public HeroMinimap()
        {
            Root = new VisualElement { pickingMode = PickingMode.Ignore };
            Root.AddToClassList("rf-hero-minimap");
            map = new VisualElement { pickingMode = PickingMode.Ignore };
            map.AddToClassList("rf-hero-minimap__map");
            dots = new VisualElement { pickingMode = PickingMode.Ignore };
            dots.AddToClassList("rf-hero-minimap__dots");
            map.Add(dots);
            arrow = new Label("") { pickingMode = PickingMode.Ignore }; // location-arrow
            arrow.AddToClassList("rf-icon");
            arrow.AddToClassList("rf-hero-minimap__hero");
            map.Add(arrow);
            Root.Add(map);
            var frame = new VisualElement { pickingMode = PickingMode.Ignore };
            frame.AddToClassList("rf-hero-minimap__frame");
            var art = Resources.Load<Texture2D>("HeroUI/minimap-frame");
            if (art) frame.style.backgroundImage = new StyleBackground(art);
            Root.Add(frame);
        }

        public void Update(PlayerController hero)
        {
            if (!hero) return;
            EnsureCamera();
            var centre = hero.transform.position;

            if (Time.unscaledTime >= nextRender)
            {
                nextRender = Time.unscaledTime + RenderEvery;
                cam.transform.SetPositionAndRotation(new Vector3(centre.x, centre.y + Above, centre.z), Quaternion.Euler(90f, 0f, 0f));
                // the world's haze is for the view across the island; from straight above it only washes the map out
                var fog = RenderSettings.fog;
                RenderSettings.fog = false;
                cam.Render();
                RenderSettings.fog = fog;
            }

            if (Time.unscaledTime >= nextFind)
            {
                nextFind = Time.unscaledTime + FindEvery;
                enemies.Clear();
                foreach (var e in Object.FindObjectsByType<EnemyController>())
                {
                    if (e && e.gameObject.activeInHierarchy && !e.Stats.IsDead && Flat(e.transform.position - centre).sqrMagnitude < Radius * Radius * 1.2f) enemies.Add(e);
                }
            }

            var used = 0;
            foreach (var p in GameManager.Instance.Players.GetAllPlayers())
            {
                if (!p || p == hero || !p.InRavebridge) continue;
                Place(used++, p.transform.position - centre, "rf-hero-minimap__dot--friend");
            }
            foreach (var e in enemies)
            {
                if (!e || e.Stats.IsDead) continue;
                Place(used++, e.transform.position - centre, "rf-hero-minimap__dot--enemy");
            }
            // the people with something to say or a quest to give, gold like the marks over their heads
            if (IslandNpc.MarkFor != null)
            {
                foreach (var n in IslandNpc.All)
                {
                    if (!n || IslandNpc.MarkFor(n.Key) == IslandNpc.Mark.None) continue;
                    Place(used++, n.transform.position - centre, "rf-hero-minimap__dot--quest");
                }
            }
            for (var i = used; i < pool.Count; i++) pool[i].style.display = DisplayStyle.None;

            // the hero in the middle, turned the way they face (the glyph points up and to the right)
            arrow.style.rotate = new Rotate(hero.transform.eulerAngles.y - 45f);
        }

        /// <summary>A dot at a place relative to the hero, held on the rim when it is past the edge.</summary>
        private void Place(int index, Vector3 offset, string kind)
        {
            while (pool.Count <= index)
            {
                var d = new VisualElement { pickingMode = PickingMode.Ignore };
                d.AddToClassList("rf-hero-minimap__dot");
                dots.Add(d);
                pool.Add(d);
            }
            var dot = pool[index];
            dot.style.display = DisplayStyle.Flex;
            dot.EnableInClassList("rf-hero-minimap__dot--friend", kind == "rf-hero-minimap__dot--friend");
            dot.EnableInClassList("rf-hero-minimap__dot--enemy", kind == "rf-hero-minimap__dot--enemy");
            dot.EnableInClassList("rf-hero-minimap__dot--quest", kind == "rf-hero-minimap__dot--quest");
            var v = new Vector2(offset.x, offset.z) / Radius;
            if (v.sqrMagnitude > 0.92f * 0.92f) v = v.normalized * 0.92f;
            dot.style.left = Length.Percent(50f + v.x * 50f);
            dot.style.top = Length.Percent(50f - v.y * 50f);
        }

        private void EnsureCamera()
        {
            if (cam) return;
            texture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32) { name = "Hero Minimap" };
            var go = new GameObject("Hero Minimap Camera") { hideFlags = HideFlags.DontSave };
            cam = go.AddComponent<Camera>();
            cam.enabled = false; // drawn by hand, a few times a second
            cam.orthographic = true;
            cam.orthographicSize = Radius;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = Above + 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.36f, 0.48f); // the sea, where nothing is drawn
            cam.cullingMask = ~LayerMask.GetMask("UI");
            cam.targetTexture = texture;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderShadows = false;
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.None;
            map.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(texture));
        }

        public void Dispose()
        {
            if (cam) Object.Destroy(cam.gameObject);
            if (texture) { texture.Release(); Object.Destroy(texture); }
            cam = null;
            texture = null;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
