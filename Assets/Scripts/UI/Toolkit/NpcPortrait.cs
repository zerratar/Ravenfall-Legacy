using System.Collections.Generic;
using UnityEngine;

namespace Shinobytes.UI
{
    /// <summary>
    /// An NPC's face for the dialogue box (RavebridgeDialogueView): their own model's head and shoulders, rendered once
    /// into a small texture the first time they are talked to and kept. A camera and a soft light in front of their
    /// face for that one render, only their renderers drawn (moved to a spare layer meanwhile), on a dark backdrop, then
    /// copied into a plain texture (UI Toolkit left the render texture itself blank). A character in the stream gets a
    /// wider one, head and chest, for the "Who plays?" window's cards (RavebridgeView), rendered fresh each time (their
    /// look and gear change).
    /// </summary>
    public static class NpcPortrait
    {
        private const int Size = 320; // drawn larger than shown (150 px)
        private const int Layer = 31;
        private static readonly Dictionary<string, Texture2D> faces = new Dictionary<string, Texture2D>();
        private static readonly List<(GameObject Go, int Layer)> moved = new List<(GameObject, int)>();

        public static Texture2D Of(IslandNpc npc)
        {
            if (!npc) return null;
            var key = string.IsNullOrEmpty(npc.Key) ? npc.name : npc.Key;
            if (faces.TryGetValue(key, out var face) && face) return face;
            face = Render(npc.transform, npc.HeadHeight, "Portrait " + key, false);
            if (face) faces[key] = face;
            return face;
        }

        /// <summary>A character's head and chest, 4:3, rendered now (the caller keeps or destroys it).</summary>
        public static Texture2D Of(PlayerController player) => player ? Render(player.transform, 2f, "Portrait " + player.Name, true) : null;

        private static Texture2D Render(Transform root, float headHeight, string name, bool wide)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return null;

            // where the face is: the head bone when there is one, else most of the way up
            var animator = root.GetComponentInChildren<Animator>();
            var head = animator && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            var scale = root.lossyScale.y;
            var faceAt = head ? head.position + Vector3.up * 0.06f * scale : root.position + Vector3.up * headHeight * 0.78f;
            var forward = root.forward;
            // the wide one looks from further off and more from the front, between the face and the chest, so the
            // head (hat and all), the shoulders and the armour are in it
            var width = wide ? 400 : Size;
            var height = wide ? 300 : Size;
            var aim = wide ? faceAt - Vector3.up * 0.22f * scale : faceAt - Vector3.up * 0.1f * scale;
            var distance = wide ? 3.2f : 1.05f;
            var aside = wide ? 0.12f : 0.27f;

            var texture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 4);
            Texture2D face = null;

            var camGo = new GameObject("Portrait camera");
            var lightGo = new GameObject("Portrait light");
            moved.Clear();
            try
            {
                foreach (var r in renderers)
                {
                    // not a character's pet following at its heels
                    if (!r || r is ParticleSystemRenderer || r is SpriteRenderer || r.GetComponent<TMPro.TMP_Text>() || r.GetComponentInParent<PetController>()) continue;
                    moved.Add((r.gameObject, r.gameObject.layer));
                    r.gameObject.layer = Layer;
                }

                var cam = camGo.AddComponent<Camera>();
                cam.enabled = false;
                cam.cullingMask = 1 << Layer;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.1f, 0.08f, 1f);
                cam.fieldOfView = 24f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 10f;
                cam.targetTexture = texture;
                // a little from the side and above, as a portrait is taken
                var from = faceAt + (forward * distance + root.right * (distance * aside) + Vector3.up * 0.05f) * scale;
                cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(aim - from));

                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 4f * scale;
                light.intensity = 2.2f;
                light.color = new Color(1f, 0.92f, 0.8f);
                light.cullingMask = 1 << Layer;
                lightGo.transform.position = faceAt + (forward * 0.9f + root.right * -0.4f + Vector3.up * 0.5f) * scale;

                cam.Render();

                var previous = RenderTexture.active;
                RenderTexture.active = texture;
                face = new Texture2D(width, height, TextureFormat.RGBA32, true) { name = name };
                face.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                face.Apply(true, true);
                RenderTexture.active = previous;
            }
            finally
            {
                foreach (var (go, layer) in moved) if (go) go.layer = layer;
                moved.Clear();
                // off at once: Destroy waits for the frame's end, and the light would show in this frame's picture
                camGo.SetActive(false);
                lightGo.SetActive(false);
                Object.Destroy(camGo);
                Object.Destroy(lightGo);
                RenderTexture.ReleaseTemporary(texture);
            }
            return face;
        }
    }
}
