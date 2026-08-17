# AI assistant: knowledge, retrieval, and doing what the interface can do

Written after the first assistant shipped and immediately ran out of things it knew. This is a
design, not a plan of record.

**Scope: the assistant on the website**, the bubble in the corner of the dashboard. Not the in-game
chat bot. They may share a knowledge base one day, but they have different audiences, different
authentication and different risks, and conflating them is how the safe answer for one becomes the
wrong answer for the other.

## What exists today

- `AiService` on the official OpenAI SDK and the Responses API, with a bounded tool loop.
- A confirmation gate: a tool declares for itself whether it needs agreeing to, the run stops and
  hands back a description of what it would do, and only `ContinueAsync` executes it. Traced: there
  are exactly two paths to a handler, and a batch containing anything confirmable stops whole.
- `PlayerAssistant`: seven tools for a player (characters, account, skills, inventory, stash, vendor
  stock, move items) and three more for an administrator (server status, live streams, find player).
- `NewsAssistant` for writing announcements.
- Key, model, on/off and a per player daily question cap in server settings.

What is proven: the confirmation gate and the scoping, both by construction rather than by prompt.
What is not: any of it against a live API. Nothing has had a real conversation yet.

## The six things being asked for, in order of how hard they are

1. **A fact store an admin manages.** Easy.
2. **Retrieval the model can reach.** Easy, and the technique is a sizing question rather than a
   design question.
3. **Facts derived from the code**, so the assistant is correct about rules the wiki never wrote
   down. Easy, and the only part of this that cannot decay, provided it reads values rather than
   copying them.
4. **Wiki content.** Medium, and mostly about attribution rather than fetching.
5. **Learning from corrections.** This is the dangerous one.
6. **Doing anything through the chat that can be done through the interface.** This is not a feature
   at all, it is a standing commitment, and it is the one that shapes everything else.

Taking them in the order they were described would deliver the fact store and leave the two that
matter unresolved, so they are dealt with in the order above.

---

## 1. Facts

### Where they live

Two options, and the difference is entirely about the schema situation:

- **A JSON file**, the way announcements already work: one file under
  `FolderPaths.GeneratedDataPath`, written temp-file-and-move. No DDL, diffable, trivially
  backed up, and readable by a person when something looks wrong.
- **A table**, which means hand-run DDL again, because there are no EF migrations and `GameData`
  loads every set eagerly at boot.

**Start with the file.** At a few hundred to a few thousand facts it is the same speed as a table
and considerably easier to inspect, and being able to read the knowledge base in a text editor is
worth a lot while the shape is still being learned. Move to a table when it stops fitting, which is
a decision that can be made later with evidence.

### Shape

```csharp
public sealed class Fact
{
    public Guid Id;
    public string Title;         // what it answers, in a line
    public string Body;          // the answer, markdown
    public string[] Tags;        // "combat", "vendor", "streamer", for filtering
    public FactSource Source;    // Admin, Learned, Wiki
    public FactStatus Status;    // Published, Proposed, Retired
    public string CreatedBy;     // a user name, or "assistant"
    public DateTime CreatedUtc;
    public DateTime UpdatedUtc;
    public Guid? Supersedes;     // the fact this replaces, when it replaces one
    public int TimesUsed;        // retrieval count, for finding the load bearing ones
    public DateTime? LastUsedUtc;
    public string SourceUrl;     // for wiki facts, where it came from
    public DateTime? FetchedUtc; // for wiki facts, how stale it is
}
```

`Supersedes` rather than editing in place, so a wrong answer that got published can be traced back
to when it was introduced. `TimesUsed` because the fact nobody ever retrieves is not the one worth
arguing about, and the fact retrieved constantly is the one worth getting right.

### Managing them

An admin page: list, search by tag and text, add, edit, retire. Retire rather than delete, for the
same reason as `Supersedes`.

---

## 2. Retrieval

### Vectors are not needed yet, and that is a measurement not an opinion

The instruction was "if it uses a vectorization or what not, I don't care which or how, as long as
it works". So the honest answer is what works at the size this will actually be.

At a few hundred facts, keyword and tag search over titles and bodies will beat a poorly tuned
vector store, and when it returns the wrong thing you can see why. A vector store that returns the
wrong thing is a debugging session. Embeddings become worth it when the corpus is large enough that
keyword misses are common, and that is measurable rather than guessable: log the retrievals that
returned nothing, and read them.

So: **a search tool now, embeddings when the miss log says so.** The `Fact` shape above already has
room for a stored vector without changing anything else.

### Retrieval as a tool, never as a prompt stuffing

The whole corpus does not go into the request. The model calls `search_facts(query)` and gets back
the few that matched. Three reasons: cost stays bounded as the corpus grows, the answer can say
which fact it used, and a fact that is never retrieved costs nothing.

---

## 3. Learning from corrections, which is where this gets dangerous

The ask: "if a user corrects the bot, it must remember the corrections so it can self improve."

The failure mode is not subtle: one player, confused or amusing themselves, explains something
false to an assistant that has announced it learns from corrections, and it then tells everybody
else. It does not take malice. Somebody confidently wrong about a mechanic is far more common than
somebody deliberately poisoning it, and produces the same result.

Being on the website rather than in chat helps but does not solve it. The audience is signed in
through Twitch, so a correction has a name on it and can be traced and undone, and the population
is smaller than a Twitch chat. That lowers the odds. It does not make "a correction becomes a fact
everyone sees" a safe rule, and the queue that fixes it costs almost nothing.

### The rule, decided

- **A player's correction becomes a `Proposed` fact.** Nobody else sees it. It appears in an admin
  queue with the conversation that produced it, and an admin accepts, edits or discards it.
- **An admin's or moderator's correction is accepted straight away.** They already have that
  authority through the interface, and the principle below is that the chat is not a second, weaker
  set of rules. Somebody who can fix a page can fix this.
- The assistant may write proposals itself. That is the "create new facts itself" part, and it is
  safe precisely because a proposal is inert until somebody with authority publishes it.
- **Publishing is the privileged act**, not proposing.

Worth being clear about what the website scope changes here. The audience is signed in through
Twitch rather than anonymous, so a bad correction has a name on it and can be traced and undone.
That makes it less likely, not impossible, and the queue costs almost nothing. The rule above is
what was asked for and also what I would have recommended.

### Per-user memory, separately

**"It remembered what I told it" and "it learned something for everybody" are different promises**,
and the first is most of the felt benefit with almost none of the risk. A note attached to one
person, applied only to their own conversations, can be written instantly with no review at all.
Worth having alongside the proposal queue, and worth naming differently in the interface so nobody
thinks they have taught the assistant something everyone will hear.

---

## 4. Doing what the interface can do

> "admin should in theory be able to do anything with the chat that they can do through the
> interface. the same applies for users."

This is the part that is an architecture rather than a feature, and it is worth being explicit
about what it commits us to.

### Three rules that make it checkable rather than aspirational

**A tool wraps a service method, never the data layer.** The precedent is already set and worth
holding to: `move_items` goes through the same `PlayerManager.SendToCharacter` that the Send button
on the inventory page calls. It is the button, pressed by a different finger. Anything that method
refuses, the tool refuses, without the refusal being written twice.

**Authorisation comes from the session, never from an argument.** Also already the pattern: the
admin tools exist only when the session says administrator, and the clan tools resolve permissions
for the acting character. A tool the model cannot see is one it cannot be talked into.

**Anything that changes something goes behind the confirmation gate.** Already built and traced.

Together these mean parity grows one capability at a time and can be prioritised, and that a
capability cannot arrive with weaker rules than the page it mirrors.

### The thing that will bite: tool count

Every tool's name, description and schema is in every request. Ten is nothing. Eighty is a
significant per-question cost paid whether or not any of them is used, on every question, for every
player.

Options, in the order I would reach for them:

1. **Group by area and expose a small set.** The assistant on the vendor page does not need the clan
   tools. Page context is already sent, so the tool set can vary with it.
2. **A two step lookup**: a `capabilities(area)` tool that lists what exists, and the specific tools
   loaded on demand. Costs a round trip, saves the standing overhead.
3. **Role gating**, already in place, which keeps a player's set small even when the admin set is
   large.

This wants deciding before the tool count grows, not after.

### What their example questions actually need

The questions asked are diagnostic, because they split almost evenly:

| Question | Needs |
|---|---|
| "where am I currently?" | live character state. Data we hold, needs a tool. |
| "do I have a pet equipped?" | data we hold (`ActiveBattlePet`), needs a tool. |
| "what weapon should I get?" | item search plus their skills plus what they can afford. Data. |
| "where should I train now?" | character state plus training knowledge. Half data, half facts. |
| "what does weapon aim really do?" | pure game knowledge. Facts or wiki. |
| "how is magic power affecting my magic?" | game mechanics. Facts. |
| "what the hell is a streamer token?" | facts. |
| "can you equip the best weapon for me?" | a mutating action. Needs an equip service method and the confirmation gate. |

**So the fact store on its own answers about half of them.** The other half is tools over data we
already have. Worth saying plainly, because "add a knowledge base" sounds like it fixes this and it
fixes part of it.

---

## 5. The wiki, measured rather than assumed

`ravenfall.fandom.com`. Fandom, so MediaWiki 1.43.9, and **`api.php` answers**: structured fetching
rather than scraping HTML. That closes the question the first draft could not.

What is actually there, as of writing:

| | |
|---|---|
| Pages | 377 |
| Real articles, redirects excluded | **30** |
| Total wikitext | **92.7 KB, roughly 24,000 tokens** |
| Active users | **1** |
| Stubs under 400 bytes | 6 of 30 |

Four things follow, and they matter more than being able to fetch it does.

**It is small enough that retrieval is not a problem.** Thirty articles. A title and keyword match
over thirty things is not a search problem, and anything vector shaped here would be engineering for
a difficulty that does not exist. This is the strongest evidence for starting without embeddings.

**One article is a quarter of the wiki.** `Weapons` is 24.6 KB on its own, about 6,000 tokens, and
it is the article covering "what does weapon aim really do". Too big to hand back whole from a tool.
It wants splitting by section, which the API supports directly, so retrieval returns the section
rather than the page.

**The stubs are exactly where players will ask.** Marketplace, Mining, Woodcutting, Farming and
Fishing are all under 400 bytes. Those are skills people train, which is the "where should I train
now" category. So ingesting the wiki does not answer the training questions. The fact store has to,
and the proposal queue is how those gaps get found.

**One active user.** It is not a living source that will fill its own gaps, so a refresh can be
infrequent and nothing should be designed assuming the wiki improves. Community maintained also
means it is not ours and not authoritative: link to it, quote briefly with attribution, and let a
wrong answer be traceable to it.

Ingest into the same fact store with `Source = Wiki`, a `SourceUrl` and a `FetchedUtc`, so retrieval
stays uniform and staleness is visible. Always surface the link: the best answer to a deep question
is a correct short one plus a pointer to the page with the detail.

## 6. Facts derived from the code

The wiki has thirty articles and six of them are stubs, so a lot of what players ask is written
down nowhere except in the code that decides it. Seeding facts from that code at startup is the
right instinct, and there is a trap in the obvious version of it worth naming before anybody builds
it.

### The trap

**A fact seeded as text is a copy of a value, and a copy goes stale silently.** Write "buying from
the vendor costs at least three times what it pays" into a fact at startup, change
`VendorBuyMultiplier` to four a year later, and the assistant now states the old number with total
confidence and an authoritative looking source. That is worse than not knowing: "I do not know"
sends somebody to ask a person, a wrong number does not.

So the rule is: **read the value, never copy it.**

### Tier one: derived facts, which cannot go stale

The body is produced by a function that reads the live value when it is generated, and it is
regenerated on every startup. The prose is a template; the number is interpolated from the constant
itself.

```csharp
Derive("How many characters can I have?",
    () => "Each account can have up to " + PlayerManager.MaxCharacterCount + " characters.");

Derive("How long does a marketplace listing last?",
    () => "A listing expires after " + MarketplaceManager.ListingLifetime.TotalDays + " days.");

Derive("What does the vendor charge?",
    () => "At least " + GameMath.VendorBuyMultiplier + " times what it pays for the same item, and " +
          "never less than " + GameMath.MinimumVendorBuyPrice + " coins.");
```

There is a second guarantee here that is easy to miss and is the best part: **a derived fact
references the constant in C#, so deleting or renaming the constant breaks the build.** The fact
cannot outlive the thing it describes, and it fails at compile time rather than in front of a
player. Nothing else in this document has that property.

Candidates already in the code, all of which are questions somebody will ask:

| Constant | Answers |
|---|---|
| `PlayerManager.MaxCharacterCount` | how many characters can I have |
| `PlayerManager.AutoJoinDungeonCost`, `AutoJoinRaidCost` | what does auto join cost |
| `PlayerManager.AutoRestCostPerSecond` | what does resting cost |
| `PlayerManager.Enchanting_CooldownCoinsPerSecond` | what does skipping the enchant cooldown cost |
| `MarketplaceManager.ListingLifetime` | how long do listings last |
| `SessionManager.MaxPlayerExpMultiplier`, `ExpMultiplierMinutesPerScroll` | how do multipliers work |
| `GameMath.VendorBuyMultiplier`, `MinimumVendorBuyPrice`, `MaxLevel` | vendor prices, level cap |
| `ClanBankDefaults.ForRoleLevel` | what can my clan rank withdraw |

Derived facts should be **not editable** in the admin panel, and say so. An edit would be
overwritten at the next startup, and a field that silently discards what you typed is worse than a
field that refuses.

### The reflection driven ones are even better

Some answers are not a constant but a list the code already enumerates. `SkillsExtended.AsList()`
walks every skill by reflection, which is why adding a skill to the game made it appear in the
assistant without anybody remembering to. The same shape covers the item catalogue, the command
list and the clan rank defaults.

These are the best kind of derived fact: **the fact is the query**. It cannot drift because there
is nothing to drift from.

### Tier two: guarded facts, for prose nobody can generate

Most of what makes a good answer is not a number. "Weapon aim increases how often you hit in melee,
so it matters more against high defence targets than raw power does" is a sentence a person writes,
and no generator produces it. But it depends on values the code owns, and those can be watched.

A hand written fact can declare what it depends on:

```csharp
public sealed class Fact
{
    // ... as above ...

    /// <summary>Values from the code this fact's wording depends on, and what they were when it
    /// was written. Checked at startup; a mismatch marks the fact stale rather than wrong.</summary>
    public Dictionary<string, string> DependsOn;
}
```

At startup, each declared value is read again and compared. A mismatch does not delete the fact and
does not correct it, because neither can be done safely by a machine. It marks the fact **stale**
and puts it in the same admin queue the player corrections use, saying what changed: *"this fact
was written when VendorBuyMultiplier was 3, it is now 4"*.

That is the answer to keeping these current as the game changes, and it is mechanical rather than a
discipline somebody has to remember during a refactor.

### What this does and does not cover

It answers **how much, how many, how long, what are the rules**. That is a real and useful slice,
and it is precisely the slice the wiki's stubs leave open.

It does not answer **where should I train, what weapon should I get, is this worth it**. Those are
judgement, and they come from admin written facts, the wiki, and eventually the accepted player
corrections. Worth sizing honestly: derivation makes the assistant reliably correct about rules,
not knowledgeable about the game.

---

## Suggested order

1. **Fact store, admin management, and a search tool.** The smallest change that makes answers
   better, and it teaches us what people actually ask.
2. **Proposals and corrections**, including per-user notes. Makes it improve without letting
   anybody poison it.
3. **Data tools for the pure lookups**: character state, pet, item search, training locations.
   This is where half those example questions get answered.
4. **Derived facts from the code**, plus the staleness guard for hand written ones. Cheap,
   cannot rot, and it covers the rules questions the wiki's stubs leave open.
5. **Wiki ingestion.** Thirty articles, so one pass rather than a pipeline, with `Weapons` split
   by section because it is a quarter of the corpus on its own.
6. **Embeddings**, if and when the retrieval miss log justifies it.
7. **Mutating tools** such as equipping, behind the gate that already exists.

Steps 1 and 3 together are what change the experience. Step 2 is what stops step 1 becoming a
liability. Step 4 is the cheapest of the lot and the only part that cannot decay, so it is worth
doing before the corpus is large enough to hide a stale answer in.

## Open questions

1. **Facts in a file or a table?** I would start with a file, for no DDL and readability. Moving
   later is cheap; starting with DDL is not.
2. ~~**Do moderator corrections publish immediately?**~~ Answered: admins and moderators both
   publish directly, players queue for review.
3. **Per-user memory: yes?** It is most of the felt benefit for almost none of the risk, and it is a
   different promise from global learning. If yes, it needs its own name in the interface.
4. **What is the cost ceiling?** The daily question cap exists, but retrieval and a larger tool set
   multiply the tokens per question. A monthly budget would let the defaults be set from something
   real rather than from caution.
5. ~~**Is the wiki API open?**~~ Answered: yes. Fandom, MediaWiki 1.43.9, and the whole thing is
   thirty articles.
6. **How much should the assistant volunteer that it does not know?** A bot that says "I have no
   fact about that, I have told an admin" is more useful than one that reasons plausibly from
   nothing, and it also produces the proposal queue for free. But it is a different personality and
   worth choosing deliberately.
