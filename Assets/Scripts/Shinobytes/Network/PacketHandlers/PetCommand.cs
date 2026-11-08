using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using RavenNest.Combat;
using RavenNest.Models;

/// <summary>
///     Battle pets from chat (docs/battle-pets-design.md). !pet stays the cosmetic pet; the bot sends "pet_command" for
///     !pets with the action first: !pets (the pet that is out), !pets stable, !pets use (name or number), !pets away,
///     !pets rename (name), !pets skills, !pets slot 2 howl, !pets evolve, !pets wake (a pet item you have; the one you
///     wear when left out), !pets release (name) confirm, !pets about (kind). The server keeps the stable; the answer
///     brings it back and the pet in the world follows.
/// </summary>
public class PetCommand : ChatBotCommandHandler<string>
{
    public PetCommand(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override async void Handle(string data, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var text = (data ?? "").Trim();
        var space = text.IndexOf(' ');
        var action = (space < 0 ? text : text.Substring(0, space)).ToLowerInvariant();
        var rest = space < 0 ? "" : text.Substring(space + 1).Trim();
        var pets = player.BattlePets;

        try
        {
            switch (action)
            {
                case "":
                case "info":
                    Info(player, gm, client);
                    return;
                case "stable":
                case "list":
                case "all":
                    Stable(player, gm, client);
                    return;
                case "use":
                case "out":
                case "send":
                case "summon":
                    await Use(player, rest, gm, client);
                    return;
                case "away":
                case "rest":
                case "dismiss":
                case "stop":
                    await Result(player, gm, client, await Game.RavenNest.Players.PetUseAsync(player.Id, Guid.Empty), r => client.SendReply(gm, Localization.MSG_PET_AWAY));
                    return;
                case "rename":
                case "name":
                    await Rename(player, rest, gm, client);
                    return;
                case "skills":
                case "skill":
                    Skills(player, gm, client);
                    return;
                case "slot":
                    await Slot(player, rest, gm, client);
                    return;
                case "evolve":
                    await Evolve(player, gm, client);
                    return;
                case "wake":
                case "awaken":
                    await Wake(player, rest, gm, client);
                    return;
                case "release":
                case "free":
                    await Release(player, rest, gm, client);
                    return;
                case "about":
                case "kind":
                case "species":
                    About(rest, gm, client);
                    return;
                default:
                    client.SendReply(gm, Localization.MSG_PET_USAGE);
                    return;
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("pet_command: " + exc);
            client.SendReply(gm, Localization.MSG_PET_ERROR);
        }
    }

    // ---------------------------------------------------------------- words

    public static string Strike(PetStrike s) => s == PetStrike.Bite ? "bites" : s == PetStrike.Pounce ? "pounces" : "fires bolts";
    public static string Element(Element e) => e == RavenNest.Combat.Element.Physical ? "no element" : e.ToString().ToLowerInvariant();
    public static string Role(PetRole r) => r.ToString().ToLowerInvariant();
    private static string Pct(double v) => (v * 100).ToString(v * 100 < 10 ? "0.#" : "0", CultureInfo.InvariantCulture) + "%";

    private static BattlePet Find(PlayerController player, string name)
    {
        var stable = player.BattlePets.Stable;
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (int.TryParse(name, out var n) && n >= 1 && n <= stable.Count) return stable[n - 1];
        return stable.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? stable.FirstOrDefault(p => p.Name != null && p.Name.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            ?? stable.FirstOrDefault(p => string.Equals(BattlePets.GetSpecies(p.Species)?.NameAt(p.Stage), name, StringComparison.OrdinalIgnoreCase));
    }

    private static string List(PlayerController player) =>
        string.Join(", ", player.BattlePets.Stable.Select((p, i) => (i + 1) + ". " + p.Name + " (" + (BattlePets.GetSpecies(p.Species)?.NameAt(p.Stage) ?? p.Species) + " " + p.Level + (p.Active ? ", out" : "") + ")"));

    private void Info(PlayerController player, GameMessage gm, GameClient client)
    {
        var pets = player.BattlePets;
        if (pets.Stable.Count == 0) { client.SendReply(gm, Localization.MSG_PET_NONE); return; }
        var pet = pets.Active;
        if (pet == null) { client.SendReply(gm, Localization.MSG_PET_NONE_OUT, List(player)); return; }
        var species = BattlePets.GetSpecies(pet.Species);
        if (species == null) { client.SendReply(gm, Localization.MSG_PET_ERROR); return; }
        var skills = string.Join(", ", (pet.Slots ?? "").Split(',').Select(BattlePets.GetSkill).Where(s => s != null).Select(s => s.Name));
        var taming = player.Stats.Taming.Level;
        var next = pet.Stage >= BattlePets.MaxStage ? Localization.MSG_PET_NEXT_DONE
            : string.Format(Localization.MSG_PET_NEXT_EVOLVE, species.NameAt(pet.Stage + 1), BattlePets.EvolveLevels[pet.Stage], BattlePets.EvolveSkillLevels[pet.Stage]);
        var c = pets.Controller;
        var max = c ? c.MaxHealth : species.Sheet(pet.Level, pet.Stage).MaxHealth;
        var health = c ? c.Health : max;
        client.SendReply(gm, Localization.MSG_PET_INFO, pet.Name, pet.Level, species.NameAt(pet.Stage), Role(species.Role), Element(species.Element), Strike(species.Strike),
            health, max, skills.Length > 0 ? skills : "none yet", next);
    }

    private void Stable(PlayerController player, GameMessage gm, GameClient client)
    {
        var pets = player.BattlePets;
        if (pets.Stable.Count == 0) { client.SendReply(gm, Localization.MSG_PET_NONE); return; }
        client.SendReply(gm, Localization.MSG_PET_STABLE, pets.Stable.Count, BattlePets.StableSize(player.Stats.Taming.Level), List(player));
    }

    private async Task Use(PlayerController player, string name, GameMessage gm, GameClient client)
    {
        var pet = Find(player, name);
        if (pet == null) { client.SendReply(gm, Localization.MSG_PET_NOT_FOUND, name); return; }
        var result = await Game.RavenNest.Players.PetUseAsync(player.Id, pet.Id);
        await Result(player, gm, client, result, r => client.SendReply(gm, Localization.MSG_PET_OUT, r.Pet?.Name ?? pet.Name));
    }

    private async Task Rename(PlayerController player, string name, GameMessage gm, GameClient client)
    {
        var pet = player.BattlePets.Active;
        if (pet == null) { client.SendReply(gm, Localization.MSG_PET_NONE_OUT, List(player)); return; }
        var result = await Game.RavenNest.Players.PetRenameAsync(player.Id, pet.Id, name);
        await Result(player, gm, client, result, r => client.SendReply(gm, Localization.MSG_PET_RENAMED, r.Pet?.Name));
    }

    private void Skills(PlayerController player, GameMessage gm, GameClient client)
    {
        var pet = player.BattlePets.Active;
        if (pet == null) { client.SendReply(gm, Localization.MSG_PET_NONE_OUT, List(player)); return; }
        var species = BattlePets.GetSpecies(pet.Species);
        var count = BattlePets.SkillSlots(player.Stats.Taming.Level, pet.Stage);
        var slots = (pet.Slots ?? "").Split(',');
        var shown = Enumerable.Range(0, count).Select(i => (i + 1) + ". " + (i < slots.Length ? BattlePets.GetSkill(slots[i])?.Name ?? "empty" : "empty"));
        var learned = species.SkillsAt(pet.Level, pet.Stage).Select(s => s.Name).ToList();
        var later = species.Skills.Select(BattlePets.GetSkill).Where(s => s != null && (s.LearnLevel > pet.Level || s.Stage > pet.Stage)).Select(s => s.Name + " (" + s.LearnLevel + ")").ToList();
        client.SendReply(gm, Localization.MSG_PET_SKILLS, pet.Name, string.Join(", ", shown), learned.Count > 0 ? string.Join(", ", learned) : "none", later.Count > 0 ? string.Join(", ", later) : "nothing");
    }

    private async Task Slot(PlayerController player, string rest, GameMessage gm, GameClient client)
    {
        var pet = player.BattlePets.Active;
        if (pet == null) { client.SendReply(gm, Localization.MSG_PET_NONE_OUT, List(player)); return; }
        var parts = rest.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !int.TryParse(parts[0], out var slot)) { client.SendReply(gm, Localization.MSG_PET_USAGE); return; }
        var skill = parts.Length > 1 ? parts[1] : "";
        var result = await Game.RavenNest.Players.PetSlotAsync(player.Id, pet.Id, slot - 1, skill);
        await Result(player, gm, client, result, r => client.SendReply(gm, Localization.MSG_PET_SLOT, BattlePets.FindSkill(skill)?.Name ?? "Nothing", slot));
    }

    private async Task Evolve(PlayerController player, GameMessage gm, GameClient client)
    {
        var pet = player.BattlePets.Active;
        if (pet == null) { client.SendReply(gm, Localization.MSG_PET_NONE_OUT, List(player)); return; }
        var species = BattlePets.GetSpecies(pet.Species);
        var old = species?.NameAt(pet.Stage) ?? pet.Name;
        var result = await Game.RavenNest.Players.PetEvolveAsync(player.Id, pet.Id);
        await Result(player, gm, client, result, r =>
        {
            Game.RavenBot?.Announce(Localization.MSG_PET_EVOLVED, player.PlayerName, old, species?.NameAt(r.Pet?.Stage ?? 0) ?? r.Pet?.Name);
        });
    }

    private async Task Wake(PlayerController player, string name, GameMessage gm, GameClient client)
    {
        GameInventoryItem item;
        if (string.IsNullOrWhiteSpace(name)) item = player.Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
        else
        {
            var owned = player.Inventory.GetInventoryItemsOfType(ItemCategory.Pet, ItemType.Pet);
            var equipped = player.Inventory.GetEquipmentOfCategory(ItemCategory.Pet);
            var all = owned.ToList();
            if (equipped != null) all.Add(equipped);
            item = all.FirstOrDefault(x => string.Equals(x.Item?.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? all.FirstOrDefault(x => x.Item?.Name != null && x.Item.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        if (item?.Item == null) { client.SendReply(gm, Localization.MSG_PET_NO_ITEM, string.IsNullOrWhiteSpace(name) ? "on" : name); return; }
        var species = BattlePets.SpeciesForItem(item.Item.GenericPrefab) ?? BattlePets.SpeciesForItem(item.Item.Name);
        if (species == null) { client.SendReply(gm, Localization.MSG_PET_NOT_A_PET, item.Item.Name); return; }

        var result = await Game.RavenNest.Players.PetWakeAsync(player.Id, item.Item.Id);
        if (result != null && result.Status == PetResultStatus.Ok && result.Pet != null && !result.Pet.Active)
        {
            // woken pets go straight out
            player.BattlePets.Apply(result);
            result = await Game.RavenNest.Players.PetUseAsync(player.Id, result.Pet.Id);
        }
        await Result(player, gm, client, result, r => client.SendReply(gm, Localization.MSG_PET_WOKEN, item.Item.Name, species.NameAt(0), Role(species.Role), Strike(species.Strike)));
    }

    private async Task Release(PlayerController player, string rest, GameMessage gm, GameClient client)
    {
        var confirm = rest.EndsWith(" confirm", StringComparison.OrdinalIgnoreCase);
        var name = confirm ? rest.Substring(0, rest.Length - 8).Trim() : rest;
        var pet = Find(player, name);
        if (pet == null) { client.SendReply(gm, Localization.MSG_PET_NOT_FOUND, name); return; }
        if (!confirm) { client.SendReply(gm, Localization.MSG_PET_RELEASE_CONFIRM, pet.Name); return; }
        var result = await Game.RavenNest.Players.PetReleaseAsync(player.Id, pet.Id);
        await Result(player, gm, client, result, r => client.SendReply(gm, Localization.MSG_PET_RELEASED, pet.Name));
    }

    private static void About(string name, GameMessage gm, GameClient client)
    {
        var s = BattlePets.Species.FirstOrDefault(x => x.StageNames.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) || string.Equals(x.Id, name, StringComparison.OrdinalIgnoreCase))
            ?? BattlePets.SpeciesForCreature(null, name)
            ?? BattlePets.Species.FirstOrDefault(x => x.StageNames.Any(n => n.IndexOf(name ?? "", StringComparison.OrdinalIgnoreCase) >= 0) && !string.IsNullOrWhiteSpace(name));
        if (s == null) { client.SendReply(gm, Localization.MSG_PET_UNKNOWN_KIND, name); return; }
        var how = s.Source == PetSource.Tamed ? string.Format(Localization.MSG_PET_ABOUT_TAME, s.TameLevel) : Localization.MSG_PET_ABOUT_ITEM;
        client.SendReply(gm, Localization.MSG_PET_ABOUT, s.NameAt(0), Role(s.Role), Element(s.Element), Strike(s.Strike), s.Blurb, how, string.Join(", ", s.StageNames));
    }

    /// <summary>Applies a command's answer to the stable and replies: <paramref name="ok"/> on success, the reason otherwise.</summary>
    private static Task Result(PlayerController player, GameMessage gm, GameClient client, PetResult result, Action<PetResult> ok)
    {
        if (result == null) { client.SendReply(gm, Localization.MSG_PET_ERROR); return Task.CompletedTask; }
        if (result.Pets != null || result.Pet != null) player.BattlePets.Apply(result);
        var name = result.Pet?.Name ?? "";
        switch (result.Status)
        {
            case PetResultStatus.Ok: ok(result); break;
            case PetResultStatus.NoSuchPet: client.SendReply(gm, Localization.MSG_PET_NOT_FOUND, name); break;
            case PetResultStatus.CannotEvolve: client.SendReply(gm, Localization.MSG_PET_CANNOT_EVOLVE, name, result.Missing); break;
            case PetResultStatus.AlreadyAwake: client.SendReply(gm, Localization.MSG_PET_ALREADY_AWAKE, name); break;
            case PetResultStatus.ItemMissing: client.SendReply(gm, Localization.MSG_PET_ITEM_MISSING, name); break;
            case PetResultStatus.NoSuchItem: client.SendReply(gm, Localization.MSG_PET_NO_ITEM, name); break;
            case PetResultStatus.NotAPetItem: client.SendReply(gm, Localization.MSG_PET_NOT_A_PET, name); break;
            case PetResultStatus.StableFull: client.SendReply(gm, Localization.MSG_PET_STABLE_FULL, name, result.Missing); break;
            case PetResultStatus.BadName: client.SendReply(gm, Localization.MSG_PET_BAD_NAME); break;
            case PetResultStatus.NoSuchSkill: client.SendReply(gm, Localization.MSG_PET_NO_SKILL, player.BattlePets.Active?.Name ?? "Your pet", "that"); break;
            case PetResultStatus.NotLearned:
                {
                    var parts = (result.Missing ?? ":").Split(':');
                    client.SendReply(gm, Localization.MSG_PET_NOT_LEARNED, player.BattlePets.Active?.Name ?? "Your pet", parts[0], parts.Length > 1 ? parts[1] : "?");
                    break;
                }
            case PetResultStatus.NoSuchSlot: client.SendReply(gm, Localization.MSG_PET_NO_SLOT, player.BattlePets.Active?.Name ?? "Your pet", result.Missing); break;
            default: client.SendReply(gm, Localization.MSG_PET_ERROR); break;
        }
        return Task.CompletedTask;
    }
}

/// <summary>
///     !tame (creature) from chat (docs/battle-pets-design.md, section 2): each defeat of that kind of creature is a try
///     while the player fights; !tame alone says what is being tamed, !tame stop ends it.
/// </summary>
public class TameCommand : ChatBotCommandHandler<string>
{
    public TameCommand(GameManager game, RavenBotConnection server, PlayerManager playerManager)
        : base(game, server, playerManager)
    {
    }

    public override void Handle(string data, GameMessage gm, GameClient client)
    {
        var player = PlayerManager.GetPlayer(gm.Sender);
        if (!player)
        {
            client.SendReply(gm, Localization.MSG_NOT_PLAYING);
            return;
        }

        var text = (data ?? "").Trim();
        var pets = player.BattlePets;
        var level = player.Stats.Taming.Level;
        if (text.Length == 0)
        {
            if (pets.Taming == null) client.SendReply(gm, Localization.MSG_TAME_NOTHING);
            else client.SendReply(gm, Localization.MSG_TAME_STATUS, pets.Taming.NameAt(0), ChanceText(level, pets.Taming), level);
            return;
        }
        if (text.Equals("stop", StringComparison.OrdinalIgnoreCase) || text.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            pets.StopTaming();
            client.SendReply(gm, Localization.MSG_TAME_STOPPED);
            return;
        }

        var species = Find(text);
        var problem = pets.StartTaming(species, text);
        if (problem != null) { client.SendReply(gm, Localization.MSG_TAME_CANNOT, problem); return; }
        client.SendReply(gm, player.ActiveSkill.IsCombatSkill() ? Localization.MSG_TAME_STARTED : Localization.MSG_TAME_NOT_FIGHTING,
            species.NameAt(0), ChanceText(level, species), level);
    }

    private static string ChanceText(int level, PetSpecies species)
    {
        var c = Taming.Chance(level, species) * 100;
        return c.ToString(c < 1 ? "0.##" : "0.#", CultureInfo.InvariantCulture) + "%";
    }

    /// <summary>A creature by what people call it: "bunny", "bunnies", "metalon", "red dragon", "quackers".</summary>
    private static PetSpecies Find(string text)
    {
        var t = text.Trim().ToLowerInvariant();
        if (t.EndsWith("ies")) t = t.Substring(0, t.Length - 3) + "y";
        else if (t.EndsWith("s") && t.Length > 3) t = t.Substring(0, t.Length - 1);
        foreach (var s in BattlePets.Tameable)
        {
            if (s.Id == t || s.Id.Replace('-', ' ') == t) return s;
            foreach (var c in s.Creatures) if (c.ToLowerInvariant() == t) return s;
            foreach (var n in s.StageNames) if (n.ToLowerInvariant() == t) return s;
        }
        foreach (var s in BattlePets.Tameable)
        {
            foreach (var c in s.Creatures) if (c.ToLowerInvariant().Contains(t)) return s;
            if (s.Id.Contains(t)) return s;
        }
        return null;
    }
}
