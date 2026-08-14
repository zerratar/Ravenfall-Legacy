# Ravenfall Legacy Documentation

This documentation provides a high-level overview of the Ravenfall Legacy codebase to help developers understand the game's architecture and systems.

## What is Ravenfall?

Ravenfall is a **Twitch-integrated Idle RPG** where viewers can join a streamer's game session via chat commands. Players train various skills, fight enemies, participate in raids and dungeons, and explore multiple islands.

**Website:** https://www.ravenfall.stream/

## Documentation Index

### Core Systems
- [Architecture Overview](./architecture-overview.md) - High-level system design
- [GameManager](./core/game-manager.md) - Central game orchestrator
- [Player System](./player/player-controller.md) - Player management and control

### Game Features
- [Skills & Progression](./systems/skills.md) - Skill system and experience
- [Combat System](./systems/combat.md) - Fighting, enemies, and attacks
- [Inventory & Equipment](./systems/inventory.md) - Items and gear
- [Events (Raids & Dungeons)](./systems/events.md) - Special game events

### World
- [Islands & World](./world/islands.md) - World structure and navigation
- [Chunks & Tasks](./world/chunks.md) - Training areas and activities
- [Ferry System](./world/ferry.md) - Inter-island travel

### Integration
- [Twitch Integration](./integration/twitch.md) - Bot commands and Twitch features
- [RavenNest API](./integration/ravennest.md) - Server communication

### Design Notes
- [Twitch Message Budget](./twitch-message-budget.md) - chat length vs rate limits, and prioritising active players
- [Player Component Architecture](./player-component-architecture.md) - which MonoBehaviours should stay components, and where a system fits better
- [Refactor Risk Log](./refactor-risk-log.md) - behaviour changes to verify before shipping an update

### Setup
- [Unity 6.7 Local Fixes](./unity-6.7-local-fixes.md) - Required patches that live outside version control (third-party assets, URP settings, generated collider data)

## Quick Reference

### Key Scripts Location
```
Assets/Scripts/
├── Game/
│   ├── Managers/          # Core managers (GameManager, RaidManager, etc.)
│   ├── Player/            # Player-related scripts
│   ├── Combat/            # Combat system
│   ├── Handlers/          # Event and action handlers
│   ├── Inventory/         # Inventory system
│   ├── Skills/            # Skill implementations
│   └── Controllers/       # Game controllers (Dungeon, Raid, Island)
├── World/                 # World chunks and management
├── Tasks/                 # Skill task implementations
└── UI/                    # User interface
```

### Unity Version
As of writing: Unity 6000.0.14f1 (Unity 6)

## Related Projects
- [RavenNest (Server)](https://github.com/zerratar/ravennest) - Website and game server
- [RavenBot](https://github.com/zerratar/RavenBot) - Twitch chat bot
- [RavenWeave](https://github.com/zerratar/RavenWeave) - Game updater/patcher
- [Ravenfall-Overlay](https://github.com/zerratar/Ravenfall-Overlay) - Twitch extension
