using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Hero mode (docs/hero-mode-design.md): the streamer's character played as an action RPG hero. 7a: the hero itself,
/// on top of playing by hand (6c). WASD walks relative to the camera, the mouse aims, the left mouse attacks what is
/// at the cursor (and keeps at it), 1 to 4 are the style's skills, Space dodges, Q drinks a potion, V switches the
/// camera (HeroCamera). Skills cost mana, which refills by itself, and have cooldowns. The hero's aura (how much less
/// damage it takes) is a placeholder until the waves of 7b set it.
/// </summary>
public class HeroController : MonoBehaviour
{
    public const float DodgeCooldown = 3f;
    private const float DodgeDistance = 5f;
    private const float DodgeSeconds = 0.4f;
    private const float PotionShare = 0.35f;
    private const float ManaPerSecond = 4f;
    private const float Aura = 0.5f; // 7a: the hero takes half damage; 7b scales it to the waves

    public static HeroController Active { get; private set; }
    public static event Action Changed;

    public PlayerController Player { get; private set; }
    public float Mana { get; private set; }
    public float MaxMana => 100f;
    public int Potions { get; private set; }
    public int MaxPotions => 3;
    public float DodgeReadyAt { get; private set; }
    public IReadOnlyList<HeroSkill> Kit => kit;
    public float ReadyAt(int slot) => readyAt[slot];

    /// <summary>A skill that could not be used: its slot, for the HUD to flash, and why.</summary>
    public static event Action<int, string> Refused;

    private readonly List<HeroSkill> kit = new List<HeroSkill>();
    private readonly float[] readyAt = new float[4];
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
        hero.kit.AddRange(HeroSkill.Melee);
        Active = hero;

        // the melee style for the kit's damage and experience
        var current = player.GetTask() == TaskType.Fighting ? player.GetTaskArgument() : null;
        var arg = current == "attack" || current == "strength" || current == "defense" ? current : "all";
        player.SetManualTask(TaskType.Fighting, arg, null, null);

        game.Camera.FollowHero(player);
        hero.heroCamera = game.Camera.HeroView;
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
            Destroy(hero);
            var game = GameManager.Instance;
            if (game && game.Camera) game.Camera.StopHeroView();
        }
        Changed?.Invoke();
    }

    /// <summary>The melee style's level: the average of attack, strength and defense.</summary>
    public int StyleLevel => (Player.Stats.Attack.Level + Player.Stats.Strength.Level + Player.Stats.Defense.Level) / 3;

    /// <summary>Damage the hero takes, after the aura, a dodge's moment and the war cry's guard.</summary>
    public int AdjustTaken(int damage)
    {
        if (Time.time < invulnerableUntil) return 0;
        var factor = Aura * (Time.time < guardUntil ? 0.5f : 1f);
        return Mathf.Max(damage > 0 ? 1 : 0, Mathf.RoundToInt(damage * factor));
    }

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
            if (Input.GetKeyDown(KeyCode.Space)) { Dodge(); return; }
            if (Input.GetKeyDown(KeyCode.Q)) Potion();
            for (var i = 0; i < 4; i++) if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Cast(i);
            if (busy) return;

            var h = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            var v = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            if (h != 0f || v != 0f)
            {
                var direction = ((heroCamera ? heroCamera.Forward : transform.forward) * v + (heroCamera ? heroCamera.Right : transform.right) * h).normalized;
                Player.taskTarget = null;
                Player.InterruptAction();
                Player.SetDestination(Player.Position + direction * 2f);
                walking = true;
                return;
            }
            if (walking) { walking = false; Player.SetDestination(Player.Position); }
        }

        if (Input.GetMouseButton(0) && !OverHud()) Attack();
        if (Player.taskTarget != null) Player.DoManualTask();
    }

    private bool walking;

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

    /// <summary>The enemy at the cursor (or the closest one toward it), attacked until it falls.</summary>
    private void Attack()
    {
        var enemy = EnemyAt(aim, 3f) ?? Closest(Player.Position, 8f, Flat(aim - Player.Position), 60f);
        if (!enemy) { Face(aim); return; }
        if (!ReferenceEquals(Player.taskTarget, enemy)) Player.SetManualTask(TaskType.Fighting, Player.GetTaskArgument(), enemy.GetComponentInParent<Chunk>(), enemy);
    }

    /// <summary>A skill hit: the style's damage times the skill's, the kill counted as any other.</summary>
    public void Hit(EnemyController enemy, float multiplier)
    {
        if (!enemy || enemy.Stats.IsDead) return;
        var chunk = enemy.GetComponentInParent<Chunk>();
        if (chunk != null) Player.Chunk = chunk;
        var damage = Mathf.Max(1, Mathf.RoundToInt(GameMath.CalculateMeleeDamage(Player, enemy) * multiplier));
        Player.SessionStats.AddDamageDealt(damage);
        if (enemy.TakeDamage(Player, damage)) Player.Combat.Defeated(enemy);
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

    private EnemyController Closest(Vector3 from, float radius, Vector3 direction, float halfAngle)
    {
        EnemyController best = null;
        var bestDistance = radius;
        foreach (var e in EnemiesNear(from, radius))
        {
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
        if (StyleLevel < skill.UnlockLevel) { Refused?.Invoke(slot, "locked"); return; }
        if (Time.time < readyAt[slot]) { Refused?.Invoke(slot, "cooldown"); return; }
        if (Mana < skill.Mana) { Refused?.Invoke(slot, "mana"); return; }
        if (!skill.Run(this)) return;
        Mana -= skill.Mana;
        readyAt[slot] = Time.time + skill.Cooldown;
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
        foreach (var e in new List<EnemyController>(EnemiesNear(Player.Position, HeroSkill.SpinRadius))) Hit(e, HeroSkill.SpinMultiplier);
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
