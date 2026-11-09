using System;
using System.Collections.Generic;
using System.Linq;
using RavenNest.Combat;
using RavenNest.Models;
using UnityEngine;

/// <summary>
///     A player's battle pets in the game (docs/battle-pets-design.md): the stable as the server sends it, the pet
///     that is out (a <see cref="BattlePetController"/> in the world), taming (!tame: each defeat of that kind of
///     creature is a try, Taming experience every time, the chance from the library, the pet made by the server), the
///     share of experience a pet and Taming get while a pet is out, and the blows a tank pet takes for its owner. The
///     cosmetic pet stays hidden while a battle pet is out, so there is one animal at the owner's heels. Eggs in the
///     stable (pets at level 0) are reported once a minute while their owner plays, and hatch on the server.
/// </summary>
public sealed class PlayerBattlePets
{
    /// <summary>Pet experience is sent to the server this often (when there is any).</summary>
    private const float ReportEvery = 60f;

    /// <summary>A tame is announced at most this often, whoever tamed it, so a busy stream's chat is not flooded.</summary>
    private const float AnnounceEvery = 8f;
    private static float nextAnnounce;

    private readonly PlayerController owner;
    private readonly List<BattlePet> stable = new List<BattlePet>();
    private double pendingExperience;
    private Guid pendingPet; // the pet that earned the pending experience
    private float nextReport, nextSteer, nextHide, nextIncubate;
    private bool cosmeticHidden;

    public PlayerBattlePets(PlayerController owner) => this.owner = owner;

    public IReadOnlyList<BattlePet> Stable => stable;
    public BattlePet Active => stable.FirstOrDefault(p => p.Active && !PetEggs.IsUnhatched(p.Level));

    /// <summary>The owner's clan's Rookery level as the game knows it (0 without a clan or the skill).</summary>
    public int RookeryLevel => owner && owner.clanHandler && owner.clanHandler.InClan
        ? owner.clanHandler.ClanInfo?.ClanSkills?.FirstOrDefault(s => s != null && s.Name == Rookery.Name)?.Level ?? 0 : 0;

    /// <summary>The minutes of play an egg in the stable still needs (as the game knows the Rookery).</summary>
    public int MinutesLeft(BattlePet egg)
    {
        var species = egg != null ? BattlePets.GetSpecies(egg.Species) : null;
        if (species == null) return 0;
        return Math.Max(1, (int)Math.Ceiling(PetEggs.HatchMinutes(species, RookeryLevel) - egg.PlayTime.TotalMinutes));
    }
    public BattlePetController Controller { get; private set; }

    /// <summary>The kind being tamed (!tame), or null.</summary>
    public PetSpecies Taming { get; private set; }

    public void Load(IEnumerable<BattlePet> pets)
    {
        stable.Clear();
        if (pets != null) stable.AddRange(pets.Where(p => p != null && !string.IsNullOrEmpty(p.Species)));
        Sync();
    }

    /// <summary>A command's answer: the stable after the change.</summary>
    /// <summary>The stable's places as the server last said (Taming and the clan's Rookery); 0 before it said.</summary>
    public int ServerStableSize { get; private set; }

    /// <summary>
    ///     An experience report's answer: only the pet's numbers. The whole pet would bring back its Active flag as it was
    ///     when the report left, and a pet sent away meanwhile would come back out.
    /// </summary>
    private void ApplyExperience(PetResult result)
    {
        if (result == null) return;
        if (result.StableSize > 0) ServerStableSize = result.StableSize;
        var pet = result.Pet;
        if (pet == null) return;
        var mine = stable.FirstOrDefault(p => p.Id == pet.Id);
        if (mine == null) return;
        mine.Level = pet.Level;
        mine.Experience = pet.Experience;
        mine.Stage = pet.Stage;
        mine.Slots = pet.Slots;
        if (Controller && Controller.Data != null && Controller.Data.Id == mine.Id) Controller.Refresh(mine);
    }

    public void Apply(PetResult result)
    {
        if (result == null) return;
        if (result.StableSize > 0) ServerStableSize = result.StableSize;
        if (result.Pets != null) Load(result.Pets);
        else if (result.Pet != null)
        {
            var i = stable.FindIndex(p => p.Id == result.Pet.Id);
            if (i >= 0) stable[i] = result.Pet; else stable.Add(result.Pet);
            Sync();
        }
    }

    /// <summary>For bots and tests without a server: a pet that lives only in this game.</summary>
    public BattlePet AddLocal(string speciesId, int level = 1, int stage = 0, string prefab = null)
    {
        var species = BattlePets.GetSpecies(speciesId);
        if (species == null) return null;
        var slots = species.SkillsAt(level, stage).Select(s => s.Id).Take(BattlePets.SkillSlots(owner.Stats?.Taming?.Level ?? 1, stage)).ToArray();
        foreach (var p in stable) p.Active = false;
        var pet = new BattlePet
        {
            Id = Guid.NewGuid(), CharacterId = owner.Id, Name = species.NameAt(stage), Level = level, Stage = stage,
            Species = species.Id, Source = (int)species.Source, Slots = string.Join(",", slots), Active = true, Prefab = prefab ?? species.ModelAt(stage),
        };
        stable.Add(pet);
        Sync();
        return pet;
    }

    /// <summary>The pet in the world follows the stable: the active one out, the right one, with the server's numbers.</summary>
    private void Sync()
    {
        var active = Active;
        if (active == null)
        {
            if (Controller) UnityEngine.Object.Destroy(Controller.gameObject);
            Controller = null;
            return;
        }
        if (Controller && Controller.Data != null && Controller.Data.Id == active.Id)
        {
            Controller.Refresh(active);
            return;
        }
        if (Controller) UnityEngine.Object.Destroy(Controller.gameObject);
        Controller = BattlePetController.Spawn(owner, active);
    }

    /// <summary>The owner leaves the game: the pet's unsent experience is sent (the owner is gone by the answer), and the pet goes.</summary>
    public void Leave()
    {
        var pet = pendingPet;
        var amount = pendingExperience;
        pendingExperience = 0;
        var game = owner ? owner.GameManager : null;
        if (pet != Guid.Empty && amount > 0 && game && !owner.IsBot)
        {
            var characterId = owner.Id;
            _ = game.RavenNest.Players.PetExperienceAsync(characterId, pet, amount);
        }
        Remove();
    }

    public void Remove()
    {
        if (Controller) UnityEngine.Object.Destroy(Controller.gameObject);
        Controller = null;
        ShowCosmetic(true);
    }

    // ---------------------------------------------------------------- every frame (cheap)

    public void Tick()
    {
        var now = Time.time;
        if (now >= nextHide)
        {
            nextHide = now + 1f;
            ShowCosmetic(!Controller || !BattlePetController.Shown);
            if (Active != null && !Controller) Sync(); // a model that could not be made, or the player came back
        }

        if (pendingExperience > 0 && now >= nextReport && Controller && !owner.IsBot) Report();

        if (now >= nextIncubate)
        {
            nextIncubate = now + ReportEvery;
            if (!owner.IsBot && stable.Any(p => PetEggs.IsUnhatched(p.Level))) Incubate();
        }

        if (Taming != null && now >= nextSteer)
        {
            nextSteer = now + 10f;
            Steer();
        }
    }

    /// <summary>
    ///     One animal at the owner's heels: the cosmetic pet hides while a battle pet is drawn. Checked every second,
    ///     not only when that changes: the cosmetic pet is an equipment model, made again whenever the equipment
    ///     refreshes (the inventory feed), and a new one came in visible beside the battle pet.
    /// </summary>
    private void ShowCosmetic(bool show)
    {
        var changed = cosmeticHidden == show;
        cosmeticHidden = !show;
        if (show && !changed) return; // shown and left alone: nothing of ours to undo
        foreach (var pet in owner.GetComponentsInChildren<PetController>(true))
        {
            pet.HiddenByBattlePet = !show;
            foreach (var r in pet.GetComponentsInChildren<Renderer>(true))
            {
                if (r.enabled != show) r.enabled = show;
            }
        }
    }

    // ---------------------------------------------------------------- experience

    /// <summary>Experience the owner earned in a skill: while a pet is out, the pet and Taming get a share.</summary>
    public void Earned(RavenNest.Models.Skill skill, double experience)
    {
        if (!Controller || experience <= 0 || skill == RavenNest.Models.Skill.Taming) return;
        var share = experience * BattlePets.ExperienceShare;
        var earner = Controller.Data.Id;
        if (earner != pendingPet)
        {
            // another pet is out now: what the last one earned is sent first, for that one
            if (pendingExperience > 0 && !owner.IsBot) Report();
            pendingExperience = 0;
            pendingPet = earner;
        }
        pendingExperience += share;
        var taming = owner.Stats?.Taming;
        if (taming != null && taming.AddExp(share, out var levels)) owner.CelebrateSkillLevelUp(RavenNest.Models.Skill.Taming, levels);
    }

    private async void Report()
    {
        var pet = pendingPet;
        if (pet == Guid.Empty) return;
        var amount = pendingExperience;
        pendingExperience = 0;
        nextReport = Time.time + ReportEvery;
        try
        {
            var result = await owner.GameManager.RavenNest.Players.PetExperienceAsync(owner.Id, pet, amount);
            if (result == null || result.Status != PetResultStatus.Ok) { if (result == null && pet == pendingPet) pendingExperience += amount; return; }
            ApplyExperience(result);
            if (result.LevelsGained > 0 && Controller && Controller.Data != null && Controller.Data.Id == pet)
            {
                Controller.ShowText(string.Format(Localization.MSG_PET_LEVEL_SHORT, result.Pet.Level));
                PetFx.Burst(Controller.transform.position, Mathf.Max(0.5f, Controller.Height), PetFx.ElementColor(Controller.Species.Element));
            }
        }
        catch (Exception exc)
        {
            if (pet == pendingPet) pendingExperience += amount;
            Shinobytes.Debug.LogError("Pet experience for " + owner.PlayerName + ": " + exc.Message);
        }
    }

    // ---------------------------------------------------------------- eggs

    /// <summary>The eggs' minute: the server warms them and hatches the ready ones; a hatch is a little moment.</summary>
    private async void Incubate()
    {
        try
        {
            var eggs = stable.Where(p => PetEggs.IsUnhatched(p.Level)).Select(p => p.Id).ToList();
            var result = await owner.GameManager.RavenNest.Players.PetIncubateAsync(owner.Id);
            if (!owner || owner.Removed || result == null || result.Status != PetResultStatus.Ok) return;
            Apply(result);
            foreach (var id in eggs)
            {
                var pet = stable.FirstOrDefault(p => p.Id == id);
                if (pet == null || PetEggs.IsUnhatched(pet.Level)) continue;
                Hatched(pet);
            }
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Pet eggs for " + owner.PlayerName + ": " + exc.Message);
        }
    }

    private void Hatched(BattlePet pet)
    {
        var species = BattlePets.GetSpecies(pet.Species);
        if (species == null) return;
        var where = Controller && Controller.Data != null && Controller.Data.Id == pet.Id ? Controller.transform.position : owner.Position;
        PetFx.Burst(where, 1.2f, PetFx.ElementColor(species.Element));
        if (Time.time < nextAnnounce || owner.GameManager?.RavenBot == null) return;
        nextAnnounce = Time.time + AnnounceEvery;
        owner.GameManager.RavenBot.Announce(Localization.MSG_PET_HATCHED, owner.PlayerName, species.EggName, species.NameAt(0));
    }

    // ---------------------------------------------------------------- taming

    /// <summary>Starts taming a kind of creature, or says why not.</summary>
    public string StartTaming(PetSpecies species, string asked)
    {
        var level = owner.Stats?.Taming?.Level ?? 1;
        var problem = RavenNest.Combat.Taming.CannotTame(level, species, asked);
        if (problem != null) return problem;
        Taming = species;
        nextSteer = 0f;
        Steer();
        return null;
    }

    public void StopTaming() => Taming = null;

    /// <summary>Whether an enemy is of the kind being tamed.</summary>
    public bool IsTamingTarget(EnemyController e)
    {
        if (Taming == null || !e) return false;
        var d = e.CombatDefinition;
        var species = BattlePets.SpeciesForCreature(d?.Id, d?.Name) ?? BattlePets.SpeciesForCreature(null, PetModels.KindName(e.name));
        return species == Taming;
    }

    /// <summary>Sends the owner to the fighting area of their island that has the creature being tamed.</summary>
    private void Steer()
    {
        if (Taming == null || owner.IsAway || owner.Island == null || owner.GameManager == null) return;
        if (owner.Chunk != null && owner.Chunk.Enemies != null && owner.Chunk.Enemies.Any(IsTamingTarget)) return;
        var chunks = owner.GameManager.Chunks.GetChunksOfType(owner.Island, TaskType.Fighting);
        var chunk = chunks?.FirstOrDefault(c => c.Enemies != null && c.Enemies.Any(IsTamingTarget));
        if (chunk == null) return;
        if (!owner.ActiveSkill.IsCombatSkill()) return; // only while fighting: !tame does not change what is trained
        owner.Chunk = chunk;
        owner.GotoActiveChunk();
    }

    /// <summary>A defeat the owner helped with: a try when it is the kind being tamed.</summary>
    public async void Defeated(EnemyController e)
    {
        if (Taming == null || !IsTamingTarget(e)) return;
        var species = Taming;
        var stat = owner.Stats?.Taming;
        if (stat == null) return;

        // every try trains Taming: the next level's experience over the tries a level takes
        var level = stat.Level;
        var exp = GameMath.ExperienceForLevel(level + 1) / RavenNest.Combat.Taming.TriesPerLevel(level) * RavenNest.Combat.Taming.TryShare(level, species.TameLevel);
        if (stat.AddExp(exp, out var levels)) owner.CelebrateSkillLevelUp(RavenNest.Models.Skill.Taming, levels);

        // a treat it loves makes it likelier, and is eaten when it works
        var treat = Treat(species);
        if (UnityEngine.Random.value >= RavenNest.Combat.Taming.Chance(stat.Level, species, treat != null)) return;
        if (owner.IsBot)
        {
            AddLocal(species.Id);
            Tamed(e.Position, species);
            return;
        }

        try
        {
            var result = await owner.GameManager.RavenNest.Players.PetTameAsync(owner.Id, species.Id, treat?.Item?.Id ?? Guid.Empty);
            if (result != null && result.Status == PetResultStatus.TamingTooLow)
            {
                // the server checks the saved Taming level, which can be a level behind just after a level up: once more after the next save
                await System.Threading.Tasks.Task.Delay(15000);
                if (!owner || owner.Removed) return;
                result = await owner.GameManager.RavenNest.Players.PetTameAsync(owner.Id, species.Id, treat?.Item?.Id ?? Guid.Empty);
            }
            if (result == null) return;
            if (result.Status == PetResultStatus.StableFull)
            {
                owner.GameManager.RavenBot?.SendReply(owner, Localization.MSG_PET_STABLE_FULL, species.NameAt(0), result.Missing);
                Taming = null;
                return;
            }
            if (result.Status != PetResultStatus.Ok) return;
            Apply(result);
            Tamed(e ? e.Position : owner.Position, species);
        }
        catch (Exception exc)
        {
            Shinobytes.Debug.LogError("Taming for " + owner.PlayerName + ": " + exc.Message);
        }
    }

    /// <summary>A treat the species loves that the owner carries, or null.</summary>
    public GameInventoryItem Treat(PetSpecies species)
    {
        if (species?.Treats == null || species.Treats.Length == 0 || owner.Inventory == null) return null;
        foreach (var item in owner.Inventory.GetAllItems())
        {
            if (item?.Item == null || item.Amount <= 0 || (item.InventoryItem != null && item.InventoryItem.Equipped)) continue;
            foreach (var t in species.Treats) if (string.Equals(t, item.Item.Name, StringComparison.OrdinalIgnoreCase)) return item;
        }
        return null;
    }

    private void Tamed(Vector3 where, PetSpecies species)
    {
        PetFx.Burst(where, 1.2f, PetFx.ElementColor(species.Element));
        if (Time.time < nextAnnounce || owner.GameManager?.RavenBot == null) return;
        nextAnnounce = Time.time + AnnounceEvery;
        owner.GameManager.RavenBot.Announce(Localization.MSG_PET_TAMED, owner.PlayerName, species.NameAt(0));
    }

    // ---------------------------------------------------------------- blows on the owner

    /// <summary>A blow on the owner: a tank pet near takes its share; returns what is left for the owner.</summary>
    public int Intercept(int damage)
    {
        if (damage <= 0 || !Controller) return damage;
        var share = Controller.GuardShare();
        if (share <= 0f) return damage;
        var taken = Mathf.RoundToInt(damage * share);
        Controller.TakeHit(taken);
        return damage - taken;
    }
}
