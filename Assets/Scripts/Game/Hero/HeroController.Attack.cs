using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The hero's click attack (after the user's Gatebound): a swing starts the moment the button goes down. On an enemy
/// in reach it swings at once (an enemy further off is run to first, then swung at); with Shift, while WASD moves the
/// hero, or over the shoulder, it swings toward the cursor even into empty air, turning to the enemy nearest the aim.
/// Holding the button keeps swinging. Three swings make a combo (the third hits harder) that resets after a pause.
/// From above it plays like RuneScape: a clicked enemy is fought until it falls or the hero is sent elsewhere; over the
/// shoulder every swing is the player's. A click on a tree, rock, fish, farm or NPC does that (ManualPlayerController).
///
/// A swing roots the hero; its hit lands halfway through, on everything in a 120 degree arc in front within the
/// weapon's reach (five at most, each once). A roll may cancel it after a third. A hit stops the hero's and the
/// enemy's animation for a moment (not the game: viewers share the world), shakes the camera a little, and the blade
/// leaves a trail. Bow, magic and healing shoot on the click instead: an arrow, a bolt or holy light, at the enemy or
/// along the aim.
/// </summary>
public partial class HeroController
{
    private const float ComboReset = 1.2f;
    private const float CancelShare = 0.35f;  // a roll may cut a swing short after this much of it
    private const float ChainShare = 0.7f;    // a held or buffered click starts the next swing from here
    private const float HitShare = 0.5f;      // a swing's hit lands here
    private const float ShotShare = 0.3f;     // a shot leaves here
    private const float ShotRange = 16f;
    private const float PickPixels = 60f;     // how near the cursor (at 1080 lines) an enemy counts as clicked

    private float attackStarted;
    private float attackEnds;
    private float attackHitAt;
    private bool attackLanded;
    private int combo = -1;
    private float lastAttackEnd;
    private EnemyController attackTarget;
    private EnemyController pursuing;
    private EnemyController autoTarget;       // from above: fought until it falls
    private bool gathering;                   // a tree, rock, fish or farm clicked: the character's own task runs
    private Vector3 attackDirection;
    private bool attackQueued;
    private bool holdingAttack;
    private bool attackInPlace;
    private float animatorSpeedBefore = 1f;
    private TrailRenderer trail;
    private Dictionary<string, float> clipLengths;

    /// <summary>A swing (or a shot) is under way: the hero stands still for it.</summary>
    public bool Attacking => Time.time < attackEnds;

    private bool CanCancelAttack => !Attacking || Time.time >= attackStarted + (attackEnds - attackStarted) * CancelShare;

    private bool Ranged => Style != HeroStyle.Melee;

    /// <summary>How far a swing reaches: the weapon's kind; a spear further, a one-handed weapon less.</summary>
    private float Reach
    {
        get
        {
            var weapon = Player.Inventory.GetEquipmentOfCategory(RavenNest.Models.ItemCategory.Weapon);
            switch (weapon?.Type)
            {
                case RavenNest.Models.ItemType.TwoHandedSpear: return 3.4f;
                case RavenNest.Models.ItemType.TwoHandedSword:
                case RavenNest.Models.ItemType.TwoHandedAxe: return 2.9f;
                default: return 2.4f;
            }
        }
    }

    /// <summary>The swing animations for the melee weapon (tools/hero/hero-animations.cs), and how fast they play.</summary>
    private (string Set, float Speed) MeleeSet
    {
        get
        {
            var weapon = Player.Inventory.GetEquipmentOfCategory(RavenNest.Models.ItemCategory.Weapon);
            switch (weapon?.Type)
            {
                case RavenNest.Models.ItemType.TwoHandedSword: return ("Hero-2HSword", 1.6f);
                case RavenNest.Models.ItemType.TwoHandedAxe: return ("Hero-2HAxe", 1.8f);
                case RavenNest.Models.ItemType.TwoHandedSpear: return ("Hero-Spear", 1.6f);
                case RavenNest.Models.ItemType.OneHandedAxe: return ("Hero-Axe", 1.3f);
                default: return ("Hero-Sword", 1.3f);
            }
        }
    }

    // ---- Input -----------------------------------------------------------------------------------------------

    /// <summary>The left mouse went down: attack the enemy at the cursor, swing toward the cursor, or walk there.</summary>
    private void Click(bool inPlace)
    {
        var shoulder = heroCamera && heroCamera.Mode == HeroCamera.View.Shoulder;
        var enemy = EnemyUnderCursor();
        if (!enemy && shoulder) enemy = Closest(Player.Position, Ranged ? ShotRange : Reach + 4f, heroCamera.Forward, 35f);
        holdingToMove = false;

        if (enemy)
        {
            HeroMarker.Show(enemy.Position, HeroMarker.Attack, 1.8f);
            holdingAttack = true;
            attackInPlace = false;
            autoTarget = shoulder ? null : enemy;
            Attack(enemy);
            return;
        }
        autoTarget = null;
        if (inPlace)
        {
            holdingAttack = true;
            attackInPlace = true;
            Attack(null);
            return;
        }

        // loot on the ground: walk to it (walking over it picks it up)
        var cam = Camera.main;
        var drop = cam ? LootDrop.UnderCursor(cam, PickPixels * Screen.height / 1080f) : null;
        if (drop && drop.Owner == Player)
        {
            holdingAttack = false;
            pursuing = null;
            HeroMarker.Show(drop.Rest, HeroMarker.Walk);
            WalkTo(drop.Rest);
            return;
        }

        // a tree, rock, fish, farm patch or NPC: the character's own way of doing it
        var manual = Player.GetComponent<ManualPlayerController>();
        if (manual && cam && !Attacking && manual.Interact(cam, false, out _))
        {
            holdingAttack = false;
            pursuing = null;
            if (steering) StopSteering();
            gathering = Player.taskTarget != null;
            return;
        }
        if (shoulder)
        {
            holdingAttack = true;
            attackInPlace = true;
            Attack(null);
            return;
        }
        holdingAttack = false;
        pursuing = null;
        holdingToMove = true;
        nextHoldMove = Time.time + 0.12f;
        if (TryGround(aim, out var spot)) HeroMarker.Show(spot, HeroMarker.Walk);
        WalkTo(aim);
    }

    /// <summary>Attack an enemy (run to it first when it is out of reach) or, with none, toward the aim.</summary>
    private void Attack(EnemyController enemy)
    {
        if (enemy) enemy.HoldStill(6f);
        if (enemy && OutOfReach(enemy))
        {
            pursuing = enemy;
            Pursue(enemy);
            return;
        }
        pursuing = null;
        if (Attacking)
        {
            // mid swing: the next one is buffered and starts as soon as this one allows
            attackQueued = true;
            attackTarget = enemy;
            return;
        }
        StartAttack(enemy);
    }

    /// <summary>Too far to swing at (a little inside the reach, so a swing begun at its edge still lands) or to shoot.</summary>
    private bool OutOfReach(EnemyController enemy)
    {
        var distance = Flat(enemy.Position - Player.Position).magnitude;
        return distance > (Ranged ? ShotRange : Reach - 0.2f);
    }

    /// <summary>Run to an enemy: to a spot well inside the reach on this side of it, not onto it.</summary>
    private void Pursue(EnemyController enemy)
    {
        StopGathering();
        var away = Flat(Player.Position - enemy.Position);
        var stand = Ranged ? ShotRange * 0.8f : Reach * 0.55f;
        var spot = away.magnitude > stand ? enemy.Position + away.normalized * stand : Player.Position;
        Player.SetDestination(spot);
    }

    /// <summary>Back to the hero's fighting after a tree, rock or fish: the style's task and stance.</summary>
    private void StopGathering()
    {
        if (!gathering) return;
        gathering = false;
        EnterStyleTask();
    }

    /// <summary>From above, a clicked enemy is fought on: the next swing once the last is done, until it falls.</summary>
    private bool AutoAttacking => autoTarget && !autoTarget.Stats.IsDead && autoTarget.gameObject.activeInHierarchy
        && heroCamera && heroCamera.Mode == HeroCamera.View.TopDown;

    /// <summary>Every frame: the swing's hit, its end, the next swing (held or buffered), and running to a target.</summary>
    private void UpdateAttack(bool buttonHeld)
    {
        if (!buttonHeld) holdingAttack = false;
        if (autoTarget && !AutoAttacking) autoTarget = null;

        if (attackEnds > 0f)
        {
            // a swing or a shot keeps turning with a target on the move, until it lands
            if (!attackLanded && attackTarget && !attackTarget.Stats.IsDead) FaceAttack(attackTarget.Position);
            if (!attackLanded && Time.time >= attackHitAt) LandAttack();
            var chainAt = attackStarted + (attackEnds - attackStarted) * ChainShare;
            var auto = AutoAttacking;
            if (Time.time >= chainAt && (attackQueued || holdingAttack || auto))
            {
                var next = attackQueued ? attackTarget : auto ? autoTarget : attackInPlace ? null : attackTarget;
                if (next && (next.Stats.IsDead || !next.gameObject.activeInHierarchy)) next = attackInPlace ? null : NearestAhead();
                attackQueued = false;
                if (next && OutOfReach(next)) { EndAttack(); pursuing = next; }
                else if (next || attackInPlace) StartAttack(next);
            }
            else if (Time.time >= attackEnds) EndAttack();
        }

        // from above: on to the next swing (or after the enemy) once the last one is over
        if (!Attacking && !pursuing && AutoAttacking && !steering && !busy && !gathering) Attack(autoTarget);

        if (pursuing)
        {
            if (pursuing.Stats.IsDead || !pursuing.gameObject.activeInHierarchy) { pursuing = null; return; }
            if (!OutOfReach(pursuing))
            {
                var target = pursuing;
                pursuing = null;
                StartAttack(target);
            }
            else if (Time.time >= nextHoldMove)
            {
                nextHoldMove = Time.time + 0.2f;
                Pursue(pursuing);
            }
        }
    }

    /// <summary>A living enemy close in front, for a held swing whose target fell.</summary>
    private EnemyController NearestAhead() => Closest(Player.Position, Ranged ? ShotRange : Reach + 1.5f, Forward, 70f);

    // ---- The swing ---------------------------------------------------------------------------------------------

    private void StartAttack(EnemyController enemy)
    {
        if (busy) return;
        StopGathering();
        // aim assist: the clicked enemy, else the one nearest the aim close by, else straight at the cursor
        var toward = enemy ? enemy.Position : aim;
        if (!enemy)
        {
            var near = Closest(Player.Position, Ranged ? ShotRange : Reach + 1f, Flat(aim - Player.Position), 40f);
            if (near) { enemy = near; toward = near.Position; }
        }
        attackTarget = enemy;
        attackDirection = Flat(toward - Player.Position).sqrMagnitude > 0.01f ? Flat(toward - Player.Position).normalized : Forward;

        // rooted: no walking while the swing plays
        if (steering) StopSteering();
        holdingToMove = false;
        if (!agent) agent = Player.GetComponent<NavMeshAgent>();
        if (agent && agent.isOnNavMesh) { agent.ResetPath(); agent.velocity = Vector3.zero; }
        Player.taskTarget = null;
        FaceAttack(toward);
        Player.Equipment.ShowWeapon(AttackTypeOf(Style));

        var speedUp = Mathf.Max(0.3f, Style == HeroStyle.Melee ? Player.GetModifiers().AttackSpeedMultiplier : Player.GetModifiers().CastSpeedMultiplier);
        combo = Time.time - lastAttackEnd < ComboReset && combo < 2 ? combo + 1 : 0;
        string state;
        float speed;
        switch (Style)
        {
            // the bow clip is 0.8 s; a shot takes 0.55
            case HeroStyle.Ranged: state = "Hero-Shoot"; speed = 1.45f; break;
            case HeroStyle.Magic:
            case HeroStyle.Healing: state = "Hero-Cast"; speed = 1.2f; break;
            default:
                var set = MeleeSet;
                state = set.Set + "-" + (combo + 1);
                speed = set.Speed;
                break;
        }
        speed *= speedUp;
        float length;
        if (Style == HeroStyle.Melee) length = ClipLength(state) / speed;
        else length = (Style == HeroStyle.Ranged ? 0.55f : 0.7f) / speedUp;

        attackStarted = Time.time;
        attackEnds = Time.time + length;
        attackHitAt = Time.time + length * (Style == HeroStyle.Melee ? HitShare : ShotShare);
        attackLanded = false;
        if (animator)
        {
            if (Style == HeroStyle.Melee || Style == HeroStyle.Ranged) { animatorSpeedBefore = 1f; animator.speed = speed; }
            animator.CrossFadeInFixedTime(state, 0.04f, 0);
        }
        if (Style == HeroStyle.Melee)
        {
            Trail(true);
            // the whoosh just before the blade comes down
            GameSounds.Play(SoundKind.Swing, Player.Position, combo == 2 ? 0.9f : 0.7f, Mathf.Max(0f, length * HitShare - 0.12f), Player);
        }
    }

    private void EndAttack()
    {
        if (attackEnds <= 0f) return;
        attackEnds = 0f;
        lastAttackEnd = Time.time;
        if (animator) animator.speed = animatorSpeedBefore;
        Trail(false);
    }

    /// <summary>Square to a point: the swing's or the shot's way.</summary>
    private void FaceAttack(Vector3 point)
    {
        var to = Flat(point - Player.Position);
        if (to.sqrMagnitude < 0.01f) return;
        attackDirection = to.normalized;
        transform.rotation = Quaternion.LookRotation(attackDirection);
    }

    /// <summary>Cut a swing short (a roll, a skill).</summary>
    private void CancelAttack()
    {
        attackQueued = false;
        pursuing = null;
        EndAttack();
    }

    /// <summary>The hit: a melee arc in front, or the shot leaving.</summary>
    private void LandAttack()
    {
        attackLanded = true;
        var finisher = combo == 2;
        if (Style == HeroStyle.Melee)
        {
            var center = Player.Position + attackDirection * 0.5f;
            var struck = new List<EnemyController>();
            foreach (var e in EnemiesNear(center, Reach + 0.3f))
            {
                var to = Flat(e.Position - Player.Position);
                // the one swung at is hit wherever it stepped, if still in reach; others in the arc in front
                if (e != attackTarget && to.magnitude > 1f && Vector3.Angle(attackDirection, to) > 60f) continue;
                struck.Add(e);
                if (struck.Count >= 5) break;
            }
            foreach (var e in struck) Strike(e, finisher ? 1.35f : 1f);
            if (struck.Count > 0) Impact(struck, finisher);
            return;
        }

        // a shot: at the target, or along the aim to the first enemy on the way (or as far as it goes)
        var target = attackTarget && !attackTarget.Stats.IsDead ? attackTarget : FirstAlong(attackDirection, ShotRange);
        var end = Player.Position + attackDirection * ShotRange + Vector3.up;
        var fx = HeroEffects.Get;
        GameSounds.Play(Style == HeroStyle.Ranged ? SoundKind.BowShot : Style == HeroStyle.Magic ? SoundKind.Zap : SoundKind.HolyCast, Player.Position, 0.8f, 0f, Player);
        switch (Style)
        {
            case HeroStyle.Ranged: Shoot(fx?.Arrow, target, end, 34f, 0.5f, e => { Strike(e, 1f); Impact(new List<EnemyController> { e }, false); }); break;
            case HeroStyle.Magic: Shoot(fx?.Lightning, target, end, 26f, 0f, e => { Strike(e, 1f); Impact(new List<EnemyController> { e }, false); }); break;
            default: Shoot(fx?.HolyBolt, target, end, 22f, 0f, e => { Strike(e, 1f); Impact(new List<EnemyController> { e }, false); }); break;
        }
    }

    /// <summary>The first living enemy along a direction, within a metre of the line.</summary>
    private EnemyController FirstAlong(Vector3 direction, float range)
    {
        EnemyController best = null;
        var bestAlong = range;
        foreach (var e in EnemiesNear(Player.Position, range))
        {
            var to = Flat(e.Position - Player.Position);
            var along = Vector3.Dot(to, direction);
            if (along < 0f || along > bestAlong) continue;
            var off = (to - direction * along).magnitude;
            if (off > 1.1f) continue;
            bestAlong = along;
            best = e;
        }
        return best;
    }

    // ---- How a hit feels ---------------------------------------------------------------------------------------

    /// <summary>A short stop of the hero's and the enemies' animation, a little shake: stronger for a finisher or a kill.</summary>
    private void Impact(List<EnemyController> struck, bool finisher)
    {
        var kill = struck.Exists(e => e && e.Stats.IsDead);
        var stop = Mathf.Min(0.12f, 0.05f * (finisher ? 1.5f : 1f) + (kill ? 0.04f : 0f));
        StartCoroutine(HitStop(stop, struck));
        if (heroCamera) heroCamera.Shake(Mathf.Min(0.35f, 0.08f * (finisher ? 1.6f : 1f) * (kill ? 1.3f : 1f)), 0.12f);
        foreach (var e in struck) if (e) HeroEffects.Burst(HeroEffects.Get?.Arrival, e.Position + Vector3.up * 1.1f, 0.6f, 1f);

        // the blow's sound: steel in flesh (heavier for a finisher or a kill), an arrow going in; the spells have theirs
        var at = struck.Count > 0 && struck[0] ? struck[0].Position + Vector3.up : Player.Position;
        if (Style == HeroStyle.Melee) GameSounds.Play(finisher || kill ? SoundKind.HeavySlash : SoundKind.Slash, at, 0.85f, 0f, Player);
        else if (Style == HeroStyle.Ranged) GameSounds.Play(SoundKind.ArrowHit, at, 0.8f, 0f, Player);
        // a yelp now and then from one still standing (the fallen cry out in WaveRun)
        if (!kill && Random.value < 0.35f) GameSounds.Play(SoundKind.GoblinHurt, at, 0.6f, 0.05f, Player);
    }

    private IEnumerator HitStop(float seconds, List<EnemyController> struck)
    {
        var frozen = new List<Animator>();
        if (animator) frozen.Add(animator);
        foreach (var e in struck)
        {
            var a = e ? e.GetComponentInChildren<Animator>() : null;
            if (a && !e.Stats.IsDead) frozen.Add(a);
        }
        var speeds = frozen.ConvertAll(a => a.speed);
        foreach (var a in frozen) a.speed = 0f;
        yield return new WaitForSecondsRealtime(seconds);
        for (var i = 0; i < frozen.Count; i++)
        {
            if (!frozen[i]) continue;
            // the hero's swing speed may have ended meanwhile
            frozen[i].speed = frozen[i] == animator ? (Attacking && (Style == HeroStyle.Melee || Style == HeroStyle.Ranged) ? speeds[i] : animatorSpeedBefore) : speeds[i];
        }
    }

    /// <summary>The blade's trail while it swings: a short white streak from the weapon's tip.</summary>
    private void Trail(bool on)
    {
        if (on && !trail)
        {
            var weapon = Player.Equipment.MeleeWeapon;
            var material = Resources.Load<Material>("HeroTrail");
            if (!weapon || !material) return;
            // the tip: the far end of the weapon mesh's longest side
            var mesh = weapon.GetComponentInChildren<MeshFilter>();
            var tip = new GameObject("Hero trail").transform;
            tip.SetParent(mesh ? mesh.transform : weapon, false);
            if (mesh && mesh.sharedMesh)
            {
                var b = mesh.sharedMesh.bounds;
                var axis = b.size.x >= b.size.y && b.size.x >= b.size.z ? Vector3.right : b.size.y >= b.size.z ? Vector3.up : Vector3.forward;
                var a = b.center + Vector3.Scale(b.extents, axis);
                var c = b.center - Vector3.Scale(b.extents, axis);
                tip.localPosition = (a.sqrMagnitude >= c.sqrMagnitude ? a : c) * 0.92f;
            }
            trail = tip.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.time = 0.14f;
            trail.minVertexDistance = 0.03f;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.32f), new Keyframe(1f, 0f));
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.9f, 0.7f), 1f) },
                new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.emitting = false;
        }
        if (!trail) return;
        if (on) trail.Clear();
        trail.emitting = on;
    }

    private float ClipLength(string state)
    {
        if (clipLengths == null)
        {
            clipLengths = new Dictionary<string, float>();
            var controller = animator ? animator.runtimeAnimatorController : null;
            if (controller) foreach (var clip in controller.animationClips) clipLengths[clip.name] = clip.length;
        }
        // the state's clip (tools/hero/hero-animations.cs names the states after their sets)
        var clipName = ClipOf(state);
        return clipName != null && clipLengths.TryGetValue(clipName, out var length) ? length : 1.2f;
    }

    private static string ClipOf(string state)
    {
        var i = state.LastIndexOf('-');
        if (i < 0 || !int.TryParse(state.Substring(i + 1), out var n)) return null;
        switch (state.Substring(0, i))
        {
            case "Hero-2HSword": return "2Hand-Sword-Attack" + n;
            case "Hero-2HAxe": return "2Hand-Axe-Attack" + n;
            case "Hero-Spear": return "2Hand-Spear-Attack" + n;
            case "Hero-Sword": return n == 1 ? "Sword-Attack-R1" : n == 2 ? "Sword-Attack-L1" : "Sword-Attack-R2";
            case "Hero-Axe": return n == 1 ? "Mace-Attack-R1" : n == 2 ? "Mace-Attack-L1" : "Mace-Attack-R2";
            default: return null;
        }
    }
}
