using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The streamer plays a character by hand (phase 6c of docs/ravebridge-design.md), the old RuneScape way: WASD walks
/// relative to the camera, a click on the ground walks there, a click on a tree, gather spot, rock, fishing spot,
/// farm patch or enemy does that (the target's own training spot runs the task, so range, experience and items are
/// those of automatic play, and nothing picks the next target by itself), a click on an NPC talks to it. While a
/// character is controlled PlayerController skips its automatic play; the camera follows it (GameCamera.FollowControlled).
/// Hero mode will build its skills and dodge on this.
/// </summary>
public class ManualPlayerController : MonoBehaviour
{
    private const float ClickPixels = 48f;
    private static readonly RaycastHit[] hits = new RaycastHit[32];

    private PlayerController player;
    private PlayerAnimationController animations;
    private bool walkingByKeys;

    public bool Active { get; internal set; }

    /// <summary>An NPC was clicked (IslandNpc talks): the dialogue window listens.</summary>
    public static event Action<PlayerController, IslandNpc> TalkTo;

    void Start()
    {
        this.player = GetComponent<PlayerController>();
        this.animations = player.Animations;
    }

    public void Poll()
    {
        if (!Active || !player || player.Stats.IsDead)
            return;

        var cam = Camera.main;
        if (!cam) return;

        if (!Keybinds.IsTyping)
        {
            var h = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            var v = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            if (h != 0f || v != 0f)
            {
                var forward = cam.transform.forward; forward.y = 0f; forward.Normalize();
                var right = cam.transform.right; right.y = 0f; right.Normalize();
                var direction = (forward * v + right * h).normalized;
                // keys take over from whatever the character was doing
                player.taskTarget = null;
                player.InterruptAction();
                player.SetDestination(player.Position + direction * 2f);
                walkingByKeys = true;
                return;
            }
            if (walkingByKeys)
            {
                walkingByKeys = false;
                player.SetDestination(player.Position);
            }
        }

        if (Input.GetMouseButtonDown(0) && !OverHud())
        {
            Click(cam);
        }

        if (player.taskTarget != null)
        {
            player.DoManualTask();
        }
    }

    private static bool OverHud() => Shinobytes.UI.HudPointer.IsOverHud() || UIUtility.IsPointerOverUIElement() || Shinobytes.UI.DraggableWindow.IsPointerOverWindow;

    private void Click(Camera cam)
    {
        Physics.SyncTransforms();
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        var count = Physics.RaycastNonAlloc(ray, hits, 400f, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, 0, count, HitComparer.Instance);

        Vector3? ground = null;
        for (var i = 0; i < count; i++)
        {
            var c = hits[i].collider;
            if (!c || c.transform.IsChildOf(player.transform)) continue;

            var npc = c.GetComponentInParent<IslandNpc>();
            if (npc) { Talk(npc); return; }
            var enemy = c.GetComponentInParent<EnemyController>();
            if (enemy && !enemy.Stats.IsDead) { Act(TaskType.Fighting, CombatArgument(), enemy.GetComponentInParent<Chunk>(), enemy); return; }
            var rock = c.GetComponentInParent<RockController>();
            if (rock) { Act(TaskType.Mining, null, rock.GetComponentInParent<Chunk>(), rock); return; }
            var fishing = c.GetComponentInParent<FishingController>();
            if (fishing) { Act(TaskType.Fishing, null, fishing.GetComponentInParent<Chunk>(), fishing); return; }
            var farm = c.GetComponentInParent<FarmController>();
            if (farm) { Act(TaskType.Farming, null, farm.GetComponentInParent<Chunk>(), farm); return; }

            if (!c.isTrigger && c.name != "Do Not Walk" && ground == null) ground = hits[i].point;
        }

        // trees and gather spots have no colliders: the closest one to the click on screen
        var resource = ResourceUnderMouse(cam);
        if (resource != null)
        {
            var chunk = resource.Field.GetComponentInParent<Chunk>();
            if (chunk != null) { Act(chunk.Type, null, chunk, resource); return; }
        }

        if (ground.HasValue)
        {
            player.taskTarget = null;
            player.InterruptAction();
            player.SetDestination(ground.Value);
        }
    }

    private void Act(TaskType task, string argument, Chunk chunk, object target)
    {
        if (chunk == null) return;
        player.InterruptAction();
        player.SetManualTask(task, argument, chunk, target);
    }

    private void Talk(IslandNpc npc)
    {
        player.taskTarget = null;
        if (Vector3.Distance(player.Position, npc.transform.position) > npc.TalkDistance)
        {
            player.SetDestination(npc.transform.position);
            pendingTalk = npc;
            return;
        }
        player.SetDestination(player.Position);
        TalkTo?.Invoke(player, npc);
    }

    private IslandNpc pendingTalk;

    private void LateUpdate()
    {
        // walking up to someone to talk to them
        if (!Active || !pendingTalk || !player) return;
        if (Vector3.Distance(player.Position, pendingTalk.transform.position) <= pendingTalk.TalkDistance)
        {
            var npc = pendingTalk;
            pendingTalk = null;
            player.SetDestination(player.Position);
            TalkTo?.Invoke(player, npc);
        }
        else if (player.taskTarget != null) pendingTalk = null;
    }

    /// <summary>The combat style the character already trains, or all of them.</summary>
    private string CombatArgument()
    {
        var current = player.GetTask() == TaskType.Fighting ? player.GetTaskArgument() : null;
        return string.IsNullOrEmpty(current) || current == "fighting" ? "all" : current;
    }

    private ResourceTarget ResourceUnderMouse(Camera cam)
    {
        var island = player.Island;
        if (!island) return null;
        if (fieldsOf != island)
        {
            fieldsOf = island;
            fields.Clear();
            fields.AddRange(island.GetComponentsInChildren<ResourceField>());
        }

        ResourceTarget best = null;
        var bestPixels = ClickPixels;
        var mouse = (Vector2)Input.mousePosition;
        foreach (var field in fields)
        {
            var targets = field.Targets;
            for (var i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (!t.IsAvailable) continue;
                var screen = cam.WorldToScreenPoint(t.Position + Vector3.up * 1.2f);
                if (screen.z <= 0f || screen.z > 120f) continue;
                var pixels = Vector2.Distance(mouse, screen);
                if (pixels < bestPixels) { bestPixels = pixels; best = t; }
            }
        }
        return best;
    }

    private IslandController fieldsOf;
    private readonly List<ResourceField> fields = new List<ResourceField>();

    private sealed class HitComparer : IComparer<RaycastHit>
    {
        public static readonly HitComparer Instance = new HitComparer();
        public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
    }
}
