using System.Collections.Generic;
using RavenNest.Combat;
using RavenNest.Models;
using UnityEngine;

/// <summary>
///     A battle pet in the world (docs/battle-pets-design.md): follows its owner, joins the owner's fights on the
///     owner's target, and uses its skills by themselves. How its attacks look comes from its species' strike style,
///     so every model can fight: Bite plays the model's attack clip, Pounce is a leap done in code (crouch, jump in,
///     squash on the hit, hop back), Bolt stays back and fires a bolt of its element. Its blows are worked out with
///     the shared combat rules from the species' sheet and land as its owner's (threat, kills and experience go to the
///     owner; status effects too). A tank pet takes a share of the blows on its owner (all of them while it guards); at
///     0 health a pet is knocked out for 30 s, with no penalty.
/// </summary>
public sealed class BattlePetController : MonoBehaviour
{
    private const int GroundLayers = 25556799;   // what the navmesh is baked from (as PetController)
    private const float SnapDistance = 14f;
    private const float FollowSlack = 0.6f;
    private const float RunSpeed = 4.5f;
    private const float LeaveFightBeyond = 22f;  // a target this far from the owner is not the pet's business

    public PlayerController Owner { get; private set; }
    public BattlePet Data { get; private set; }
    public PetSpecies Species { get; private set; }
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public bool KnockedOut => Time.time < knockedUntil;
    public float Height => model != null ? model.Height : 0.6f;

    private PetModels.Model model;
    private PetClipPlayer clips;
    private CombatSheet sheet;
    private readonly Dictionary<string, float> readyAt = new Dictionary<string, float>();
    private readonly List<IAttackable> targets = new List<IAttackable>();
    private static readonly PetRandom random = new PetRandom();
    private static Shinobytes.UI.FloatingTextView floating;
    private static DamageCounterManager counters;

    private float knockedUntil, nextAttack, guardUntil, lastFight, nextRegen, nextGround, groundY, skillPauseUntil;
    private float hardenUntil, hardenAmount, frenzyUntil, frenzyAmount;
    private Vector3 position;
    private Quaternion rotation;
    private float speed;
    private bool placed;

    // the strike being played: a pounce's leap, a bite's wait for the clip's hit, a bolt's hop
    private enum Move { None, Windup, Leap, Land, Back, BiteWait, Hop }
    private Move move;
    private float moveStarted, moveLength;
    private Vector3 moveFrom, moveTo;
    private EnemyController moveTarget;
    private System.Action moveHit;
    private Vector3 baseScale = Vector3.one;

    public static BattlePetController Spawn(PlayerController owner, BattlePet data)
    {
        var species = BattlePets.GetSpecies(data?.Species);
        if (!owner || species == null) return null;
        var go = new GameObject("Battle pet: " + owner.PlayerName + "'s " + data.Name);
        var pet = go.AddComponent<BattlePetController>();
        pet.Setup(owner, data, species);
        return pet;
    }

    private void Setup(PlayerController owner, BattlePet data, PetSpecies species)
    {
        Owner = owner;
        Data = data;
        Species = species;
        model = PetModels.Create(species, data.Stage, data.Prefab);
        if (model != null)
        {
            model.Root.transform.SetParent(transform, false);
            model.Root.transform.localPosition = Vector3.zero;
            model.Root.transform.localRotation = Quaternion.identity;
            baseScale = model.Root.transform.localScale;
            clips = new PetClipPlayer(model.Animator, model.Clips);
        }
        Refresh(data);
        Health = MaxHealth;
    }

    /// <summary>New numbers from the server (a level, an evolution, other slots).</summary>
    public void Refresh(BattlePet data)
    {
        var stageChanged = Data != null && data != null && Data.Stage != data.Stage;
        Data = data ?? Data;
        sheet = Species.Sheet(Mathf.Max(1, Data.Level), Data.Stage);
        var share = MaxHealth > 0 ? (float)Health / MaxHealth : 1f;
        MaxHealth = Mathf.Max(1, sheet.MaxHealth);
        Health = Mathf.Clamp(Mathf.RoundToInt(MaxHealth * share), 1, MaxHealth);
        if (stageChanged)
        {
            // the next model (or the same one bigger)
            if (model != null && model.Root) Destroy(model.Root);
            clips?.Dispose();
            model = PetModels.Create(Species, Data.Stage, Data.Prefab);
            if (model != null)
            {
                model.Root.transform.SetParent(transform, false);
                baseScale = model.Root.transform.localScale;
                clips = new PetClipPlayer(model.Animator, model.Clips);
            }
            PetFx.Burst(transform.position, Mathf.Max(0.6f, Height), PetFx.ElementColor(Species.Element));
        }
    }

    private void OnDestroy() => clips?.Dispose();

    // ---------------------------------------------------------------- every frame

    private void Update()
    {
        if (!Owner || Owner.isDestroyed) { Destroy(gameObject); return; }
        clips?.Tick();

        var hidden = Owner.ferryHandler.OnFerry || Owner.ferryHandler.Active || Owner.onsenHandler.InOnsen || Owner.Stats == null || Owner.Stats.IsDead;
        if (model != null && model.Root && model.Root.activeSelf == hidden) model.Root.SetActive(!hidden);
        if (hidden) { placed = false; move = Move.None; return; }

        if (KnockedOut) { Lie(); return; }
        if (knockedUntil > 0f) GetUp();

        var target = FightTarget();
        if (target) lastFight = Time.time;
        Regenerate();

        if (move != Move.None) { Strike(); return; }

        Vector3 goal;
        float slack;
        if (target)
        {
            var standoff = Standoff(target);
            var from = target.Position;
            var away = position - from;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Owner.Position - from;
            away.y = 0f;
            goal = from + (away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.forward) * standoff;
            slack = 0.4f;
        }
        else
        {
            goal = Owner.transform.TransformPoint(new Vector3(-0.9f, 0f, -0.8f));
            slack = FollowSlack;
        }

        Walk(goal, slack, target);

        if (target && Time.time >= nextAttack)
        {
            var reach = Standoff(target) + 0.8f;
            var d = position - target.Position;
            d.y = 0f;
            if (d.magnitude <= reach) Attack(target);
        }
    }

    private EnemyController FightTarget()
    {
        if (Owner.Stats.IsDead) return null;
        var e = Owner.CombatTarget as EnemyController;
        if (!e || e.Stats == null || e.Stats.IsDead || e.Removed) return null;
        if ((e.Position - Owner.Position).sqrMagnitude > LeaveFightBeyond * LeaveFightBeyond) return null;
        return e;
    }

    private float Standoff(EnemyController target)
    {
        if (Species.Strike == PetStrike.Bolt) return 5f;
        return Mathf.Max(0.8f, target.GetHitRange() * 0.8f + Height * 0.4f);
    }

    private void Walk(Vector3 goal, float slack, EnemyController facing)
    {
        var dt = Mathf.Max(Time.deltaTime, 0.0001f);
        if (!placed || (goal - position).sqrMagnitude > SnapDistance * SnapDistance)
        {
            position = Owner.transform.TransformPoint(new Vector3(-0.9f, 0f, -0.8f));
            rotation = Owner.transform.rotation;
            groundY = Owner.Position.y;
            placed = true;
            speed = 0f;
        }

        var to = goal - position;
        to.y = 0f;
        var distance = to.magnitude;
        var wanted = distance <= slack ? 0f : RunSpeed * (distance > 3f ? 1.6f : 1f);
        speed = Mathf.MoveTowards(speed, wanted, 14f * dt);
        if (distance > 0.001f) position += to / distance * Mathf.Min(distance, speed * dt);

        if (Time.time >= nextGround)
        {
            nextGround = Time.time + 0.25f;
            groundY = Physics.Raycast(position + Vector3.up * 3f, Vector3.down, out var hit, 8f, GroundLayers, QueryTriggerInteraction.Ignore) ? hit.point.y : Owner.Position.y;
        }
        position.y = Mathf.Lerp(position.y, groundY, 10f * dt);

        var look = facing ? facing.Position - position : speed > 0.3f ? to : Owner.Position - position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.0001f) rotation = Quaternion.RotateTowards(rotation, Quaternion.LookRotation(look), 480f * dt);
        transform.SetPositionAndRotation(position, rotation);

        clips?.Play(speed > 0.3f ? PetClipPlayer.Kind.Move : PetClipPlayer.Kind.Idle);
    }

    private void Regenerate()
    {
        if (Time.time < nextRegen || Health >= MaxHealth) return;
        nextRegen = Time.time + 1f;
        if (Time.time - lastFight < 5f) return;
        Health = Mathf.Min(MaxHealth, Health + Mathf.Max(1, MaxHealth / 20));
    }

    // ---------------------------------------------------------------- attacking

    private float AttackInterval()
    {
        var faster = Time.time < frenzyUntil ? frenzyAmount : 0f;
        return (float)Species.Stats.AttackInterval / (1f + faster);
    }

    private void Attack(EnemyController target)
    {
        nextAttack = Time.time + AttackInterval();
        if (Time.time >= skillPauseUntil && UseSkill(target)) return;
        Play(target, () => Hit(target, 1, null));
    }

    /// <summary>Plays the species' strike at the target; <paramref name="hit"/> runs at the moment it lands.</summary>
    private void Play(EnemyController target, System.Action hit)
    {
        moveTarget = target;
        moveHit = hit;
        moveStarted = Time.time;
        switch (Species.Strike)
        {
            case PetStrike.Bite:
                // the model's own attack clip, the hit a little before its middle
                if (clips != null && clips.PlayOnce(PetClipPlayer.Kind.Attack, PetClipPlayer.Kind.Idle))
                {
                    move = Move.BiteWait;
                    moveLength = Mathf.Min(0.5f, clips.Length(PetClipPlayer.Kind.Attack) * 0.4f);
                }
                else
                {
                    // no clip after all: pounce
                    move = Move.Windup;
                    moveLength = 0.12f;
                }
                break;
            case PetStrike.Pounce:
                move = Move.Windup;
                moveLength = 0.12f;
                break;
            default:
                move = Move.Hop;
                moveLength = 0.28f;
                clips?.PlayOnce(PetClipPlayer.Kind.Attack, PetClipPlayer.Kind.Idle);
                var from = position + Vector3.up * Height * 0.7f + rotation * Vector3.forward * 0.2f;
                var h = moveHit;
                moveHit = null;
                PetFx.Bolt(from, target.transform, Vector3.up * Mathf.Max(0.5f, target.HealthBarOffset * 0.5f), PetFx.ElementColor(Species.Element), Mathf.Clamp(Height * 0.25f, 0.12f, 0.35f), h);
                break;
        }
    }

    private void Strike()
    {
        var dt = Time.deltaTime;
        var t = moveLength > 0 ? Mathf.Clamp01((Time.time - moveStarted) / moveLength) : 1f;
        var root = model != null && model.Root ? model.Root.transform : null;
        switch (move)
        {
            case Move.Windup:
                if (root) root.localScale = new Vector3(baseScale.x * 1.08f, baseScale.y * 0.82f, baseScale.z * 1.08f);
                if (t >= 1f)
                {
                    move = Move.Leap;
                    moveStarted = Time.time;
                    moveLength = 0.22f;
                    moveFrom = position;
                    var tp = moveTarget ? moveTarget.Position : position + rotation * Vector3.forward;
                    var dir = tp - position;
                    dir.y = 0f;
                    var reach = moveTarget ? Mathf.Max(0.5f, moveTarget.GetHitRange() * 0.6f + Height * 0.2f) : 0.5f;
                    moveTo = tp - (dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward) * reach;
                    moveTo.y = position.y;
                    clips?.Play(PetClipPlayer.Kind.Move, 0.05f, true);
                    if (root) root.localScale = new Vector3(baseScale.x * 0.92f, baseScale.y * 1.12f, baseScale.z * 0.92f);
                }
                break;
            case Move.Leap:
                {
                    var p = Vector3.Lerp(moveFrom, moveTo, t);
                    p.y += Mathf.Sin(t * Mathf.PI) * Mathf.Max(0.3f, Height * 0.6f);
                    transform.position = p;
                    if (t >= 1f)
                    {
                        position = moveTo;
                        RunHit();
                        move = Move.Land;
                        moveStarted = Time.time;
                        moveLength = 0.12f;
                        if (root) root.localScale = new Vector3(baseScale.x * 1.2f, baseScale.y * 0.7f, baseScale.z * 1.2f);
                    }
                    break;
                }
            case Move.Land:
                if (root) root.localScale = Vector3.Lerp(new Vector3(baseScale.x * 1.2f, baseScale.y * 0.7f, baseScale.z * 1.2f), baseScale, t);
                if (t >= 1f)
                {
                    move = Move.Back;
                    moveStarted = Time.time;
                    moveLength = 0.25f;
                    moveFrom = position;
                    var back = moveTarget ? position - moveTarget.Position : -(rotation * Vector3.forward);
                    back.y = 0f;
                    moveTo = position + (back.sqrMagnitude > 0.0001f ? back.normalized : Vector3.back) * 0.6f;
                }
                break;
            case Move.Back:
                {
                    var p = Vector3.Lerp(moveFrom, moveTo, t);
                    p.y += Mathf.Sin(t * Mathf.PI) * 0.15f;
                    transform.position = p;
                    if (t >= 1f) { position = moveTo; End(); }
                    break;
                }
            case Move.BiteWait:
                if (t >= 1f) { RunHit(); End(); }
                break;
            case Move.Hop:
                {
                    var p = position;
                    p.y += Mathf.Sin(t * Mathf.PI) * Mathf.Max(0.12f, Height * 0.25f);
                    transform.position = p;
                    if (t >= 1f) { transform.position = position; End(); }
                    break;
                }
        }
        if (moveTarget)
        {
            var look = moveTarget.Position - transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) { rotation = Quaternion.RotateTowards(rotation, Quaternion.LookRotation(look), 720f * dt); transform.rotation = rotation; }
        }
    }

    private void RunHit()
    {
        var h = moveHit;
        moveHit = null;
        try { h?.Invoke(); } catch (System.Exception exc) { Shinobytes.Debug.LogError("Battle pet hit: " + exc); }
    }

    private void End()
    {
        move = Move.None;
        if (model != null && model.Root) model.Root.transform.localScale = baseScale;
    }

    /// <summary>One blow on an enemy, worked out from the pet's sheet, landing as its owner's.</summary>
    private void Hit(EnemyController e, double multiplier, Element? element, double armorIgnored = 0)
    {
        if (!e || e.Stats == null || e.Stats.IsDead || !Owner) return;
        var defender = CombatRules.Sheet(e);
        if (defender == null) return;
        var result = CombatMath.Attack(sheet, Species.Stats.Style, defender, random, new AttackOptions { Multiplier = multiplier, Element = element, ArmorIgnored = armorIgnored });
        CombatRules.BlowLanded?.Invoke(Owner, e, result, defender);
        Owner.SessionStats.AddDamageDealt(result.Total);
        if (e.TakeDamage(Owner, result.Total)) Owner.Combat.Defeated(e);
    }

    // ---------------------------------------------------------------- skills

    private bool UseSkill(EnemyController target)
    {
        var slots = (Data.Slots ?? "").Split(',');
        foreach (var id in slots)
        {
            var skill = BattlePets.GetSkill(id);
            if (skill == null || !skill.IsActive) continue;
            if (readyAt.TryGetValue(skill.Id, out var ready) && Time.time < ready) continue;
            var active = skill.Active;
            if (!Fits(active, target)) continue;
            Resolve(active, target);
            if (targets.Count == 0) continue;

            readyAt[skill.Id] = Time.time + (float)active.Cooldown;
            skillPauseUntil = Time.time + 1.5f;
            ShowText(skill.Name);
            Cast(skill, active, target);
            return true;
        }
        return false;
    }

    private static float Share(IAttackable f)
    {
        var s = f?.GetStats();
        return s == null || s.Health.MaxValue <= 0 ? 1f : (float)s.Health.CurrentValue / s.Health.MaxValue;
    }

    private bool Fits(ActiveSkill active, EnemyController target)
    {
        var radius = active.Radius > 0 ? (float)active.Radius : 6f;
        switch (active.Use)
        {
            case SkillUse.InFight: return target;
            case SkillUse.OnBoss: return target && (target.IsRaidBoss || target.IsDungeonBoss || (target.CombatDefinition != null && target.CombatDefinition.IsBoss));
            case SkillUse.TargetLow: return target && Share(target) < 0.2f;
            case SkillUse.SelfHurt: return MaxHealth > 0 && Health < MaxHealth / 2;
            case SkillUse.EnemiesNear: return target && CountEnemies(target.Position, Mathf.Max(radius, 4f)) >= 2;
            case SkillUse.AllyHurt:
                foreach (var ally in Allies(radius)) if (Share(ally) < 0.6f) return true;
                return false;
            case SkillUse.AllyHarmed:
                foreach (var ally in Allies(radius)) if (CombatEffects.HasHarmful(ally)) return true;
                return false;
            case SkillUse.EnemyOnAlly:
                foreach (var e in EnemyController.Active)
                    if (Near(e, position, radius) && e.TargetPlayer == Owner) return true;
                return false;
            default: return false;
        }
    }

    private bool Near(EnemyController e, Vector3 center, float radius) =>
        e && e.Stats != null && !e.Stats.IsDead && e.Island == Owner.Island && (e.Position - center).sqrMagnitude <= radius * radius;

    private int CountEnemies(Vector3 center, float radius)
    {
        var n = 0;
        foreach (var e in EnemyController.Active) if (Near(e, center, radius)) n++;
        return n;
    }

    private IEnumerable<PlayerController> Allies(float radius)
    {
        yield return Owner;
        var game = Owner.GameManager;
        if (!game || game.Players == null) yield break;
        foreach (var p in game.Players.GetAllPlayers())
        {
            if (!p || p == Owner || p.Stats == null || p.Stats.IsDead || p.Island != Owner.Island) continue;
            if ((p.Position - position).sqrMagnitude > radius * radius) continue;
            yield return p;
        }
    }

    private void Resolve(ActiveSkill active, EnemyController target)
    {
        targets.Clear();
        var radius = active.Radius > 0 ? (float)active.Radius : 6f;
        var max = Mathf.Max(1, active.MaxTargets);
        switch (active.Targeting)
        {
            case SkillTargeting.Self:
                targets.Add(Owner); // a stand in: Self effects are the pet's own (Cast)
                break;
            case SkillTargeting.Target:
                if (target) targets.Add(target);
                break;
            case SkillTargeting.LowestAlly:
                {
                    PlayerController lowest = null;
                    var least = 2f;
                    foreach (var ally in Allies(radius))
                    {
                        var share = Share(ally);
                        if (active.Use == SkillUse.AllyHarmed ? CombatEffects.HasHarmful(ally) && share < least : share < least) { least = share; lowest = ally; }
                    }
                    if (lowest) targets.Add(lowest);
                    break;
                }
            case SkillTargeting.Allies:
                foreach (var ally in Allies(radius)) { if (targets.Count >= max) break; targets.Add(ally); }
                break;
            case SkillTargeting.Cone:
            case SkillTargeting.Around:
                if (target) targets.Add(target);
                foreach (var e in EnemyController.Active)
                {
                    if (targets.Count >= max) break;
                    if (Near(e, active.Targeting == SkillTargeting.Around ? position : (target ? target.Position : position), radius) && !targets.Contains(e)) targets.Add(e);
                }
                break;
            case SkillTargeting.AroundTarget:
            case SkillTargeting.Chain:
            case SkillTargeting.FarthestEnemy:
                if (!target) break;
                targets.Add(target);
                foreach (var e in EnemyController.Active)
                {
                    if (targets.Count >= max) break;
                    if (Near(e, target.Position, radius) && !targets.Contains(e)) targets.Add(e);
                }
                break;
        }
    }

    private void Cast(PetSkill skill, ActiveSkill active, EnemyController target)
    {
        var self = active.Targeting == SkillTargeting.Self;
        var toAllies = active.Targeting == SkillTargeting.Allies || active.Targeting == SkillTargeting.LowestAlly;
        var color = toAllies ? PetFx.HealColor : PetFx.ElementColor(Species.Element);
        var hasDamage = false;
        foreach (var e in active.Effects) if (e.Kind == SkillEffectKind.Damage) hasDamage = true;

        if (self || toAllies || !hasDamage)
        {
            PetFx.Ring(self || toAllies ? position : (target ? target.Position : position), active.Radius > 0 ? (float)active.Radius * 0.6f : 1.5f, color);
            clips?.PlayOnce(PetClipPlayer.Kind.Attack, PetClipPlayer.Kind.Idle);
            Apply(active, self);
        }
        else
        {
            // damage: the strike carries it, so it lands with the pounce, the bite or the bolt
            var list = new List<IAttackable>(targets);
            Play(target, () => { targets.Clear(); targets.AddRange(list); Apply(active, false); });
            if (active.Targeting == SkillTargeting.Cone || active.Targeting == SkillTargeting.Around) PetFx.Ring(target ? target.Position : position, 2f, color);
        }
    }

    private void Apply(ActiveSkill active, bool self)
    {
        foreach (var effect in active.Effects)
        {
            var seconds = (float)effect.Seconds;
            if (self)
            {
                switch (effect.Kind)
                {
                    case SkillEffectKind.DamageTaken: hardenUntil = Time.time + seconds; hardenAmount = (float)effect.Amount; break;
                    case SkillEffectKind.AttackSpeed: frenzyUntil = Time.time + seconds; frenzyAmount = (float)effect.Amount; break;
                    case SkillEffectKind.HealPercent: Health = Mathf.Min(MaxHealth, Health + Mathf.RoundToInt(MaxHealth * (float)effect.Amount)); break;
                }
                continue;
            }
            foreach (var t in targets)
            {
                if (t == null || (t is Object o && !o)) continue;
                switch (effect.Kind)
                {
                    case SkillEffectKind.Damage:
                        if (t is EnemyController de) for (var i = 0; i < Mathf.Max(1, effect.Hits); ++i) Hit(de, effect.Amount, effect.Element, effect.ArmorIgnored);
                        break;
                    case SkillEffectKind.DamageOverTime:
                        CombatEffects.AddDamageOverTime(Owner, t, effect.Element, TypicalHit() * effect.Amount, seconds);
                        break;
                    case SkillEffectKind.Heal:
                        t.Heal(HealAmount(effect.Amount));
                        break;
                    case SkillEffectKind.HealOverTime:
                        CombatEffects.AddHealOverTime(Owner, t, HealAmount(effect.Amount), seconds);
                        break;
                    case SkillEffectKind.HealPercent:
                        { var s = t.GetStats(); if (s != null) t.Heal(Mathf.RoundToInt((float)(s.Health.MaxValue * effect.Amount))); }
                        break;
                    case SkillEffectKind.Shield:
                        if (t is PlayerController shielded) shielded.AddShield(HealAmount(effect.Amount), seconds);
                        break;
                    case SkillEffectKind.DamageTaken:
                        CombatEffects.AddModifier(t, CombatEffects.ModifierKind.DamageTaken, effect.Amount, seconds);
                        break;
                    case SkillEffectKind.DamageDealt:
                        CombatEffects.AddModifier(t, CombatEffects.ModifierKind.DamageDealt, effect.Amount, seconds);
                        break;
                    case SkillEffectKind.AttackSpeed:
                        CombatEffects.AddModifier(t, CombatEffects.ModifierKind.AttackSpeed, effect.Amount, seconds);
                        break;
                    case SkillEffectKind.Taunt:
                        // a pet's taunt is its guard: it takes every blow aimed at its owner for the time
                        guardUntil = Mathf.Max(guardUntil, Time.time + seconds);
                        break;
                    case SkillEffectKind.Stun:
                        CombatEffects.CrowdControl(Owner, t, StatusEffectKind.Stun, seconds);
                        break;
                    case SkillEffectKind.Freeze:
                        CombatEffects.CrowdControl(Owner, t, StatusEffectKind.Freeze, seconds);
                        break;
                    case SkillEffectKind.Root:
                        CombatEffects.CrowdControl(Owner, t, StatusEffectKind.Root, seconds);
                        break;
                    case SkillEffectKind.Cleanse:
                        CombatEffects.Cleanse(t);
                        break;
                    case SkillEffectKind.CrowdControlImmunity:
                        CombatEffects.ImmuneToCrowdControl(t, seconds);
                        break;
                }
            }
        }
    }

    private double TypicalHit()
    {
        var s = sheet.Style(Species.Stats.Style);
        return CombatMath.MaxHit(s.DamageLevel, s.EffectivePower) * 0.7 * sheet.DamageMultiplier;
    }

    private int HealAmount(double multiplier) => Mathf.Max(1, CombatMath.Heal(sheet, random, multiplier).Amount);

    // ---------------------------------------------------------------- taking blows for its owner

    /// <summary>The share of a blow on its owner this pet takes: a tank takes half, all while it guards; others none.</summary>
    public float GuardShare()
    {
        if (KnockedOut || Species.Role != PetRole.Tank || !placed) return 0f;
        if ((position - Owner.Position).sqrMagnitude > 8f * 8f) return 0f;
        return Time.time < guardUntil ? 1f : 0.5f;
    }

    public void TakeHit(int damage)
    {
        if (KnockedOut || damage <= 0) return;
        if (Time.time < hardenUntil) damage = Mathf.Max(0, Mathf.RoundToInt(damage * (1f + hardenAmount)));
        // its armour takes its share, as a player's would
        damage = Mathf.Max(0, Mathf.RoundToInt(damage * (float)CombatMath.ArmorReduction(sheet.Armor)));
        lastFight = Time.time;
        Health -= damage;
        if (GraphicsToggler.GraphicsEnabled)
        {
            if (!counters) counters = FindAnyObjectByType<DamageCounterManager>(FindObjectsInactive.Include);
            if (counters) counters.Add(transform, damage);
        }
        if (Health <= 0) KnockOut();
        else if (clips != null && move == Move.None) clips.PlayOnce(PetClipPlayer.Kind.Hit, PetClipPlayer.Kind.Idle);
    }

    private void KnockOut()
    {
        Health = 0;
        knockedUntil = Time.time + (float)BattlePets.KnockoutSeconds;
        move = Move.None;
        End();
        if (clips == null || !clips.Hold(PetClipPlayer.Kind.Down))
        {
            // no clip for it: it lies on its side
            if (model != null && model.Root) model.Root.transform.localRotation = Quaternion.Euler(0f, 0f, 80f);
        }
        ShowText(Localization.MSG_PET_KNOCKED_OUT_SHORT);
    }

    private void Lie()
    {
        // stays where it fell; snaps along if its owner goes far away
        if ((Owner.Position - position).sqrMagnitude > SnapDistance * SnapDistance)
        {
            position = Owner.transform.TransformPoint(new Vector3(-0.9f, 0f, -0.8f));
            transform.position = position;
        }
    }

    private void GetUp()
    {
        knockedUntil = 0f;
        Health = MaxHealth;
        if (model != null && model.Root) model.Root.transform.localRotation = Quaternion.identity;
        clips?.Release(PetClipPlayer.Kind.Idle);
        PetFx.Ring(position, Mathf.Max(0.6f, Height), PetFx.HealColor);
    }

    // ---------------------------------------------------------------- words over it

    public void ShowText(string text)
    {
        if (!GraphicsToggler.GraphicsEnabled || string.IsNullOrEmpty(text)) return;
        if (!floating) floating = FindAnyObjectByType<Shinobytes.UI.FloatingTextView>();
        if (floating) floating.AddText(transform, text);
    }

    private sealed class PetRandom : ICombatRandom
    {
        public double Next() => Random.value * 0.999999;
    }
}
