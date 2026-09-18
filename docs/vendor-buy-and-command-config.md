# Vendor buy and configurable commands

Assessment of two future ideas. Nothing planned in yet, both sit behind the marketplace and
vendor work in [trade-plan-commit-design.md](trade-plan-commit-design.md).

## Vendor buy from player sold stock

The idea: the vendor holds what players sold it, others can buy from that stock at a price
above what the seller received, so the vendor consumes coins rather than generating them.

### The economics hold up

Sell 100 coal at 10 gives the seller 1000. Another player buying those 100 back at 12 costs
1200. Net effect on the money supply is minus 200. It is a sink.

Two things to be clear eyed about:

**It does not fix the faucet, it adds a conditional sink.** Vendor selling still creates coins
out of nothing. The sink only operates on the fraction of stock that somebody buys back. If
coal sells to the vendor constantly and nobody ever buys it, the faucet is unchanged. That is
still strictly better than today, where there is no sink at all, but it should not be mistaken
for solving coin inflation.

**It puts a ceiling on marketplace prices.** Mostly a good thing: it stops gouging on common
goods and gives newer players a reliable supply. But "slightly higher" is probably too slight.
If the vendor is nearly as cheap as other players, the player marketplace dies for everything
common and only exists for enchanted and rare items. The spread wants to be wide enough that
buying from another player is usually the better deal, and the vendor is the convenient
fallback.

### The invariant that matters

If both the buy price and the sell price move with stock, there may exist a stock level where a
round trip is profitable. That is the exploit to design against, and it will be found quickly.
A streamer's chat will brute force it within hours of release.

State it as an invariant and test it directly:

> For any quantity q and any stock level s, the cost of buying q must exceed the revenue from
> selling those same q back, evaluated at the stock level that buying leaves behind.

Test it across the whole stock range rather than at a couple of sample points. This is exactly
the kind of property that is cheap to check exhaustively and expensive to discover in
production.

### How to de-risk the economy question

Ship it on the website dashboard first, with no game or bot update at all. The marketplace
already lives there. That gives real data on what people actually buy back, at what rate, and
what it does to marketplace prices, while it is still trivially reversible. The chat command,
and the game and bot updates it needs, can follow once the numbers are known.

That directly answers the reason this was never built. The worry was swaying the economy
without being able to see it happening. On the website you can watch it and switch it off.

### Fit with the work already planned

Vendor buy is allocation across a stock at a computed price. That is the same allocation
function the marketplace purchase needs. Built after the plan and commit work it is a small
addition rather than a new subsystem, which is a good sign the ordering is right.

## Configurable commands

The instinct is right and the value is real, but the framing needs splitting in two, because
"flexible enough to do practically anything" is where systems like this usually die.

### The trap

A fully general intent interpreter is a scripting language. Owning one means owning sandboxing,
versioning, a debugging story, malicious or merely broken streamer configs, and the support
load when someone's custom command misbehaves. And it still cannot do anything the client has no
code for. No amount of intent flexibility adds a boss type the game cannot render.

### The split that makes it work

**Capability layer.** What the game can actually do: train a skill, join a raid, equip an item,
travel to an island. New capabilities need a game update. That is unavoidable and fine.

**Command layer.** How chat text maps onto a capability: the command name, aliases, which
language, argument shorthands, whether it is enabled, cooldowns, who may use it, what the
response says. This is data.

Almost everything described sits in the command layer.

### Most of it needs no game update at all

`CommandProvider.GetCommand` resolves a chat command by string lookup in a dictionary:

```csharp
if (processors.TryGetValue(part.ToLower(), out processor))
```

The bot parses chat and decides which named message to send the game. So the mapping from chat
text to game action is already loose and already lives in the bot. Per streamer aliases,
localized command names, enabling and disabling commands, custom response text and per command
cooldowns are a configuration layer over machinery that exists today. Bot and server only.

This also connects to the localization work. A Spanish streamer wanting `!entrenar` rather than
`!train` is the same feature, and it is bot side.

### The piece that makes it safe across versions

Streamers run different game versions. If commands become configurable, the bot needs to know
what the connected client can actually do, or it will happily accept a command the game cannot
service.

The session settings already carry `client_version`. The natural extension is a capability list
reported at session start, so the bot enables only the commands this client supports. That is
worth designing in from the beginning rather than retrofitting, and it solves a problem that
already exists today in a quieter form.

### Where to draw the line

Let streamers configure naming and availability. Do not let them configure behaviour. The
moment a streamer can define what a command does rather than what it is called, the support
burden stops being bounded.

One thing to decide: reserved core commands. Per channel aliases are fine, but a streamer
rebinding `!join` to something else will confuse viewers who play in several channels. A small
reserved set that cannot be rebound is probably worth it.

## Bot management dashboard

Worth separating from the rest, because it is the highest value per unit of effort of anything
discussed here, and it does not depend on the command configuration work.

Streamers currently have no visibility into the bot and no control over it. A first version
showing connection state, which channel the bot believes it is in, and per command enable and
disable would be a real improvement on its own.

Note that the username change bug would have been self diagnosable with nothing more than
"the bot thinks it is in channel X" on a page the streamer can open. That is a decent argument
for building the visibility half before the configuration half.

It also makes the choice between a local bot and the centralized one manageable, which is
currently something a streamer has to understand rather than something they can see.

## Summary

- Vendor buy: plausible, economics are sound, build the round trip invariant in from the start,
  and ship it on the website first so the economy can be observed before it reaches chat.
- Configurable commands: valuable and largely achievable without a game update, provided the
  scope is naming and availability rather than behaviour.
- Capability manifest at session start: small, and it is the thing that keeps the above safe
  across mixed client versions.
- Bot dashboard: do the visibility half early, independent of everything else.
