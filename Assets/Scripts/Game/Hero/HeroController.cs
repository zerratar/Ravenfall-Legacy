using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Hero mode (docs/hero-mode-design.md): the streamer's character played as an action RPG hero, on top of playing by
/// hand (6c). WASD walks relative to the camera, the mouse aims, the left mouse attacks the enemy at the cursor (and
/// keeps at it) or walks there, 1 to 4 are the style's skills, Tab switches the style (melee, ranged, magic, healing),
/// Space dodges, Q drinks a potion, V switches the camera (HeroCamera), C and I open the hero's details and gear.
/// Skills cost mana, which refills by itself, and have cooldowns. The hero's aura (how much less damage it takes) is
/// a placeholder until the waves of 7b set it.
/// </summary>
public class HeroController : MonoBehaviour
{
    public const float DodgeCooldown = 3f;
    private const float DodgeDistance = 5f;
    private const float DodgeSeconds = 0.4f;
    private const float PotionShare = 0.35f;
    private const float ManaPerSecond = 4f;
    private const float Aura = 0.5f; // 7a: the hero takes half damage; 7b scales it to the waves
    private const float StyleSwitchCooldown = 1f;
    private const float HolyBoltRange = 10f;
    private const float HolyBoltInterval = 1.1f;

    public static HeroController Active { get; private set; }
    public static event Action Changed;

    public PlayerController Player { get; private set; }
    public float Mana { get; private set; }
    public float MaxMana => 100f;
    public int Potions { get; private set; }
    public int MaxPotions => 3;
    public float DodgeReadyAt { get; private set; }
    public IReadOnlyList<HeroSkill> Kit => kit;
    public float ReadyAt(int slot) => readyAt[(int)Style][slot];
    public HeroStyle Style { get; private set; }
    public float StyleReadyAt { get; private set; }

    /// <summary>A skill that could not be used: its slot, for the HUD to flash, and why.</summary>
    public static event Action<int, string> Refused;

    private readonly List<HeroSkill> kit = new List<HeroSkill>();

    // what hero mode put away (the players list, the details window), to bring back when it ends
    private bool listWasVisible;
    private bool detailsWereVisible;
    private readonly float[][] readyAt = { new float[4], new float[4], new float[4], new float[4] }; // by style
    private Animator animator;
    private HeroCamera heroCamera;
    private Vector3 aim;
    private float potionReadyAt;
    private float invulnerableUntil;
    private float guardUntil;
    private bool busy; // a dodge or a leap moves the hero itself
    private float spinUntil;
    private float nextSpinHit;
    private EnemyController[] enemies = Array.Empty<EnemyController>();
    private float enemiesAt;
    private EnemyController holyTarget; // the healer's basic attack
    private float nextHolyBolt;
    private float rapidFireUntil;
    private float nextRapidArrow;
    private bool angel;

    /// <summary>The character played by hand becomes the hero.</summary>
    public static bool Begin(PlayerController player)
    {
        var game = GameManager.Instance;
        if (!player || !player.Controlled || !game) return false;
        if (Active) End();
        var hero = player.gameObject.AddComponent<HeroController>();
        hero.Player = player;
        hero.animator = player.GetComponentInChildren<Animator>();
        hero.Mana = hero.MaxMana;
        hero.Potions = hero.MaxPotions;
        Active = hero;

        // the style the character trains, if it fights; melee otherwise
        var training = player.GetTask() == TaskType.Fighting;
        hero.meleeArgument = training && (player.TrainingAttack || player.TrainingStrength || player.TrainingDefense) ? player.GetTaskArgument() : "all";
        var style = !training ? HeroStyle.Melee : player.TrainingHealing ? HeroStyle.Healing : player.TrainingMagic ? HeroStyle.Magic : player.TrainingRanged ? HeroStyle.Ranged : HeroStyle.Melee;
        hero.SetStyle(style);
        game.Camera.FollowHero(player);
        hero.heroCamera = game.Camera.HeroView;

        // an action RPG's screen: the players list and the details window step aside (C and I bring the hero's back)
        var list = Shinobytes.UI.PlayerListWindow.Instance;
        hero.listWasVisible = list != null && list.RequestedVisible;
        if (list != null) list.SetVisible(false);
        var details = game.Camera.Observer;
        hero.detailsWereVisible = details && details.IsVisible;
        if (details)
        {
            details.Observe(player, float.MaxValue);
            details.SetVisibility(false);
        }
        Changed?.Invoke();
        return true;
    }

    /// <summary>Back to playing by hand.</summary>
    public static void End()
    {
        var hero = Active;
        Active = null;
        if (hero)
        {
            hero.StopSpin();
            hero.ShowOwnTags(true);
            var list = Shinobytes.UI.PlayerListWindow.Instance;
            if (list != null && hero.listWasVisible) list.SetVisible(true);
            var game0 = GameManager.Instance;
            var details = game0 ? game0.Camera.Observer : null;
            if (details) details.SetVisibility(hero.detailsWereVisible);
            Destroy(hero);
            var game = GameManager.Instance;
            if (game && game.Camera) game.Camera.StopHeroView();
        }
        Changed?.Invoke();
    }

    /// <summary>The style's level: ranged, magic or healing, or for melee the average of attack, strength and defense.</summary>
    public int StyleLevel
    {
        get
        {
            var s = Player.Stats;
            switch (Style)
            {
                case HeroStyle.Ranged: return s.Ranged.Level;
                case HeroStyle.Magic: return s.Magic.Level;
                case HeroStyle.Healing: return s.Healing.Level;
                default: return (s.Attack.Level + s.Strength.Level + s.Defense.Level) / 3;
            }
        }
    }

    public string StyleName => StyleNameOf(Style);

    public static string StyleNameOf(HeroStyle style)
    {
        switch (style)
        {
            case HeroStyle.Ranged: return Localization.UI_HERO_STYLE_RANGED;
            case HeroStyle.Magic: return Localization.UI_HERO_STYLE_MAGIC;
            case HeroStyle.Healing: return Localization.UI_HERO_STYLE_HEALING;
            default: return Localization.UI_HERO_STYLE_MELEE;
        }
    }

    private static AttackType AttackTypeOf(HeroStyle style)
    {
        switch (style)
        {
            case HeroStyle.Ranged: return AttackType.Ranged;
            case HeroStyle.Magic: return AttackType.Magic;
            case HeroStyle.Healing: return AttackType.Healing;
            default: return AttackType.Melee;
        }
    }

    private string meleeArgument = "all";

    /// <summary>Tab: the next style (Shift+Tab the one before), after a short wait between switches.</summary>
    public void NextStyle(int step)
    {
        if (Time.time < StyleReadyAt) { Refused?.Invoke(-3, "cooldown"); return; }
        SetStyle((HeroStyle)(((int)Style + step + 4) % 4));
    }

    /// <summary>The style's kit, weapon, and task (its experience and its basic attack).</summary>
    private void SetStyle(HeroStyle style)
    {
        Style = style;
        StyleReadyAt = Time.time + StyleSwitchCooldown;
        kit.Clear();
        kit.AddRange(HeroSkill.For(style));
        holyTarget = null;
        rapidFireUntil = 0f;
        Player.taskTarget = null;
        Player.InterruptAction();
        var argument = style == HeroStyle.Ranged ? "ranged" : style == HeroStyle.Magic ? "magic" : style == HeroStyle.Healing ? "healing" : meleeArgument;
        Player.SetManualTask(TaskType.Fighting, argument, null, null);
        Player.Equipment.ShowWeapon(AttackTypeOf(style));
        Changed?.Invoke();
    }

    /// <summary>Damage the hero takes, after the aura, a dodge's moment and the war cry's guard; the guardian angel
    /// keeps a blow from felling the hero once.</summary>
    public int AdjustTaken(int damage)
    {
        if (Time.time < invulnerableUntil) return 0;
        var factor = Aura * (Time.time < guardUntil ? 0.5f : 1f);
        var taken = Mathf.Max(damage > 0 ? 1 : 0, Mathf.RoundToInt(damage * factor));
        if (angel && taken >= Player.Stats.Health.CurrentValue)
        {
            angel = false;
            Player.Heal(Player.Stats.Health.MaxLevel / 2);
            HeroEffects.Burst(HeroEffects.Get?.Angel, Player.Position + Vector3.up, 2.5f, 3f);
            invulnerableUntil = Time.time + 1f;
            return 0;
        }
        return taken;
    }

    public void GuardianAngel() => angel = true;
    public bool HasGuardianAngel => angel;

    public void StartRapidFire(float seconds)
    {
        rapidFireUntil = Time.time + seconds;
        nextRapidArrow = Time.time;
    }

    public bool RapidFire => Time.time < rapidFireUntil;

    private void Update()
    {
        if (!Player || Active != this) return;
        if (!Player.Controlled || !Player.InRavebridge) { End(); return; }
        ShowOwnTags(false);
        if (Player.Stats.IsDead) return;
        if (!heroCamera) heroCamera = GameManager.Instance ? GameManager.Instance.Camera.HeroView : null;

        Mana = Mathf.Min(MaxMana, Mana + ManaPerSecond * Time.deltaTime);
        if (heroCamera && heroCamera.Aim(out var point)) aim = point;
        if (Time.time < spinUntil) Spin();
        else if (spinUntil > 0f) StopSpin();
        if (busy) return;

        if (!Keybinds.IsTyping)
        {
            if (Input.GetKeyDown(KeyCode.V) && heroCamera) heroCamera.Toggle();
            if (Input.GetKeyDown(KeyCode.C)) ToggleDetails(false);
            if (Input.GetKeyDown(KeyCode.I)) ToggleDetails(true);
            if (Input.GetKeyDown(KeyCode.Tab)) NextStyle(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1);
            if (Input.GetKeyDown(KeyCode.Space)) { Dodge(); return; }
            if (Input.GetKeyDown(KeyCode.Q)) Potion();
            for (var i = 0; i < 4; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Cast(i);
            if (busy) return;

            // WASD steers the hero straight away: the agent's velocity, not a path, so it starts, turns and stops at once
            var h = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            var v = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            if (h != 0f || v != 0f)
            {
                var direction = ((heroCamera ? heroCamera.Forward : Flat(transform.forward)) * v + (heroCamera ? heroCamera.Right : Flat(transform.right)) * h).normalized;
                Steer(direction);
                return;
            }
            if (steering) StopSteering();
        }

        // the left mouse: an enemy under the cursor is attacked; the ground is walked to, and holding the button keeps
        // walking toward the cursor
        if (Input.GetMouseButtonDown(0) && !OverHud()) Click();
        else if (Input.GetMouseButton(0) && holdingToMove && Time.time >= nextHoldMove)
        {
            nextHoldMove = Time.time + 0.12f;
            WalkTo(aim);
        }
        if (!Input.GetMouseButton(0)) holdingToMove = false;
        if (holyTarget) HolyBolts();
        else if (Player.taskTarget != null) Player.DoManualTask();
        if (RapidFire) RapidArrows();
    }

    /// <summary>The healer's basic attack: bolts of holy light at the clicked enemy, from up to 10 m away.</summary>
    private void HolyBolts()
    {
        if (holyTarget.Stats.IsDead || !holyTarget.gameObject.activeInHierarchy) { holyTarget = null; return; }
        var distance = Flat(holyTarget.Position - Player.Position).magnitude;
        if (distance > HolyBoltRange)
        {
            if (Time.time >= nextHoldMove)
            {
                nextHoldMove = Time.time + 0.25f;
                Player.SetDestination(holyTarget.Position);
            }
            return;
        }
        if (Player.Movement.IsMoving) Player.SetDestination(Player.Position);
        Face(holyTarget.Position);
        if (Time.time < nextHolyBolt) return;
        nextHolyBolt = Time.time + HolyBoltInterval / Mathf.Max(0.1f, Player.GetModifiers().CastSpeedMultiplier);
        Player.Equipment.ShowWeapon(AttackType.Healing);
        Play("Hero-Cast", 0.05f);
        var target = holyTarget;
        Shoot(HeroEffects.Get?.HolyBolt, target, target.Position, 22f, 0f, e => Strike(e, 1f));
    }

    /// <summary>Rapid fire: extra arrows at the enemy the hero fights, three a second.</summary>
    private void RapidArrows()
    {
        if (Time.time < nextRapidArrow || !(Player.taskTarget is EnemyController target) || !target || target.Stats.IsDead) return;
        if (Flat(target.Position - Player.Position).magnitude > 16f) return;
        nextRapidArrow = Time.time + 0.33f;
        Face(target.Position);
        Play("Hero-Shoot", 0.03f);
        Shoot(HeroEffects.Get?.Arrow, target, target.Position, 32f, 0.4f, e => Strike(e, 0.6f));
    }

    /// <summary>C: the hero's details; I: with the gear open. Pressed again, closed.</summary>
    private void ToggleDetails(bool gear)
    {
        var game = GameManager.Instance;
        var details = game ? game.Camera.Observer : null;
        if (!details) return;
        details.Observe(Player, float.MaxValue);
        if (!details.IsVisible)
        {
            details.SetVisibility(true);
            if (gear != PlayerDetails.IsExpanded) details.ToggleInventory();
            return;
        }
        if (gear && !PlayerDetails.IsExpanded) { details.ToggleInventory(); return; }
        details.SetVisibility(false);
    }

    private bool steering;
    private bool holdingToMove;
    private float nextHoldMove;
    private NavMeshAgent agent;

    private void Steer(Vector3 direction)
    {
        if (!agent) agent = Player.GetComponent<NavMeshAgent>();
        Player.taskTarget = null;
        holyTarget = null;
        holdingToMove = false;
        if (!steering) Player.InterruptAction();
        Player.Movement.Unlock();
        if (!agent || !agent.isOnNavMesh) return;
        // a short path ahead keeps the agent going (with none it drops any velocity), and the velocity makes it start
        // and turn at once
        if (NavMesh.SamplePosition(Player.Position + direction * 2f, out var ahead, 1.5f, agent.areaMask)) agent.SetDestination(ahead.position);
        agent.velocity = direction * agent.speed;
        var turn = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, turn, 900f * Time.deltaTime);
        steering = true;
    }

    private void StopSteering()
    {
        steering = false;
        if (agent && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    private void Click()
    {
        var enemy = EnemyUnderCursor();
        if (!enemy && heroCamera && heroCamera.Mode == HeroCamera.View.Shoulder)
        {
            // over the shoulder: what is in front
            enemy = Closest(Player.Position, 8f, heroCamera.Forward, 45f);
        }
        if (enemy)
        {
            holdingToMove = false;
            HeroMarker.Show(enemy.Position, HeroMarker.Attack, 1.8f);
            Player.Equipment.ShowWeapon(AttackTypeOf(Style));
            if (Style == HeroStyle.Healing)
            {
                // healing has no attack of its own in the game: the hero's is a bolt of holy light
                Player.taskTarget = null;
                holyTarget = enemy;
                return;
            }
            if (!ReferenceEquals(Player.taskTarget, enemy)) Player.SetManualTask(TaskType.Fighting, Player.GetTaskArgument(), enemy.GetComponentInParent<Chunk>(), enemy);
            return;
        }
        if (heroCamera && heroCamera.Mode == HeroCamera.View.Shoulder) return;
        holdingToMove = true;
        nextHoldMove = Time.time + 0.12f;
        if (TryGround(aim, out var spot)) HeroMarker.Show(spot, HeroMarker.Walk);
        WalkTo(aim);
    }

    private void WalkTo(Vector3 point)
    {
        if (!TryGround(point, out var ground)) return;
        Player.taskTarget = null;
        holyTarget = null;
        Player.InterruptAction();
        Player.SetDestination(ground);
    }

    private static readonly RaycastHit[] cursorHits = new RaycastHit[16];

    /// <summary>The enemy the mouse points at (its collider), or the one standing closest to the point on the ground.</summary>
    private EnemyController EnemyUnderCursor()
    {
        var cam = Camera.main;
        if (cam && heroCamera && heroCamera.Mode == HeroCamera.View.TopDown)
        {
            var count = Physics.RaycastNonAlloc(cam.ScreenPointToRay(Input.mousePosition), cursorHits, 200f, ~0, QueryTriggerInteraction.Collide);
            EnemyController best = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var e = cursorHits[i].collider ? cursorHits[i].collider.GetComponentInParent<EnemyController>() : null;
                if (e && !e.Stats.IsDead && cursorHits[i].distance < bestDistance) { bestDistance = cursorHits[i].distance; best = e; }
            }
            if (best) return best;
        }
        return EnemyAt(aim, 1.5f);
    }

    /// <summary>The hero's own name tag and health bar stand in the way, over the shoulder; the health orb shows it.</summary>
    private void ShowOwnTags(bool show)
    {
        var game = GameManager.Instance;
        var tag = game && game.NameTags ? game.NameTags.Get(Player) : null;
        if (tag && tag.gameObject.activeSelf != show) tag.gameObject.SetActive(show);
        var bar = Player ? Player.HealthBar : null;
        if (bar && bar.gameObject.activeSelf != show) bar.gameObject.SetActive(show);
    }

    private static bool OverHud() => Shinobytes.UI.HudPointer.IsOverHud() || UIUtility.IsPointerOverUIElement() || Shinobytes.UI.DraggableWindow.IsPointerOverWindow;

    // ---- Attacking ----------------------------------------------------------------------------------------

    /// <summary>A skill hit: the style's damage times the skill's, the kill counted as any other. (Not Hit: the animation
    /// clips send a Hit event to the character's components.)</summary>
    public void Strike(EnemyController enemy, float multiplier)
    {
        if (!enemy || enemy.Stats.IsDead || !Player) return;
        var chunk = enemy.GetComponentInParent<Chunk>();
        if (chunk != null) Player.Chunk = chunk;
        float raw;
        switch (Style)
        {
            case HeroStyle.Ranged: raw = GameMath.CalculateRangedDamage(Player, enemy); break;
            case HeroStyle.Magic: raw = GameMath.CalculateMagicDamage(Player, enemy); break;
            case HeroStyle.Healing: raw = GameMath.CalculateHealing(Player, enemy); break;
            default: raw = GameMath.CalculateMeleeDamage(Player, enemy); break;
        }
        var damage = Mathf.Max(1, Mathf.RoundToInt(raw * multiplier));
        Player.SessionStats.AddDamageDealt(damage);
        if (enemy.TakeDamage(Player, damage)) Player.Combat.Defeated(enemy);
    }

    /// <summary>Ravebridge's players near a place, the hero among them, still standing.</summary>
    public IEnumerable<PlayerController> AlliesNear(Vector3 center, float radius)
    {
        var game = GameManager.Instance;
        if (!game || game.Players == null) yield break;
        foreach (var p in game.Players.GetAllPlayers())
        {
            if (p && p.InRavebridge && !p.Stats.IsDead && Flat(p.Position - center).magnitude <= radius) yield return p;
        }
    }

    /// <summary>Heals a player by the hero's healing (at least half its healing level), times the skill's.</summary>
    public void HealAlly(PlayerController p, float multiplier, bool effect = true)
    {
        if (!p || p.Stats.IsDead) return;
        var amount = Mathf.Max(GameMath.CalculateHealing(Player, p), Player.Stats.Healing.Level * 0.5f, 1f) * multiplier;
        var heal = Mathf.Min(Mathf.RoundToInt(amount), p.Stats.Health.MaxLevel);
        if (!p.Heal(heal)) return;
        Player.SessionStats.AddHealingDealt(heal);
        if (effect) HeroEffects.Burst(HeroEffects.Get?.Mend, p.Position + Vector3.up * 0.2f, 1f, 2f);
    }

    /// <summary>Smite hits the undead twice as hard: ghosts, souls, skeletons and the like, by name.</summary>
    public static bool IsUndead(EnemyController e)
    {
        if (!e) return false;
        var name = e.name.ToLowerInvariant();
        return name.Contains("soul") || name.Contains("ghost") || name.Contains("skeleton") || name.Contains("zombie") || name.Contains("undead") || name.Contains("lich") || name.Contains("wraith");
    }

    /// <summary>
    /// A projectile from the hero to an enemy (followed while it flies) or, with none, to a place; the enemy's hit, or
    /// what happens where it lands.
    /// </summary>
    public void Shoot(GameObject prefab, EnemyController target, Vector3 end, float speed, float height, Action<EnemyController> hit, Action<Vector3> landed = null, float hitScale = 1f)
    {
        var last = target ? target.Position + Vector3.up : end;
        StartCoroutine(HeroEffects.Fly(prefab, CastPoint, () =>
        {
            if (target && !target.Stats.IsDead) last = target.Position + Vector3.up;
            return last;
        }, speed, height, at =>
        {
            if (target && !target.Stats.IsDead) hit?.Invoke(target);
            landed?.Invoke(at);
        }, hitScale));
    }

    /// <summary>Where the hero's projectiles leave from: in front, at the chest.</summary>
    public Vector3 CastPoint => Player.Position + Vector3.up * 1.3f + Forward * 0.6f;

    /// <summary>The way the hero faces, flat.</summary>
    public Vector3 Forward
    {
        get
        {
            var f = Flat(transform.forward);
            return f.sqrMagnitude > 0.0001f ? f.normalized : Vector3.forward;
        }
    }

    /// <summary>A point, brought within a distance of the hero.</summary>
    public Vector3 Within(Vector3 point, float distance)
    {
        var from = Player.Position;
        var flat = Flat(point - from);
        return flat.magnitude > distance ? from + flat.normalized * distance + Vector3.up * (point.y - from.y) : point;
    }

    /// <summary>The enemy under the cursor, or the one closest to the cursor's place within a radius.</summary>
    public EnemyController EnemyNearCursor(float radius)
    {
        var e = EnemyUnderCursor();
        return e ? e : EnemyAt(aim, radius);
    }

    public IEnumerable<EnemyController> EnemiesNear(Vector3 center, float radius)
    {
        RefreshEnemies();
        foreach (var e in enemies)
        {
            if (e && !e.Stats.IsDead && e.gameObject.activeInHierarchy && Flat(e.Position - center).magnitude <= radius) yield return e;
        }
    }

    public IEnumerable<EnemyController> EnemiesInArc(float radius, float halfAngle)
    {
        foreach (var e in EnemiesNear(Player.Position, radius))
        {
            var to = Flat(e.Position - Player.Position);
            if (to.sqrMagnitude < 0.25f || Vector3.Angle(Flat(transform.forward), to) <= halfAngle) yield return e;
        }
    }

    private EnemyController EnemyAt(Vector3 point, float radius)
    {
        EnemyController best = null;
        var bestDistance = radius;
        foreach (var e in EnemiesNear(point, radius))
        {
            var d = Flat(e.Position - point).magnitude;
            if (d < bestDistance) { bestDistance = d; best = e; }
        }
        return best;
    }

    public EnemyController Closest(Vector3 from, float radius, Vector3 direction, float halfAngle, HashSet<EnemyController> skip = null)
    {
        EnemyController best = null;
        var bestDistance = radius;
        foreach (var e in EnemiesNear(from, radius))
        {
            if (skip != null && skip.Contains(e)) continue;
            var to = Flat(e.Position - from);
            if (direction.sqrMagnitude > 0.01f && Vector3.Angle(direction, to) > halfAngle) continue;
            if (to.magnitude < bestDistance) { bestDistance = to.magnitude; best = e; }
        }
        return best;
    }

    private void RefreshEnemies()
    {
        if (Time.time < enemiesAt && enemies.Length > 0) return;
        enemiesAt = Time.time + 2f;
        enemies = Player.Island ? Player.Island.GetComponentsInChildren<EnemyController>() : Array.Empty<EnemyController>();
    }

    private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    public void Face(Vector3 point)
    {
        var to = Flat(point - Player.Position);
        if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to);
    }

    public Vector3 AimPoint => aim;

    public void Play(string state, float fade = 0.08f)
    {
        if (animator) animator.CrossFadeInFixedTime(state, fade, 0);
    }

    // ---- Skills, dodge, potion -----------------------------------------------------------------------------

    public void Cast(int slot)
    {
        if (slot < 0 || slot >= kit.Count) return;
        var skill = kit[slot];
        var ready = readyAt[(int)Style];
        if (StyleLevel < skill.UnlockLevel) { Refused?.Invoke(slot, "locked"); return; }
        if (Time.time < ready[slot]) { Refused?.Invoke(slot, "cooldown"); return; }
        if (Mana < skill.Mana) { Refused?.Invoke(slot, "mana"); return; }
        if (!skill.Run(this)) { Refused?.Invoke(slot, "target"); return; }
        Mana -= skill.Mana;
        ready[slot] = Time.time + skill.Cooldown;
    }

    private void Dodge()
    {
        if (Time.time < DodgeReadyAt) { Refused?.Invoke(-1, "cooldown"); return; }
        var h = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
        var v = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        var direction = (h != 0f || v != 0f) && heroCamera ? (heroCamera.Forward * v + heroCamera.Right * h).normalized : Flat(transform.forward).normalized;
        if (!TryGround(Player.Position + direction * DodgeDistance, out var to)) return;
        DodgeReadyAt = Time.time + DodgeCooldown;
        invulnerableUntil = Time.time + DodgeSeconds + 0.1f;
        Face(to);
        Play("Hero-Roll", 0.05f);
        StartCoroutine(Move(to, DodgeSeconds, 0f, null));
    }

    private void Potion()
    {
        if (Potions <= 0 || Time.time < potionReadyAt) { Refused?.Invoke(-2, Potions <= 0 ? "empty" : "cooldown"); return; }
        Potions--;
        potionReadyAt = Time.time + 1f;
        Player.Heal(Mathf.RoundToInt(Player.Stats.Health.MaxLevel * PotionShare));
        Play("Hero-Drink");
    }

    /// <summary>A place on the island's navmesh near a point.</summary>
    public bool TryGround(Vector3 point, out Vector3 ground)
    {
        var areas = NavMeshHelper.AreaMaskAt(point);
        if (NavMesh.SamplePosition(point, out var hit, 3f, areas)) { ground = hit.position; return true; }
        ground = point;
        return false;
    }

    /// <summary>The hero moves itself (a dodge, a leap): an arc of the given height, then back on the navmesh.</summary>
    public IEnumerator Move(Vector3 to, float seconds, float height, Action landed)
    {
        busy = true;
        Player.taskTarget = null;
        Player.InterruptAction();
        Player.Movement.Lock();
        var from = Player.Position;
        for (var t = 0f; t < seconds; t += Time.deltaTime)
        {
            var k = t / seconds;
            transform.position = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * height;
            yield return null;
        }
        Player.Movement.SetPosition(to);
        Player.Movement.Unlock();
        busy = false;
        landed?.Invoke();
    }

    public void Guard(float seconds) => guardUntil = Time.time + seconds;

    public void StartSpin(float seconds)
    {
        spinUntil = Time.time + seconds;
        nextSpinHit = Time.time + 0.15f;
        Play("Hero-Spin", 0.05f);
    }

    private void Spin()
    {
        if (Time.time < nextSpinHit) return;
        nextSpinHit = Time.time + HeroSkill.SpinInterval;
        foreach (var e in new List<EnemyController>(EnemiesNear(Player.Position, HeroSkill.SpinRadius))) Strike(e, HeroSkill.SpinMultiplier);
    }

    private void StopSpin()
    {
        if (spinUntil <= 0f) return;
        spinUntil = 0f;
        Play("Hero-SpinEnd", 0.05f);
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
    }
}
