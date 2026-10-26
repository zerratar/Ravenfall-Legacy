using UnityEngine;
using UnityEngine.UIElements;

namespace Shinobytes.UI
{
    /// <summary>
    /// Talking to Ravebridge's people (phase 6e of docs/ravebridge-design.md): a click on an NPC from the character
    /// played by hand opens this window with what they say (RavebridgeQuests.Talk) and the answers; they say it out
    /// loud in a speech bubble too, for the stream. In the quest window's look: the NPC's name, a speech box, buttons.
    /// </summary>
    public class RavebridgeDialogueView : MonoBehaviour
    {
        private VisualElement window;
        private Label title;
        private Label line;
        private VisualElement options;
        private bool bound;
        private PlayerController talker;
        private IslandNpc npc;
        private RavebridgeShopWindow shop;

        private void OnEnable()
        {
            bound = false;
            ManualPlayerController.TalkTo += Open;
        }

        private void OnDisable()
        {
            ManualPlayerController.TalkTo -= Open;
            bound = false;
        }

        private void TryBind()
        {
            var doc = GetComponent<UIDocument>();
            var layer = doc != null ? doc.rootVisualElement?.Q<VisualElement>("conn-layer") : null;
            if (layer == null) return;

            window?.RemoveFromHierarchy();
            window = new VisualElement { name = "ravebridge-dialogue" };
            window.AddToClassList("rf-squest");
            window.AddToClassList("rf-ravebridge-dialogue");
            var header = new VisualElement();
            header.AddToClassList("rf-squest__header");
            title = new Label();
            title.AddToClassList("rf-squest__title");
            var close = new Button(Close) { text = "" };
            close.AddToClassList("rf-icon");
            close.AddToClassList("rf-squest__close");
            header.Add(title);
            header.Add(close);
            var speech = new VisualElement();
            speech.AddToClassList("rf-story__speech");
            line = new Label();
            line.AddToClassList("rf-story__line");
            speech.Add(line);
            options = new VisualElement();
            options.AddToClassList("rf-squest-chips");
            window.Add(header);
            window.Add(speech);
            window.Add(options);
            window.style.display = DisplayStyle.None;
            layer.Add(window);
            shop?.Remove();
            shop = new RavebridgeShopWindow(layer);
            bound = true;
        }

        private void Update()
        {
            if (bound && window.panel == null) bound = false;
            if (!bound) TryBind();
            shop?.Update();
            if (!bound || window.style.display == DisplayStyle.None) return;

            // walking away, or no longer playing, ends the talk
            if (!talker || !npc || RavebridgePlay.Playing != talker || Vector3.Distance(talker.Position, npc.transform.position) > npc.TalkDistance + 3f) Close();
            else if (Input.GetKeyDown(KeyCode.Escape) && !Keybinds.IsTyping) { EscapeKey.Claim(); Close(); }
        }

        private void Open(PlayerController player, IslandNpc who)
        {
            if (!bound) TryBind();
            if (!bound || !player || !who) return;
            talker = player;
            npc = who;
            Show(RavebridgeQuests.Talk(player, who.Key));
        }

        private void Show(RavebridgeQuests.Reply reply)
        {
            title.text = reply.Npc;
            line.text = reply.Line ?? string.Empty;
            npc.Say(reply.Line);
            options.Clear();
            foreach (var option in reply.Options)
            {
                var o = option;
                var button = new Button(() =>
                {
                    if (o.Pick == null) { Close(); return; }
                    if (o.Closes) { Close(); o.Pick(); return; }
                    o.Pick();
                    if (talker && npc) Show(RavebridgeQuests.Talk(talker, npc.Key));
                }) { text = o.Label };
                button.AddToClassList("rf-squest-chip");
                button.EnableInClassList("rf-squest-chip--active", o.Pick != null);
                options.Add(button);
            }
            window.style.display = DisplayStyle.Flex;
        }

        private void Close()
        {
            if (window != null) window.style.display = DisplayStyle.None;
            talker = null;
            npc = null;
        }
    }
}
