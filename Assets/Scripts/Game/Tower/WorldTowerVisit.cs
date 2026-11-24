using System;
using System.Collections.Generic;
using RavenNest.Models;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
///     The islet at the foot of the world tower as a place players go (docs/world-tower.md, "Visiting"): an island
///     like the others for sailing (the ferry stops at its pier when someone is bound for it or waiting there, own
///     boats have a dock on the sea chart, <c>!sail tower</c>), kept in the tower's own scene and made known to the
///     game when that scene loads, so MainWorld holds nothing of it.
///     <para>It is a calm place: nothing to train. A player there takes a place on the plaza and goes on learning the
///     skill they train at the rate they learned on their island (<see cref="PlayerProgression.TrainAway"/>, the
///     owner's rule that nobody loses training time; the tower's towns and Ravebridge do the same), and the server
///     goes on giving what they were gathering.</para>
///     <para><c>!tower</c> from the plaza: the player walks up the stair and in through the gate first, and the
///     command runs when they are inside the tunnel (<see cref="WalksInFirst"/>).</para>
/// </summary>
public sealed partial class WorldTower
{
    /// <summary>The islet's value among the islands: RavenNest.Models.Island.Tower, which the game's older library has no name for.</summary>
    public static readonly Island IslandValue = (Island)11;

    [Header("The islet as a place")]
    [Tooltip("The islet's island: made known to the game's islands when this scene loads.")]
    public IslandController Islet;
    [Tooltip("The ferry's way to the pier and on from it, and the path of its ring the stop comes after (3: it has just reached Heim).")]
    public SplineContainer FerryTo;
    public SplineContainer FerryFrom;
    public int FerryAfterPath = 3;
    [Tooltip("Where the ferry is bound when it leaves the pier.")]
    public string FerryNext = "Atria";
    [Tooltip("The mouth of the gate at the head of the stair, and the far end of its tunnel.")]
    public Transform Gate;
    public Transform TunnelEnd;
    [Tooltip("Places to stand on the plaza.")]
    public Transform[] Spots = new Transform[0];

    private sealed class Walk
    {
        public Action Again;
        public float GiveUp;
        public bool PastGate;
    }

    private bool placed;
    private PathSelector ferry;
    private float nextVisitors;
    private readonly Dictionary<PlayerController, Walk> walking = new Dictionary<PlayerController, Walk>();
    private readonly Dictionary<PlayerController, float> inside = new Dictionary<PlayerController, float>();
    private readonly Dictionary<PlayerController, int> standing = new Dictionary<PlayerController, int>();
    private readonly Dictionary<PlayerController, float> welcomed = new Dictionary<PlayerController, float>();
    private readonly List<PlayerController> scratch = new List<PlayerController>();

    /// <summary>The player stands on the islet at the tower's foot: a calm place with nothing to train.</summary>
    public static bool Calm(PlayerController player)
    {
        var tower = Instance;
        return tower && tower.Islet && player && player.Island == tower.Islet;
    }

    // the islet joins the game's islands and the ferry's ring once the game's own are there
    private void EnterPlace()
    {
        var game = GameManager.Instance;
        if (!game || game.Islands == null || !Islet) return;
        game.Islands.Register(Islet);
        ferry = FindAnyObjectByType<PathSelector>();
        if (ferry && FerryTo && FerryFrom) ferry.RegisterStop(FerryAfterPath, FerryTo, FerryFrom, Islet.Identifier, FerryNext, FerryWanted);
        placed = true;
    }

    private void LeavePlace()
    {
        if (!placed) return;
        placed = false;
        if (ferry) ferry.UnregisterStop(FerryTo);
        var game = GameManager.Instance;
        if (game && game.Islands != null && Islet) game.Islands.Unregister(Islet);
    }

    /// <summary>The ferry turns in to the pier only for someone: a passenger bound for the islet, or a player waiting on it.</summary>
    private bool FerryWanted()
    {
        var game = GameManager.Instance;
        if (!game || !Islet) return false;
        foreach (var p in game.Players.GetAllPlayers())
        {
            if (!p || p.ferryHandler == null) continue;
            if (p.ferryHandler.OnFerry && p.ferryHandler.Destination == Islet) return true;
            if (p.Island == Islet && p.ferryHandler.Embarking) return true;
        }
        return false;
    }

    /// <summary>
    ///     A player who has nothing to train here (PlayerController.GotoClosest on the islet): to a place on the plaza,
    ///     and a word about where they are, now and then.
    /// </summary>
    public static void Settle(PlayerController player, bool silent)
    {
        var tower = Instance;
        if (!tower || !player) return;
        tower.Stand(player);
        if (silent || player.IsBot) return;
        if (tower.welcomed.TryGetValue(player, out var last) && Time.time - last < 300f) return;
        tower.welcomed[player] = Time.time;
        var game = GameManager.Instance;
        if (game && game.RavenBot != null) game.RavenBot.SendReply(player, Localization.MSG_TOWER_ISLET);
    }

    private void Stand(PlayerController player)
    {
        if (Spots == null || Spots.Length == 0 || !player || player.Movement == null) return;
        if (!standing.TryGetValue(player, out var index) || index >= Spots.Length) standing[player] = index = UnityEngine.Random.Range(0, Spots.Length);
        var spot = Spots[index];
        if (!spot) return;
        player.Movement.Unlock();
        player.SetDestination(spot.position);
    }

    /// <summary>
    ///     !tower from the plaza: true when the player first walks in through the gate (TowerCommand then does nothing
    ///     yet); <paramref name="again"/> runs the command once they are inside the tunnel, or after a while if they
    ///     cannot get there. False for anyone not on the islet, for commands that take nobody into the tower, and for
    ///     the player who has just walked in.
    /// </summary>
    public static bool WalksInFirst(PlayerController player, string action, Action again)
    {
        var tower = Instance;
        if (!tower || !Calm(player) || !tower.Gate || !tower.TunnelEnd || again == null) return false;
        if (player.towerHandler == null || player.towerHandler.InTower) return false;
        if (player.ferryHandler != null && player.ferryHandler.Active) return false;
        switch (action)
        {
            case "leave": case "exit": case "boss": case "challenge": case "climb": case "up": case "continue": case "next":
            case "close": case "quit": case "end": case "stop": case "info": case "status": case "help": case "commands": case "?":
            case "map": case "talk": case "speak": case "all": case "everyone": case "group":
                return false;
        }
        if (tower.inside.TryGetValue(player, out var until) && Time.time < until) return false;
        if (tower.walking.TryGetValue(player, out var walk))
        {
            walk.Again = again;
            return true;
        }
        tower.walking[player] = new Walk { Again = again, GiveUp = Time.time + 45f };
        player.Movement.Unlock();
        player.SetDestination(tower.Gate.position);
        return true;
    }

    /// <summary>Every frame: those walking in; now and then: those standing about.</summary>
    private void Visitors()
    {
        if (walking.Count > 0)
        {
            scratch.Clear();
            foreach (var pair in walking)
            {
                var player = pair.Key;
                var walk = pair.Value;
                if (!player || player.isDestroyed || !Calm(player) || (player.towerHandler != null && player.towerHandler.InTower)) { scratch.Add(player); continue; }
                var position = player.transform.position;
                if (!walk.PastGate && Flat(position, Gate.position) < 3f)
                {
                    walk.PastGate = true;
                    player.Movement.Unlock();
                    player.SetDestination(TunnelEnd.position);
                }
                else if ((walk.PastGate && Flat(position, TunnelEnd.position) < 4f) || Time.time > walk.GiveUp)
                {
                    scratch.Add(player);
                    // inside: the command goes ahead (and for a little while any other !tower does too)
                    inside[player] = Time.time + 12f;
                    try { walk.Again(); }
                    catch (Exception exc) { Shinobytes.Debug.LogError("WorldTower: the walk in could not finish the command: " + exc.Message); }
                }
            }
            foreach (var player in scratch) walking.Remove(player);
        }

        if (Time.time < nextVisitors) return;
        nextVisitors = Time.time + 2.5f;
        var game = GameManager.Instance;
        if (!placed || !game || !Islet) return;
        foreach (var player in game.Players.GetAllPlayers())
        {
            // someone standing where they should not be for long (in the tunnel after a join that did not go through,
            // back from the tower, pushed off their place): back to a place on the plaza
            if (!Calm(player) || walking.ContainsKey(player) || player.Movement == null || player.Movement.IsMoving) continue;
            if (player.ferryHandler != null && player.ferryHandler.Active) continue;
            if (player.towerHandler != null && player.towerHandler.InTower) continue;
            if (inside.TryGetValue(player, out var until) && Time.time < until) continue;
            if (!standing.TryGetValue(player, out var index) || index >= Spots.Length || !Spots[index] || Flat(player.transform.position, Spots[index].position) > 5f) Stand(player);
        }
    }

    private static float Flat(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
