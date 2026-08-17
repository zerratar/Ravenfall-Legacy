# AI assistant: knowledge, retrieval, and doing what the interface can do

Written after the first assistant shipped and immediately ran out of things it knew. This is a
design, not a plan of record. The open questions at the end are the ones I would want answered
before building the parts that are hard to undo.

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

## The five things being asked for, in order of how hard they are

1. **A fact store an admin manages.** Easy.
2. **Retrieval the model can reach.** Easy, and the technique is a sizing question rather than a
   design question.
3. **Wiki content.** Medium, and mostly about attribution rather than fetching.
4. **Learning from corrections.** This is the dangerous one.
5. **Doing anything through the chat that can be done through the interface.** This is not a feature
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

The failure mode is not subtle. Ravenfall is played through Twitch chat. Some fraction of any
Twitch chat will, for entertainment, patiently explain something false to a bot that has announced
it learns from corrections. The bot then tells everyone else. There is no version of
"corrections become facts" that survives contact with that.

### So corrections become proposals, not facts

- A correction from a player creates a `Proposed` fact. Nobody but an admin sees it.
- An admin queue shows proposals with the conversation that produced them, and approves, edits or
  discards.
- The assistant may write proposals itself. That is the "create new facts itself" part, and it is
  safe precisely because a proposal is inert.
- **Publishing is the privileged act**, not proposing.

### Two things worth considering on top

**A correction from an admin or moderator could publish immediately.** They already have that
authority through the interface, and the whole principle below is that the chat should not be a
second, weaker set of rules. This is consistent rather than a shortcut.

**Per-user memory is a separate, cheaper thing.** "It remembered what I told it" and "it learned
something for everybody" are different features, and the first is most of the felt benefit with
almost none of the risk. A note attached to one user, applied only to their conversations, can be
written instantly with no review. Worth having as well as the proposal queue, and worth naming
differently in the interface so nobody thinks they have taught the bot something global.

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

## 5. The wiki

`wiki.ravenfall.stream`, linked from About, Commands and Download, and described in our own copy as
**community maintained**. That is the important fact about it.

- It is not ours and it is not authoritative. So: link to it, quote it briefly with attribution,
  and never present it as fact of record. A wrong answer sourced from the wiki should be traceable
  to the wiki.
- The URL shape (`index.php/Page_Name`) looks like MediaWiki, which would mean `api.php` allows
  structured fetching rather than scraping HTML. **Unverified**: it was not reachable from the
  machine this was written on, which may be this environment's networking rather than the wiki
  being down. Worth confirming before planning around it.
- Ingest titles, summaries and URLs into the same fact store with `Source = Wiki` and a
  `FetchedUtc`, on a schedule, so retrieval stays uniform and staleness is visible.
- Always surface the link. The best outcome for a deep question is a correct short answer and a
  pointer to the page that has the detail.

---

## Suggested order

1. **Fact store, admin management, and a search tool.** The smallest change that makes answers
   better, and it teaches us what people actually ask.
2. **Proposals and corrections**, including per-user notes. Makes it improve without letting
   anybody poison it.
3. **Data tools for the pure lookups**: character state, pet, item search, training locations.
   This is where half those example questions get answered.
4. **Wiki ingestion**, once the API question is settled.
5. **Embeddings**, if and when the retrieval miss log justifies it.
6. **Mutating tools** such as equipping, behind the gate that already exists.

Steps 1 and 3 together are what change the experience. Step 2 is what stops step 1 becoming a
liability.

## Open questions

1. **Facts in a file or a table?** I would start with a file, for no DDL and readability. Moving
   later is cheap; starting with DDL is not.
2. **Do moderator corrections publish immediately, or queue like a player's?** Publishing matches
   the parity principle. Queueing is safer. I lean towards publishing for admins only, queueing for
   moderators, but this is your call about your community.
3. **Per-user memory: yes?** It is most of the felt benefit for almost none of the risk, and it is a
   different promise from global learning. If yes, it needs its own name in the interface.
4. **What is the cost ceiling?** The daily question cap exists, but retrieval and a larger tool set
   multiply the tokens per question. A monthly budget would let the defaults be set from something
   real rather than from caution.
5. **Is the wiki API open?** Needed before step 4 can be planned rather than guessed at.
6. **How much should the assistant volunteer that it does not know?** A bot that says "I have no
   fact about that, I have told an admin" is more useful than one that reasons plausibly from
   nothing, and it also produces the proposal queue for free. But it is a different personality and
   worth choosing deliberately.
