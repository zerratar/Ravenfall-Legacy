using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The streamer plays a character by hand (phase 6c of docs/ravebridge-design.md), the old RuneScape way: WASD walks
/// relative to the camera, a click on the ground walks there, a click on a tree, gather spot, rock, fishing spot,
/// farm patch or enemy does that (the target's own training spot runs the task, so range, experience and items are
/// those of automatic play, and nothing picks the next target by itself), a click on an NPC talks to it. While a
/// character is controlled PlayerController skips its automatic play; the camera follows it (GameCamera.FollowControlled).
/// A marker shows where a click went (HeroMarker). Hero mode fights by itself (HeroController) and comes here for the
/// rest: trees, rocks, fish, farms and NPCs (<see cref="Interact"/>).
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

        // the hero plays itself (HeroController)
        var hero = HeroController.Active;
        if (hero && hero.Player == player)
            return;

        var cam = Camera.main;
        if (!cam) return;

        if (!Keybinds.IsTyping)
        {
            var view = GameManager.Instance ? GameManager.Instance.Camera.HeroView : null;
            if (Input.GetKeyDown(KeyCode.V) && view) view.Toggle();
            // E talks to the NPC close by (TalkPrompt shows who)
            if (Input.GetKeyDown(KeyCode.E) && !Shinobytes.UI.RavebridgeDialogueView.IsOpen)
            {
                var near = NearestNpc(player);
                if (near) { Talk(near); return; }
            }

            var h = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            var v = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            if (h != 0f || v != 0f)
            {
                var forward = view ? view.Forward : Flat(cam.transform.forward);
                var right = view ? view.Right : Flat(cam.transform.right);
                // keys take over from whatever the character was doing
                player.taskTarget = null;
                pendingTalk = null;
                Steer((forward * v + right * h).normalized);
                return;
            }
            if (walkingByKeys) StopSteering();
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

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v.normalized; }

    /// <summary>
    /// WASD steers at once, as in hero mode: the agent's velocity rather than a path, so the character starts, turns and
    /// stops with the keys (a path 2 m ahead turned it at the agent's slow turning speed).
    /// </summary>
    private void Steer(Vector3 direction)
    {
        var agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (!walkingByKeys) player.InterruptAction();
        player.Movement.Unlock();
        walkingByKeys = true;
        if (!agent || !agent.isOnNavMesh) return;
        // a short path ahead keeps the agent going (with none it drops any velocity)
        if (UnityEngine.AI.NavMesh.SamplePosition(player.Position + direction * 2f, out var ahead, 1.5f, agent.areaMask)) agent.SetDestination(ahead.position);
        agent.velocity = direction * agent.speed;
        player.transform.rotation = Quaternion.RotateTowards(player.transform.rotation, Quaternion.LookRotation(direction), 900f * Time.deltaTime);
    }

    private void StopSteering()
    {
        walkingByKeys = false;
        var agent = player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    /// <summary>The NPC within talking distance nearest the character, if any (E talks to it).</summary>
    public static IslandNpc NearestNpc(PlayerController player)
    {
        if (!player || !player.Island) return null;
        IslandNpc best = null;
        var bestDistance = float.MaxValue;
        foreach (var npc in IslandNpc.All)
        {
            if (!npc || !npc.isActiveAndEnabled) continue;
            var d = Vector3.Distance(player.Position, npc.transform.position);
            if (d <= npc.TalkDistance + 0.5f && d < bestDistance) { best = npc; bestDistance = d; }
        }
        return best;
    }

    private static bool OverHud() => Shinobytes.UI.HudPointer.IsOverHud() || UIUtility.IsPointerOverUIElement() || Shinobytes.UI.DraggableWindow.IsPointerOverWindow;

    private void Click(Camera cam)
    {
        if (Interact(cam, true, out var ground)) return;
        if (ground.HasValue)
        {
            player.taskTarget = null;
            player.InterruptAction();
            player.SetDestination(ground.Value);
            HeroMarker.Show(ground.Value, HeroMarker.Walk);
        }
    }

    /// <summary>
    /// What the mouse points at, done: an NPC talked to, a rock, fishing spot, farm patch, tree or gather spot worked,
    /// and with <paramref name="fight"/> an enemy fought. False if it points at none of them; <paramref name="ground"/>
    /// is then where on the ground it points, if anywhere.
    /// </summary>
    public bool Interact(Camera cam, bool fight, out Vector3? ground)
    {
        Physics.SyncTransforms();
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        var count = Physics.RaycastNonAlloc(ray, hits, 400f, ~0, QueryTriggerInteraction.Collide);
        Array.Sort(hits, 0, count, HitComparer.Instance);

        ground = null;
        for (var i = 0; i < count; i++)
        {
            var c = hits[i].collider;
            if (!c || c.transform.IsChildOf(player.transform)) continue;

            var npc = c.GetComponentInParent<IslandNpc>();
            if (npc) { Talk(npc); return true; }
            var enemy = fight ? c.GetComponentInParent<EnemyController>() : null;
            if (enemy && !enemy.Stats.IsDead) { Act(TaskType.Fighting, CombatArgument(), enemy.GetComponentInParent<Chunk>(), enemy, enemy.Position, HeroMarker.Attack); return true; }
            var rock = c.GetComponentInParent<RockController>();
            if (rock) { Act(TaskType.Mining, null, rock.GetComponentInParent<Chunk>(), rock, rock.transform.position, HeroMarker.Walk); return true; }
            var fishing = c.GetComponentInParent<FishingController>();
            if (fishing) { Act(TaskType.Fishing, null, fishing.GetComponentInParent<Chunk>(), fishing, fishing.transform.position, HeroMarker.Walk); return true; }
            var farm = c.GetComponentInParent<FarmController>();
            if (farm) { Act(TaskType.Farming, null, farm.GetComponentInParent<Chunk>(), farm, farm.transform.position, HeroMarker.Walk); return true; }

            if (!c.isTrigger && c.name != "Do Not Walk" && ground == null) ground = hits[i].point;
        }

        // trees and gather spots have no colliders: the closest one to the click on screen
        var resource = ResourceUnderMouse(cam);
        if (resource != null)
        {
            var chunk = resource.Field.GetComponentInParent<Chunk>();
            if (chunk != null) { Act(chunk.Type, null, chunk, resource, resource.Position, HeroMarker.Walk); return true; }
        }
        return false;
    }

    private void Act(TaskType task, string argument, Chunk chunk, object target, Vector3 at, Color marker)
    {
        if (chunk == null) return;
        player.InterruptAction();
        player.SetManualTask(task, argument, chunk, target);
        HeroMarker.Show(at, marker, task == TaskType.Fighting ? 1.8f : 2.2f);
    }

    /// <summary>Walks up to the NPC if needed and talks to it (a click, or E close by).</summary>
    public void Talk(IslandNpc npc)
    {
        HeroMarker.Show(npc.transform.position, HeroMarker.Walk, 1.6f);
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
