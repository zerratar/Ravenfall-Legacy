using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Talking to Ravebridge's people (phase 6e of docs/ravebridge-design.md), as in an RPG: a box along the bottom of
    /// the screen with the NPC's face (NpcPortrait), their name and what they say typing itself out, then numbered
    /// answers (RavebridgeQuests.Talk: the quest's choices, things to say besides, Goodbye). 1 to 9 or a click picks an
    /// answer; Space, E, Enter or a click on the box finishes the line; Esc or walking off ends the talk. Close to an NPC
    /// a small "E Talk" prompt floats over them (ManualPlayerController.NearestNpc).
    /// </summary>
    public class RavebridgeDialogueView : MonoBehaviour
    {
        private const float CharsPerSecond = 55f;

        // every line the same distance apart: left to the font, wrapped lines sat closer than others (playtest 17)
        // a percentage, never a decimal: the text parser reads "1.45em" with the computer's number format, and on a
        // Swedish one (decimal comma) it came out smaller than no tag at all, the lines drawn over each other
        private const string LineHeight = "<line-height=140%>";

        private VisualElement box;
        private VisualElement portrait;
        private Label portraitLetter;
        private Label nameLabel;
        private Label line;
        private Label hint;
        private VisualElement options;
        private VisualElement prompt;
        private Label promptName;
        private bool bound;
        private PlayerController talker;
        private IslandNpc npc;
        private RavebridgeShopWindow shop;

        private string fullLine = string.Empty;
        private float typed;
        private bool typing;
        private readonly List<(RavebridgeQuests.Option Option, VisualElement Row)> shown = new List<(RavebridgeQuests.Option, VisualElement)>();
        private float openedAt;

        /// <summary>A talk is on: it takes the keys (hero mode's skills wait).</summary>
        public static bool IsOpen { get; private set; }

        private void OnEnable()
        {
            bound = false;
            ManualPlayerController.TalkTo += Open;
        }

        private void OnDisable()
        {
            ManualPlayerController.TalkTo -= Open;
            bound = false;
            IsOpen = false;
        }

        private void TryBind()
        {
            var doc = GetComponent<UIDocument>();
            var layer = doc != null ? doc.rootVisualElement?.Q<VisualElement>("conn-layer") : null;
            if (layer == null) return;

            box?.RemoveFromHierarchy();
            prompt?.RemoveFromHierarchy();

            box = new VisualElement { name = "ravebridge-dialogue" };
            box.AddToClassList("rf-talk");
            box.RegisterCallback<PointerDownEvent>(_ => { if (typing) Finish(); });

            portrait = new VisualElement();
            portrait.AddToClassList("rf-talk__face");
            portraitLetter = new Label();
            portraitLetter.AddToClassList("rf-talk__face-letter");
            portrait.Add(portraitLetter);
            box.Add(portrait);

            var body = new VisualElement();
            body.AddToClassList("rf-talk__body");
            var head = new VisualElement();
            head.AddToClassList("rf-talk__head");
            nameLabel = new Label();
            nameLabel.AddToClassList("rf-talk__name");
            var close = new Button(Close) { text = "" };
            close.AddToClassList("rf-icon");
            close.AddToClassList("rf-talk__close");
            head.Add(nameLabel);
            head.Add(close);
            line = new Label();
            line.AddToClassList("rf-talk__line");
            line.enableRichText = true;
            options = new VisualElement();
            options.AddToClassList("rf-talk__options");
            hint = new Label(Localization.UI_TALK_SKIP);
            hint.AddToClassList("rf-talk__hint");
            body.Add(head);
            body.Add(line);
            body.Add(options);
            body.Add(hint);
            box.Add(body);
            box.style.display = DisplayStyle.None;
            layer.Add(box);

            // "E Talk" over the NPC close by
            prompt = new VisualElement { pickingMode = PickingMode.Ignore };
            prompt.AddToClassList("rf-talk-prompt");
            var key = new Label("E") { pickingMode = PickingMode.Ignore };
            key.AddToClassList("rf-talk-prompt__key");
            promptName = new Label { pickingMode = PickingMode.Ignore };
            promptName.AddToClassList("rf-talk-prompt__text");
            prompt.Add(key);
            prompt.Add(promptName);
            prompt.style.display = DisplayStyle.None;
            // under every window on the layer (the Stream quest, the shop), never over one
            layer.Insert(0, prompt);

            shop?.Remove();
            shop = new RavebridgeShopWindow(layer);
            bound = true;
        }

        private void Update()
        {
            if (bound && box.panel == null) bound = false;
            if (!bound) TryBind();
            shop?.Update();
            if (!bound) return;
            UpdatePrompt();
            if (!IsOpen) return;

            // walking away, or no longer playing, ends the talk
            if (!talker || !npc || RavebridgePlay.Playing != talker || Vector3.Distance(talker.Position, npc.transform.position) > npc.TalkDistance + 3f) { Close(); return; }
            if (Keybinds.IsTyping) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { EscapeKey.Claim(); Close(); return; }

            if (typing)
            {
                // a slow frame (the face drawn, the font filling in) must not type half the line at once
                typed += Mathf.Min(Time.unscaledDeltaTime, 0.05f) * CharsPerSecond;
                // the key that opened the talk (E) must not skip its first line
                var skip = Time.unscaledTime - openedAt > 0.3f && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return));
                if (skip || typed >= fullLine.Length) Finish();
                else DrawLine();
                return;
            }

            for (var i = 0; i < shown.Count && i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) { Pick(shown[i].Option); return; }
            }
            // with one answer, Space or E takes it too
            if (shown.Count == 1 && Time.unscaledTime - openedAt > 0.15f && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))) Pick(shown[0].Option);
        }

        private void UpdatePrompt()
        {
            var player = RavebridgePlay.Playing;
            var near = player && !IsOpen ? ManualPlayerController.NearestNpc(player) : null;
            var cam = Camera.main;
            if (!near || !cam || prompt.panel == null)
            {
                prompt.style.display = DisplayStyle.None;
                return;
            }
            // beside the mark over their head, never on it (nor on a line they say, which goes over the mark); with no
            // mark, just over the head. Worked out from the mark's place, so it stays put at any zoom.
            var marked = near.MarkShown;
            var at = marked ? near.MarkCenter : near.HeadTop;
            if (Vector3.Dot(cam.transform.forward, at - cam.transform.position) <= 0f) { prompt.style.display = DisplayStyle.None; return; }
            var panelPoint = RuntimePanelUtils.CameraTransformWorldToPanel(prompt.panel, at, cam);
            promptName.text = string.Format(CultureInfo.InvariantCulture, Localization.UI_TALK_PROMPT, RavebridgeQuests.NpcName(near.Key));
            prompt.style.display = DisplayStyle.Flex;
            var width = prompt.resolvedStyle.width;
            var height = prompt.resolvedStyle.height;
            if (float.IsNaN(width)) width = 0f;
            if (float.IsNaN(height)) height = 0f;
            if (marked)
            {
                var top = RuntimePanelUtils.CameraTransformWorldToPanel(prompt.panel, at + cam.transform.up * near.MarkHeight * 0.5f, cam);
                var half = Mathf.Abs(panelPoint.y - top.y);
                prompt.style.left = panelPoint.x + half + 10f;
                prompt.style.top = panelPoint.y - height / 2f;
            }
            else
            {
                prompt.style.left = panelPoint.x - width / 2f;
                prompt.style.top = panelPoint.y - height - 6f;
            }
        }

        private void Open(PlayerController player, IslandNpc who)
        {
            if (!bound) TryBind();
            if (!bound || !player || !who) return;
            if (npc && npc != who) npc.Hushed = false;
            talker = player;
            npc = who;
            npc.Hushed = true;
            // above hero mode's skill bar, else lower down
            box.EnableInClassList("rf-talk--low", !HeroController.Active);
            var face = NpcPortrait.Of(who);
            portrait.style.backgroundImage = face ? Background.FromTexture2D(face) : default;
            Show(RavebridgeQuests.Talk(player, who.Key));
        }

        private void Show(RavebridgeQuests.Reply reply)
        {
            nameLabel.text = reply.Npc;
            portraitLetter.text = portrait.style.backgroundImage.value.texture ? string.Empty : (string.IsNullOrEmpty(reply.Npc) ? "?" : reply.Npc.Substring(0, 1));
            fullLine = reply.Line ?? string.Empty;
            typed = 0f;
            typing = fullLine.Length > 0;
            openedAt = Time.unscaledTime;

            options.Clear();
            shown.Clear();
            var n = 0;
            foreach (var option in reply.Options)
            {
                var o = option;
                n++;
                var row = new VisualElement();
                row.AddToClassList("rf-talk__option");
                // the quest's own step stands out, ways out step back
                row.EnableInClassList("rf-talk__option--quest", o.Pick != null && o.Then == null && !o.Closes && o.Label != Localization.UI_RAVEBRIDGE_Q_MORE);
                row.EnableInClassList("rf-talk__option--leave", o.Leaves);
                var key = new Label(n.ToString(CultureInfo.InvariantCulture));
                key.AddToClassList("rf-talk__option-key");
                var text = new Label(o.Label);
                text.AddToClassList("rf-talk__option-text");
                row.Add(key);
                row.Add(text);
                row.RegisterCallback<UnityEngine.UIElements.ClickEvent>(_ => { if (!typing) Pick(o); });
                options.Add(row);
                shown.Add((o, row));
            }
            box.style.display = DisplayStyle.Flex;
            IsOpen = true;
            if (typing) DrawLine(); else Finish();
        }

        /// <summary>The line so far: the rest is there but clear, so the words do not jump about as they wrap.</summary>
        private void DrawLine()
        {
            var count = Mathf.Clamp(Mathf.FloorToInt(typed), 0, fullLine.Length);
            line.text = LineHeight + (count >= fullLine.Length ? fullLine : fullLine.Substring(0, count) + "<alpha=#00>" + fullLine.Substring(count));
            options.style.opacity = 0f;
            hint.style.visibility = Visibility.Visible;
        }

        private void Finish()
        {
            typing = false;
            line.text = LineHeight + fullLine;
            options.style.opacity = 1f;
            hint.style.visibility = Visibility.Hidden;
        }

        private void Pick(RavebridgeQuests.Option o)
        {
            if (o.Then != null) { Show(o.Then()); return; }
            if (o.Pick == null) { Close(); return; }
            if (o.Closes) { Close(); o.Pick(); return; }
            o.Pick();
            if (talker && npc) Show(RavebridgeQuests.Talk(talker, npc.Key));
        }

        private void Close()
        {
            if (box != null) box.style.display = DisplayStyle.None;
            IsOpen = false;
            if (npc) npc.Hushed = false;
            talker = null;
            npc = null;
        }
    }
}
