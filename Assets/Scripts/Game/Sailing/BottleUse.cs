using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RavenNest.Models;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// !use message in a bottle and !use treasure map (v0.9.7, docs/own-boats.md, "Messages in bottles"). A bottle comes
/// from fishing at sea. The server opens it: most of the time a note, now and then a Treasure Map that marks an island
/// the character can reach. On that island the map is dug up: the character digs where it stands, a little chest comes
/// up and the server's treasure is told in chat.
/// </summary>
/// <remarks>
/// The server decides everything (the note, the map and its island, whether the character is on it, the treasure) and
/// takes the item; the game only words the answer. The notes are numbers here, UI_BOTTLE_NOTE_n in Localization (the
/// server's Bottles.NoteCount ordinary ones and a few rare ones from 101), so they can be translated. Coins and items
/// come through the server's own pushes (server authority): nothing is added here.
/// </remarks>
public static class BottleUse
{
    public const string BottleName = "Message in a Bottle";
    public const string MapName = "Treasure Map";

    /// <summary>The rare notes that are not words to read (a drawing, a soaked note, a bottle in the bottle): no quotes round them.</summary>
    private static readonly HashSet<int> Pictures = new HashSet<int> { 101, 104, 105 };

    /// <summary>The note addressed to whoever found it: {player} in its text.</summary>
    private const string PlayerPlaceholder = "{player}";

    /// <summary>How long the character digs before the chest comes up and the reply is sent.</summary>
    public const float DigSeconds = 5f;

    private static readonly string[] BottleWords = { "bottle", "bottles", "message", "messages", "message in a bottle", "messages in a bottle", "message in bottle", "note", "a bottle" };
    private static readonly string[] MapWords = { "map", "maps", "treasure map", "treasure maps", "treasure", "a map", "the map", "treasuremap" };

    public static bool IsBottleQuery(string query) => Matches(query, BottleWords);
    public static bool IsMapQuery(string query) => Matches(query, MapWords);

    /// <summary>Whether an item is one of these two (found by its full name through the item resolver).</summary>
    public static bool Handles(Item item) =>
        item != null && (string.Equals(item.Name, BottleName, StringComparison.OrdinalIgnoreCase) || string.Equals(item.Name, MapName, StringComparison.OrdinalIgnoreCase));

    private static bool Matches(string query, string[] words)
    {
        if (string.IsNullOrWhiteSpace(query)) return false;
        var q = string.Join(" ", query.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        return Array.IndexOf(words, q) >= 0;
    }

    /// <summary>!use bottle, !use treasure map, or either found by its name.</summary>
    public static async System.Threading.Tasks.Task UseAsync(GameManager game, PlayerController player, bool map, GameMessage gm, GameClient client)
    {
        var name = map ? MapName : BottleName;
        var stacks = player.Inventory.GetBackpackItems()
            .Where(x => x != null && x.Item != null && x.InventoryItem != null && x.InventoryItem.Amount > 0 && string.Equals(x.Item.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (stacks.Count == 0)
        {
            client.SendReply(gm, map ? Localization.MSG_TREASURE_MAP_NONE : Localization.MSG_BOTTLE_NONE);
            return;
        }

        if (!map)
        {
            await OpenBottleAsync(game, player, stacks[0], gm, client);
            return;
        }

        if (Busy(player))
        {
            client.SendReply(gm, Localization.MSG_TREASURE_MAP_BUSY);
            return;
        }

        // more than one map: the one for the island the character is on, if there is one
        var here = player.Island ? player.Island.Island.ToString() : null;
        var stack = stacks.FirstOrDefault(s => string.Equals(s.InventoryItem.Tag, here, StringComparison.OrdinalIgnoreCase)) ?? stacks[0];
        await DigAsync(game, player, stack, gm, client);
    }

    /// <summary>Busy with something a dig cannot interrupt: the server only knows some of these (dungeons, raids, arena, onsen).</summary>
    private static bool Busy(PlayerController player)
    {
        return player.IsAway || player.InRavebridge || player.Stats.IsDead || player.Controlled
            || player.streamRaidHandler.InWar || player.arenaHandler.InArena || player.duelHandler.InDuel || player.raidHandler.InRaid
            || (player.onsenHandler && player.onsenHandler.InOnsen);
    }

    // ---- The bottle ----------------------------------------------------------------------------------------------

    private static async System.Threading.Tasks.Task OpenBottleAsync(GameManager game, PlayerController player, GameInventoryItem bottle, GameMessage gm, GameClient client)
    {
        var result = await game.RavenNest.Players.UseItemAsync(player.Id, bottle.InstanceId);
        if (result == null || result.InventoryItemId == Guid.Empty || string.IsNullOrEmpty(result.Outcome))
        {
            client.SendReply(gm, Localization.MSG_USE_ITEM_NOT_NOW, BottleName);
            return;
        }

        if (!player.Inventory.FedByServer) player.Inventory.UpdateInventoryItem(result.InventoryItemId, result.NewStackAmount); // else the feed carries it

        if (result.Outcome == "map")
        {
            client.SendReply(gm, Localization.MSG_BOTTLE_MAP, IslandName(result.EffectIsland));
            return;
        }

        // April 1st: a prankster's note instead (docs/easter-eggs.md); the server's roll is only worded differently
        var april = SillySurprises.AprilBottleNote();
        var note = (april ?? Note(result.Note)).Replace(PlayerPlaceholder, player.Name);
        client.SendReply(gm, april == null && Pictures.Contains(result.Note) ? Localization.MSG_BOTTLE_PICTURE : Localization.MSG_BOTTLE_NOTE, note);
    }

    /// <summary>A note's words by its number; one this game does not have yet reads as the first.</summary>
    public static string Note(int number)
    {
        var field = typeof(Localization).GetField("UI_BOTTLE_NOTE_" + number, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        return field?.GetValue(null) as string ?? Localization.UI_BOTTLE_NOTE_1;
    }

    // ---- The map -------------------------------------------------------------------------------------------------

    private static async System.Threading.Tasks.Task DigAsync(GameManager game, PlayerController player, GameInventoryItem map, GameMessage gm, GameClient client)
    {
        var result = await game.RavenNest.Players.UseItemAsync(player.Id, map.InstanceId);
        if (result == null || result.InventoryItemId == Guid.Empty || string.IsNullOrEmpty(result.Outcome))
        {
            client.SendReply(gm, Localization.MSG_USE_ITEM_NOT_NOW, MapName);
            return;
        }

        var island = IslandName(result.EffectIsland);
        if (result.Outcome == "elsewhere")
        {
            client.SendReply(gm, Localization.MSG_TREASURE_MAP_ELSEWHERE, island);
            return;
        }
        if (result.Outcome == "locked")
        {
            client.SendReply(gm, Localization.MSG_TREASURE_MAP_LOCKED, island);
            return;
        }
        if (result.Outcome != "treasure")
        {
            client.SendReply(gm, Localization.MSG_USE_ITEM_NOT_NOW, MapName);
            return;
        }

        if (!player.Inventory.FedByServer) player.Inventory.UpdateInventoryItem(result.InventoryItemId, result.NewStackAmount);

        // the server has given it all already; the dig is for the eyes, and the reply comes when the chest is up
        TreasureDig.Begin(player);
        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(DigSeconds));
        client.SendReply(gm, Localization.MSG_TREASURE_MAP_DUG, island, Found(game, result));
    }

    /// <summary>"12,390 coins, a Flawed Ruby and a Bomb Egg": what the treasure held.</summary>
    public static string Found(GameManager game, ItemUseResult result)
    {
        var parts = new List<string>();
        if (result.Coins > 0) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_TREASURE_MAP_COINS, result.Coins.ToString("N0", CultureInfo.InvariantCulture)));
        if (result.Items != null)
        {
            foreach (var reward in result.Items)
            {
                var item = game.Items.Get(reward.ItemId);
                if (item == null || string.IsNullOrEmpty(item.Name)) continue;
                var article = Utility.IsVocal(char.ToLowerInvariant(item.Name[0])) ? Localization.UI_TREASURE_MAP_ITEM_AN : Localization.UI_TREASURE_MAP_ITEM_A;
                parts.Add(string.Format(CultureInfo.InvariantCulture, article, item.Name));
            }
        }
        if (!string.IsNullOrEmpty(result.Look)) parts.Add(string.Format(CultureInfo.InvariantCulture, Localization.UI_TREASURE_MAP_LOOK, result.Look.Replace('_', ' ')));
        if (parts.Count == 0) return Localization.UI_TREASURE_MAP_NOTHING;
        if (parts.Count == 1) return parts[0];
        return string.Join(", ", parts.Take(parts.Count - 1)) + Localization.UI_TREASURE_MAP_AND + parts[parts.Count - 1];
    }

    private static string IslandName(Island island) => island.ToString();
}

/// <summary>
/// A Treasure Map dug up (BottleUse): the character digs with a shovel a step in front of where it stands, as the
/// banana island's diggers do (TreasureHunt), then a little chest comes up out of the ground, opens, and goes again.
/// The chest is the banana island's (Resources/Mangrove/Treasure Chest); without it there is only the digging.
/// </summary>
public sealed class TreasureDig : MonoBehaviour
{
    private const float ChestSeconds = 14f;
    private const float LidOpen = -110f;

    private PlayerController player;
    private IslandController island;
    private Vector3 spot;
    private float startedAt;
    private bool digging;
    private bool dug;
    private GameObject chest;
    private Transform lid;
    private float rise;

    public static void Begin(PlayerController player)
    {
        if (!player || !player.Island) return;
        var forward = player.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        var at = player.Position + forward.normalized * 1.3f;
        if (NavMesh.SamplePosition(at, out var hit, 1.5f, NavMesh.AllAreas)) at = hit.position;

        var go = new GameObject("Treasure Dig");
        go.transform.SetParent(player.Island.transform, true);
        go.transform.position = at;
        var dig = go.AddComponent<TreasureDig>();
        dig.player = player;
        dig.island = player.Island;
        dig.spot = at;
        dig.startedAt = Time.time;
        player.BeginErrand(dig.Dig);
    }

    /// <summary>The digger's errand: dig until the time is up, then the task carries on.</summary>
    private bool Dig(PlayerController p)
    {
        if (!this || dug || p.Island != island || Time.time - startedAt >= BottleUse.DigSeconds)
        {
            StopDigging();
            return false;
        }

        if (!digging)
        {
            digging = true;
            if (p.agent && p.agent.isOnNavMesh) p.agent.ResetPath();
            var look = spot - p.Position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) p.transform.rotation = Quaternion.LookRotation(look);
            p.Animations.StartFarming();
            p.Equipment.ShowShovel();
        }
        return true;
    }

    private void StopDigging()
    {
        if (!digging) return;
        digging = false;
        if (player)
        {
            player.Animations.EndFarming();
            player.Equipment.HideShovel();
        }
    }

    private void Update()
    {
        if (!dug)
        {
            if (Time.time - startedAt < BottleUse.DigSeconds) return;
            dug = true;
            StopDigging();
            if (player && player.Island == island) player.Cheer();
            MakeChest();
            Destroy(gameObject, ChestSeconds);
            return;
        }

        if (chest)
        {
            // up out of the ground first, then the lid swings back on its hinge (as the banana island's)
            rise = Mathf.MoveTowards(rise, 2f, Time.deltaTime * 0.8f);
            chest.transform.position = spot + Vector3.up * Mathf.Lerp(-1.2f, 0.02f, Mathf.SmoothStep(0f, 1f, rise));
            if (lid) lid.localRotation = Quaternion.Euler(Mathf.SmoothStep(0f, LidOpen, rise - 1f), 0f, 0f);
        }
    }

    private void MakeChest()
    {
        var prefab = UnityEngine.Resources.Load<GameObject>("Mangrove/Treasure Chest");
        if (!prefab) return;
        chest = Instantiate(prefab, spot + Vector3.down * 1.2f, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f), transform);
        foreach (var t in chest.GetComponentsInChildren<Transform>()) if (t.name.StartsWith("SM_Prop_Chest_Lid")) lid = t;
        rise = 0f;
    }

    private void OnDestroy()
    {
        StopDigging();
        if (chest) Destroy(chest);
    }
}
