using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// The big map in hero mode (the user, 2026-11-03): M opens and closes it, Esc closes it. The island the hero is on,
    /// seen from straight above (an orthographic camera drawn by hand when the view changes and every few seconds while
    /// open), north up, with the hero (an arrow), the viewers on the island, the people with something to say and where
    /// the hero is walking to laid over it. It opens on the whole island (its land, found once with rays down, not the
    /// island's sphere, which is mostly sea), as big as the screen allows; the mouse wheel zooms in and out round the
    /// pointer, a drag moves the view, and a click walks the hero there (HeroController.WalkToMap), as a click on the
    /// minimap does.
    /// </summary>
    public sealed class HeroBigMap
    {
        private const int TextureSize = 2048;
        private const float Above = 400f;
        private const float RenderEvery = 3f;
        private const float MostZoom = 25f;     // the view's half width at the closest, in metres
        private const float ZoomStep = 1.25f;
        private const float DragPixels = 6f;    // a press that moves further than this is a drag, not a click
        private const int LandSamples = 64;

        private static readonly Dictionary<IslandController, (Vector2 Centre, float Half)> lands = new Dictionary<IslandController, (Vector2, float)>();

        public VisualElement Root { get; }
        public bool IsOpen => Root.style.display == DisplayStyle.Flex;

        private readonly Label title;
        private readonly VisualElement map;
        private readonly VisualElement dots;
        private readonly VisualElement areaLayer;
        private readonly List<VisualElement> areaPool = new List<VisualElement>();
        private readonly VisualElement arrow;
        private readonly VisualElement pin;
        private readonly List<VisualElement> pool = new List<VisualElement>();
        private Camera cam;
        private RenderTexture texture;
        private IslandController island;
        private float height;
        private Vector2 landCentre;
        private float landHalf = 30f;
        private Vector2 view;       // the middle of the view, x and z
        private float half = 30f;   // the view's half width, metres
        private bool dirty;
        private float nextRender;
        private int pressed = -1;
        private Vector2 pressedAt;
        private bool dragging;

        public HeroBigMap()
        {
            Root = new VisualElement { pickingMode = PickingMode.Ignore };
            Root.AddToClassList("rf-hero-bigmap");
            Root.RegisterCallback<GeometryChangedEvent>(_ => Fit());
            var panel = new VisualElement();
            panel.AddToClassList("rf-hero-bigmap__panel");

            var head = new VisualElement { pickingMode = PickingMode.Ignore };
            head.AddToClassList("rf-hero-bigmap__head");
            var icon = new Label("\uf279") { pickingMode = PickingMode.Ignore }; // map
            icon.AddToClassList("rf-icon");
            icon.AddToClassList("rf-hero-bigmap__icon");
            title = new Label { pickingMode = PickingMode.Ignore };
            title.AddToClassList("rf-hero-bigmap__title");
            title.AddToClassList("rf-font-eb");
            var zoomOut = new Button(() => ZoomAt(new Vector2(0.5f, 0.5f), ZoomStep * ZoomStep)) { text = "\uf068" }; // minus
            var zoomIn = new Button(() => ZoomAt(new Vector2(0.5f, 0.5f), 1f / (ZoomStep * ZoomStep))) { text = "\uf067" }; // plus
            var whole = new Button(() => { view = landCentre; half = landHalf; dirty = true; }) { text = "\uf065" }; // expand
            var close = new Button(Close) { text = "\uf00d" };
            head.Add(icon);
            head.Add(title);
            foreach (var b in new[] { zoomOut, zoomIn, whole, close })
            {
                b.AddToClassList("rf-icon");
                b.AddToClassList("rf-hero-bigmap__close");
                head.Add(b);
            }

            map = new VisualElement();
            map.AddToClassList("rf-hero-bigmap__map");
            map.RegisterCallback<PointerDownEvent>(Pressed);
            map.RegisterCallback<PointerMoveEvent>(Moved);
            map.RegisterCallback<PointerUpEvent>(Released);
            map.RegisterCallback<WheelEvent>(Wheel);
            dots = new VisualElement { pickingMode = PickingMode.Ignore };
            dots.AddToClassList("rf-hero-bigmap__dots");
            // the quests' areas, under everything else on the map (QuestAreas)
            areaLayer = new VisualElement { pickingMode = PickingMode.Ignore };
            areaLayer.AddToClassList("rf-hero-bigmap__dots");
            map.Add(areaLayer);
            pin = new VisualElement { pickingMode = PickingMode.Ignore };
            pin.AddToClassList("rf-hero-bigmap__pin");
            arrow = new Label("\uf124") { pickingMode = PickingMode.Ignore }; // location-arrow
            arrow.AddToClassList("rf-icon");
            arrow.AddToClassList("rf-hero-bigmap__hero");
            map.Add(dots);
            map.Add(pin);
            map.Add(arrow);

            var hint = new Label(Localization.UI_HERO_MAP_HINT) { pickingMode = PickingMode.Ignore };
            hint.AddToClassList("rf-hero-bigmap__hint");

            panel.Add(head);
            panel.Add(map);
            panel.Add(hint);
            Root.Add(panel);
            Root.style.display = DisplayStyle.None;
        }

        public void Toggle(PlayerController hero)
        {
            if (IsOpen) Close();
            else Open(hero);
        }

        public void Open(PlayerController hero)
        {
            if (!hero) return;
            island = hero.Island ? hero.Island : GameManager.Instance ? GameManager.Instance.Islands.FindPlayerIsland(hero) : null;
            if (!island) return;
            height = island.Centre.y;
            (landCentre, landHalf) = LandOf(island);
            view = landCentre;
            half = landHalf;
            title.text = island.Identifier;
            Root.style.display = DisplayStyle.Flex;
            dirty = true;
        }

        public void Close()
        {
            Root.style.display = DisplayStyle.None;
            if (pressed >= 0 && map.HasPointerCapture(pressed)) map.ReleasePointer(pressed);
            pressed = -1;
            dragging = false;
        }

        /// <summary>As big as the screen allows, square, room left for the title and the hint.</summary>
        private void Fit()
        {
            var size = Root.layout.size;
            if (float.IsNaN(size.x) || size.x <= 0f) return;
            var side = Mathf.Clamp(Mathf.Min(size.x - 120f, size.y - 190f), 400f, 1400f);
            map.style.width = side;
            map.style.height = side;
        }

        public void Update(PlayerController hero)
        {
            if (!IsOpen) return;
            if (!hero || !hero.Island || hero.Island != island) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.Escape) && !Keybinds.IsTyping) { EscapeKey.Claim(); Close(); return; }

            if (dirty || Time.unscaledTime >= nextRender)
            {
                dirty = false;
                nextRender = Time.unscaledTime + RenderEvery;
                Render();
            }

            PlaceAreas(hero);

            var used = 0;
            foreach (var p in GameManager.Instance.Players.GetAllPlayers())
            {
                if (!p || p == hero || p.Island != island) continue;
                Place(used++, p.transform.position, "rf-hero-minimap__dot--friend");
            }
            if (IslandNpc.MarkFor != null)
            {
                foreach (var n in IslandNpc.All)
                {
                    if (!n || IslandNpc.MarkFor(n.Key) == IslandNpc.Mark.None) continue;
                    Place(used++, n.transform.position, "rf-hero-minimap__dot--quest");
                }
            }
            for (var i = used; i < pool.Count; i++) pool[i].style.display = DisplayStyle.None;

            var at = ToMap(hero.transform.position);
            arrow.style.display = Inside(at) ? DisplayStyle.Flex : DisplayStyle.None;
            arrow.style.left = Length.Percent(at.x * 100f);
            arrow.style.top = Length.Percent(at.y * 100f);
            // the glyph points up and to the right
            arrow.style.rotate = new Rotate(hero.transform.eulerAngles.y - 45f);

            var target = HeroController.Active ? HeroController.Active.MapTarget : null;
            var pinAt = target.HasValue ? ToMap(target.Value) : default;
            pin.style.display = target.HasValue && Inside(pinAt) ? DisplayStyle.Flex : DisplayStyle.None;
            pin.style.left = Length.Percent(pinAt.x * 100f);
            pin.style.top = Length.Percent(pinAt.y * 100f);
        }

        /// <summary>A world point as a share across and down the view, 0 to 1.</summary>
        private Vector2 ToMap(Vector3 world) =>
            new Vector2(0.5f + (world.x - view.x) / (2f * half), 0.5f - (world.z - view.y) / (2f * half));

        /// <summary>A share across and down the view as a place on the island (x and z).</summary>
        private Vector2 ToWorld(Vector2 share) =>
            new Vector2(view.x + (share.x - 0.5f) * 2f * half, view.y - (share.y - 0.5f) * 2f * half);

        private static bool Inside(Vector2 at) => at.x >= 0f && at.x <= 1f && at.y >= 0f && at.y <= 1f;

        private Vector2 Share(Vector2 local)
        {
            var size = map.contentRect.size;
            return size.x > 0f && size.y > 0f ? new Vector2(local.x / size.x, local.y / size.y) : new Vector2(0.5f, 0.5f);
        }

        /// <summary>Where the hero's quests can be done: a soft red circle for each area, sized to the view.</summary>
        private void PlaceAreas(PlayerController hero)
        {
            var list = QuestAreas.For(hero);
            for (var i = 0; i < list.Count; i++)
            {
                while (areaPool.Count <= i)
                {
                    var e = new VisualElement { pickingMode = PickingMode.Ignore };
                    e.AddToClassList("rf-hero-map__area");
                    areaLayer.Add(e);
                    areaPool.Add(e);
                }
                var area = areaPool[i];
                var at = ToMap(list[i].Centre);
                var size = list[i].Radius / half * 100f;
                area.style.display = DisplayStyle.Flex;
                area.style.left = Length.Percent(at.x * 100f - size / 2f);
                area.style.top = Length.Percent(at.y * 100f - size / 2f);
                area.style.width = Length.Percent(size);
                area.style.height = Length.Percent(size);
            }
            for (var i = list.Count; i < areaPool.Count; i++) areaPool[i].style.display = DisplayStyle.None;
        }

        private void Place(int index, Vector3 world, string kind)
        {
            while (pool.Count <= index)
            {
                var d = new VisualElement { pickingMode = PickingMode.Ignore };
                d.AddToClassList("rf-hero-minimap__dot");
                d.AddToClassList("rf-hero-bigmap__dot");
                dots.Add(d);
                pool.Add(d);
            }
            var dot = pool[index];
            var at = ToMap(world);
            dot.style.display = Inside(at) ? DisplayStyle.Flex : DisplayStyle.None;
            dot.EnableInClassList("rf-hero-minimap__dot--friend", kind == "rf-hero-minimap__dot--friend");
            dot.EnableInClassList("rf-hero-minimap__dot--quest", kind == "rf-hero-minimap__dot--quest");
            dot.style.left = Length.Percent(at.x * 100f);
            dot.style.top = Length.Percent(at.y * 100f);
        }

        private void Wheel(WheelEvent e)
        {
            // down (positive) zooms out, as maps do
            ZoomAt(Share(e.localMousePosition), e.delta.y > 0f ? ZoomStep : 1f / ZoomStep);
            e.StopPropagation();
        }

        /// <summary>Zooms by <paramref name="factor"/> (over 1 out) keeping the place under <paramref name="share"/> still.</summary>
        private void ZoomAt(Vector2 share, float factor)
        {
            var still = ToWorld(share);
            half = Mathf.Clamp(half * factor, MostZoom, landHalf);
            view = still - new Vector2((share.x - 0.5f) * 2f * half, -(share.y - 0.5f) * 2f * half);
            Keep();
            dirty = true;
        }

        /// <summary>The view stays over the island: its middle no further out than the land's edge.</summary>
        private void Keep()
        {
            var room = Mathf.Max(0f, landHalf - half);
            view.x = Mathf.Clamp(view.x, landCentre.x - room, landCentre.x + room);
            view.y = Mathf.Clamp(view.y, landCentre.y - room, landCentre.y + room);
        }

        private void Pressed(PointerDownEvent e)
        {
            if (pressed >= 0) return;
            pressed = e.pointerId;
            pressedAt = e.localPosition;
            dragging = false;
            map.CapturePointer(e.pointerId);
            e.StopPropagation();
        }

        private void Moved(PointerMoveEvent e)
        {
            if (e.pointerId != pressed) return;
            if (!dragging && ((Vector2)e.localPosition - pressedAt).magnitude < DragPixels) return;
            dragging = true;
            var size = map.contentRect.size;
            if (size.x <= 0f) return;
            var metresPerPixel = 2f * half / size.x;
            view += new Vector2(-e.deltaPosition.x * metresPerPixel, e.deltaPosition.y * metresPerPixel);
            Keep();
            dirty = true;
            e.StopPropagation();
        }

        /// <summary>A click (a press that did not drag) walks the hero there, to the nearest ground it can reach.</summary>
        private void Released(PointerUpEvent e)
        {
            if (e.pointerId != pressed) return;
            if (map.HasPointerCapture(pressed)) map.ReleasePointer(pressed);
            pressed = -1;
            if (!dragging && e.button == 0)
            {
                var at = ToWorld(Share(e.localPosition));
                HeroController.Active?.WalkToMap(new Vector3(at.x, height, at.y));
            }
            dragging = false;
            e.StopPropagation();
        }

        /// <summary>
        /// The island's land, square and with a little sea round it: rays down over its sphere, the hits above the sea.
        /// Worked out once per island.
        /// </summary>
        private static (Vector2 Centre, float Half) LandOf(IslandController island)
        {
            if (lands.TryGetValue(island, out var known)) return known;
            var centre = island.Centre;
            var radius = Mathf.Max(30f, island.Radius);
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            var sea = SeaChart.Sea + 0.3f;
            for (var i = 0; i < LandSamples; i++)
            {
                for (var j = 0; j < LandSamples; j++)
                {
                    var x = centre.x - radius + (i + 0.5f) * 2f * radius / LandSamples;
                    var z = centre.z - radius + (j + 0.5f) * 2f * radius / LandSamples;
                    if (!Physics.Raycast(new Vector3(x, centre.y + Above, z), Vector3.down, out var hit, Above * 2f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    if (hit.point.y < sea) continue;
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minZ = Mathf.Min(minZ, z); maxZ = Mathf.Max(maxZ, z);
                }
            }
            var land = minX > maxX
                ? (new Vector2(centre.x, centre.z), radius)
                : (new Vector2((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f), Mathf.Max(maxX - minX, maxZ - minZ) * 0.5f * 1.08f + 10f);
            lands[island] = land;
            return land;
        }

        private void Render()
        {
            EnsureCamera();
            cam.orthographicSize = half;
            cam.farClipPlane = Above + 400f;
            cam.transform.SetPositionAndRotation(new Vector3(view.x, height + Above, view.y), Quaternion.Euler(90f, 0f, 0f));
            // the world's haze is for the view across the island; from straight above it only washes the map out
            var fog = RenderSettings.fog;
            RenderSettings.fog = false;
            cam.Render();
            RenderSettings.fog = fog;
        }

        private void EnsureCamera()
        {
            if (cam) return;
            texture = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32) { name = "Hero Big Map" };
            var go = new GameObject("Hero Big Map Camera") { hideFlags = HideFlags.DontSave };
            cam = go.AddComponent<Camera>();
            cam.enabled = false; // drawn by hand when the view changes and now and then
            cam.orthographic = true;
            cam.nearClipPlane = 1f;
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
    }
}
