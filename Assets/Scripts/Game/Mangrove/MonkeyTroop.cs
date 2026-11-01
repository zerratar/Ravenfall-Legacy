using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The Mangrove island's monkeys (docs/banana-island.md): a troop spread over the island's spots, the chief on the jetty
/// under the name the streamer gave him in the story, and their business: now and then a monkey runs up to someone
/// training on the island, snatches something and runs off; the player chases it and either gets it back or the monkey
/// eats it. Only a show: nothing leaves anyone's inventory. How often depends on the banana pile (BananaPile), which
/// this ticks. In potato mode only a few monkeys come out.
/// </summary>
public sealed class MonkeyTroop : MonoBehaviour
{
    public const string ChiefKey = "monkey_chief";

    [SerializeField] private Monkey monkeyPrefab;
    [SerializeField] private int count = 12;
    [SerializeField] private int potatoCount = 4;
    /// <summary>Where the monkeys live (palms, huts, boardwalks), placed by tools/mangrove/mangrove-monkeys.cs.</summary>
    [SerializeField] private Transform[] homes;

    private const float ReachDistance = 1.8f;
    private const float ChaseSeconds = 7f;
    private const float FleeDistance = 16f;
    private const float FirstTheftAfter = 90f;

    private static readonly string[] Names =
    {
        "Gary", "Dave", "Banana Joe", "Kevin's cousin", "Sir Peel", "Mango", "Bobo", "Coco", "Ook", "Mr. Fluffles",
        "Big Steve", "Tiny Steve", "Chuckles", "Doris", "Nibbles", "Captain Grabby",
    };

    private readonly List<Monkey> monkeys = new List<Monkey>();
    private IslandController island;
    private IslandNpc chief;
    private float nextTheft;
    private bool spawned;
    private bool potato;

    public static MonkeyTroop Instance { get; private set; }
    public IReadOnlyList<Monkey> Monkeys => monkeys;
    public IslandController Island => island;

    private void Awake()
    {
        Instance = this;
        island = GetComponentInParent<IslandController>();
    }

    private void Start()
    {
        nextTheft = Time.time + FirstTheftAfter;
        BananaPile.Changed += OnPileChanged;
        RegattaRecords.UpdateBoard(island);
    }

    private void OnDestroy()
    {
        BananaPile.Changed -= OnPileChanged;
        BananaPile.Save();
        if (Instance == this) Instance = null;
    }

    private void OnApplicationQuit() => BananaPile.Save();

    private void OnPileChanged()
    {
        StoryLog.ApplyWorld();
        if (island && !island.Locked && island.GetPlayers().Count > 0)
            MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_BANANAS_MOOD_CHANGED, BananaPile.MoodName), "");
    }

    private void Update()
    {
        var game = GameManager.Instance;
        var open = island && !island.Locked;
        if (open) BananaPile.Tick(Time.deltaTime);

        var wantPotato = game && game.PotatoMode;
        if (!spawned || wantPotato != potato) Spawn(wantPotato);
        if (!chief) FindChief();

        if (open)
        {
            UpdateBusiness();
            PirateRaid.Think(island);
        }
    }

    // ---- The troop ---------------------------------------------------------------------------------

    private void Spawn(bool potatoMode)
    {
        if (!monkeyPrefab) return;
        // the island's navmesh must be there before an agent can stand on it
        if (!NavMesh.SamplePosition(HomeAt(0), out _, 6f, NavMesh.AllAreas)) return;
        potato = potatoMode;
        spawned = true;
        var want = potatoMode ? potatoCount : count;
        while (monkeys.Count > want)
        {
            var last = monkeys[monkeys.Count - 1];
            monkeys.RemoveAt(monkeys.Count - 1);
            if (last) Destroy(last.gameObject);
        }
        for (var i = monkeys.Count; i < want; ++i)
        {
            var home = HomeAt(i);
            if (!NavMesh.SamplePosition(home, out var hit, 6f, NavMesh.AllAreas)) continue;
            var monkey = Instantiate(monkeyPrefab, hit.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            monkey.name = "Monkey " + Names[i % Names.Length];
            monkey.MonkeyName = Names[i % Names.Length];
            monkey.Home = hit.position;
            monkeys.Add(monkey);
        }
    }

    private Vector3 HomeAt(int i)
    {
        if (homes != null && homes.Length > 0)
        {
            var home = homes[i % homes.Length];
            if (home) return home.position;
        }
        return transform.position;
    }

    // ---- The chief ---------------------------------------------------------------------------------

    private void FindChief()
    {
        chief = IslandNpc.Find(ChiefKey);
        if (!chief) return;
        chief.Lines = ChiefLines;
        chief.MarkSource = () => StoryLog.MarkFor("bananas");
    }

    /// <summary>The chief's name: what the streamer called him in the story (Bonzo, Sir Bananas or Kevin), or "the monkey chief".</summary>
    public static string ChiefName
    {
        get
        {
            var story = StoryLog.Story("bananas");
            var choices = story?.Choices;
            if (!string.IsNullOrEmpty(choices))
            {
                foreach (var part in choices.Split(','))
                {
                    var kv = part.Split('=');
                    if (kv.Length != 2 || !int.TryParse(kv[1], out var option)) continue;
                    switch (option)
                    {
                        case 1: return Localization.UI_STORY_BANANAS_CHOICE_1;
                        case 2: return Localization.UI_STORY_BANANAS_CHOICE_2;
                        case 3: return Localization.UI_STORY_BANANAS_CHOICE_3;
                    }
                }
            }
            return Localization.UI_STORY_BANANAS_NPC;
        }
    }

    private static IReadOnlyList<string> ChiefLines()
    {
        var lines = new List<string>();
        var name = ChiefName;
        var mood = BananaPile.Current switch
        {
            BananaPile.Mood.Abundant => Localization.UI_CHIEF_LINES_ABUNDANT,
            BananaPile.Mood.Sufficient => Localization.UI_CHIEF_LINES_SUFFICIENT,
            BananaPile.Mood.Barely => Localization.UI_CHIEF_LINES_BARELY,
            _ => Localization.UI_CHIEF_LINES_INSUFFICIENT,
        };
        foreach (var line in mood) lines.Add(Format(line, name));
        foreach (var line in Localization.UI_CHIEF_LINES) lines.Add(Format(line, name));
        return lines;
    }

    private static string Format(string line, string name) =>
        line.Replace("{chief}", name).Replace("{bananas}", BananaPile.AmountText);

    /// <summary>The streamer clicked the chief: he says how the pile stands.</summary>
    public void ChiefClicked()
    {
        if (!chief) FindChief();
        if (!chief) return;
        chief.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_CHIEF_PILE, ChiefName, BananaPile.AmountText, BananaPile.MoodName));
    }

    /// <summary>The streamer clicked a monkey: it shows off.</summary>
    public void MonkeyClicked(Monkey monkey)
    {
        if (!monkey || monkey.Busy) return;
        monkey.BeatChest();
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_MONKEY_POKED, monkey.MonkeyName));
    }

    // ---- Monkey business ---------------------------------------------------------------------------

    private void UpdateBusiness()
    {
        if (Time.time < nextTheft) return;
        nextTheft = Time.time + BananaPile.StealEvery * Random.Range(0.6f, 1.4f);
        var victim = PickVictim();
        if (!victim) return;
        var monkey = NearestFree(victim.Position);
        if (!monkey) return;
        StartCoroutine(Theft(monkey, victim));
    }

    /// <summary>Starts a theft now (testing, or the chief's grumpy mood).</summary>
    public bool StealNow(PlayerController victim = null)
    {
        victim = victim ? victim : PickVictim();
        if (!victim) return false;
        var monkey = NearestFree(victim.Position);
        if (!monkey) return false;
        StartCoroutine(Theft(monkey, victim));
        return true;
    }

    private PlayerController PickVictim()
    {
        if (!island) return null;
        var candidates = new List<PlayerController>();
        foreach (var p in island.GetPlayers())
        {
            if (!p || p.Controlled || p.Errand != null || p.InRavebridge || p.Stats.IsDead) continue;
            if (ThingsFor(p.GetTask()) == null) continue;
            candidates.Add(p);
        }
        return candidates.Count == 0 ? null : candidates[Random.Range(0, candidates.Count)];
    }

    private Monkey NearestFree(Vector3 position)
    {
        Monkey best = null;
        var bestDistance = float.MaxValue;
        foreach (var m in monkeys)
        {
            if (!m || m.Busy) continue;
            var d = (m.Position - position).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = m; }
        }
        return best;
    }

    private bool StillHere(PlayerController p) => p && p.Island == island && !p.Stats.IsDead && !p.Controlled;

    private IEnumerator Theft(Monkey monkey, PlayerController victim)
    {
        // up to them, running
        var giveUp = Time.time + 25f;
        while (monkey && StillHere(victim) && Time.time < giveUp && (monkey.Position - victim.Position).sqrMagnitude > ReachDistance * ReachDistance)
        {
            monkey.RunTo(victim.Position);
            yield return new WaitForSeconds(0.25f);
        }
        if (!monkey) yield break;
        if (!StillHere(victim) || Time.time >= giveUp || victim.Errand != null)
        {
            monkey.Settle();
            yield break;
        }

        var thing = Thing(victim.GetTask());
        MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_MONKEY_STOLE, monkey.MonkeyName, victim.Name, thing));

        // off with it, the player after it
        var away = victim.Position + Flat(monkey.Position - victim.Position, Random.insideUnitSphere) * FleeDistance;
        monkey.RunTo(away);
        var chasing = true;
        var nextPath = 0f;
        victim.BeginErrand(p =>
        {
            if (!chasing || !monkey || p.Island != island) return false;
            if (Time.time >= nextPath)
            {
                nextPath = Time.time + 0.35f;
                p.SetDestination(monkey.Position);
            }
            return true;
        });

        var ends = Time.time + ChaseSeconds;
        while (monkey && Time.time < ends && StillHere(victim))
        {
            if (monkey.Arrived && (monkey.Position - victim.Position).sqrMagnitude < 4f) break;
            yield return null;
        }
        chasing = false;
        if (!monkey) yield break;

        var close = StillHere(victim) && (monkey.Position - victim.Position).sqrMagnitude < 9f;
        if (BananaPile.Current == BananaPile.Mood.Abundant)
        {
            MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_MONKEY_GAVE_BACK, monkey.MonkeyName, victim.Name, thing));
        }
        else if (close || Random.value < 0.35f)
        {
            MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_MONKEY_GOT_BACK, victim.Name, thing, monkey.MonkeyName));
            if (victim) victim.Cheer();
        }
        else
        {
            MangroveFeed.Say(string.Format(CultureInfo.InvariantCulture, Localization.UI_MONKEY_ATE, monkey.MonkeyName, victim.Name, thing));
            monkey.Settle();
            monkey.BeatChest();
            yield break;
        }
        monkey.Settle();
    }

    private static Vector3 Flat(Vector3 direction, Vector3 jitter)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
        var d = direction.normalized + new Vector3(jitter.x, 0f, jitter.z) * 0.6f;
        return d.normalized;
    }

    private static string Thing(TaskType task)
    {
        var things = ThingsFor(task) ?? Localization.UI_MONKEY_THINGS_ANY;
        // now and then their hat, whatever they were doing
        if (Random.value < 0.2f) things = Localization.UI_MONKEY_THINGS_ANY;
        return things[Random.Range(0, things.Length)];
    }

    private static string[] ThingsFor(TaskType task) => task switch
    {
        TaskType.Fishing => Localization.UI_MONKEY_THINGS_FISHING,
        TaskType.Woodcutting => Localization.UI_MONKEY_THINGS_WOODCUTTING,
        TaskType.Mining => Localization.UI_MONKEY_THINGS_MINING,
        TaskType.Gathering => Localization.UI_MONKEY_THINGS_GATHERING,
        TaskType.Farming => Localization.UI_MONKEY_THINGS_FARMING,
        TaskType.Cooking => Localization.UI_MONKEY_THINGS_COOKING,
        TaskType.Crafting => Localization.UI_MONKEY_THINGS_CRAFTING,
        TaskType.Alchemy => Localization.UI_MONKEY_THINGS_ALCHEMY,
        _ => null,
    };
}
