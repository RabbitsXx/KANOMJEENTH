# Kanomjeen Architecture

## Team ownership
- **Lead Architect / Project Lead** — shared contracts, version gates, dependency policy, gameplay balance, final review.
- **Plugin Programmer A** — TPA, Homes, Kits, Stats.
- **Plugin Programmer B** — BuildGuard, Airdrops, ServerManager, AdminAudit, VehicleGuard.
- **Gameplay / Systems Programmer** — Core, combat/raid tagging, zones, cooldown persistence, anti-exploit shared services.
- **UI/UX Designer** — information architecture, layout, typography, states and Kanomjeen visual system.
- **UI Programmer** — Workshop effect prefab builder, stable UI names, EffectManager bridge and GUI callbacks.
- **QA / Tester** — API signature review, static checks, multiplayer scenarios, persistence/restart/exploit/performance release gates.

## Dependency direction

```text
Kanomjeen.Core
   ↑
   ├── Kanomjeen.TPA
   ├── Kanomjeen.Homes
   ├── Kanomjeen.Kits
   ├── Kanomjeen.BuildGuard
   ├── Kanomjeen.Airdrops
   ├── Kanomjeen.Stats
   └── Kanomjeen.AdminAudit

Standalone/lightweight:
   Kanomjeen.Respawn
   Kanomjeen.ServerManager
   Kanomjeen.VehicleGuard

Client Workshop UI ← EffectManager/UI contract ← Kanomjeen.Core
```

Feature plugins do not own duplicate combat/raid/cooldown/zone/UI session implementations when the shared Core service can provide them.

## Core runtime contracts

### Player state
Runtime state contains:
- combat-tag expiry
- raid-tag expiry
- last damage time
- feature transient state as required

Disconnect handlers remove transient state. Persistent gameplay cooldowns are separate and survive reconnect/restart.

### Combat
`DamageTool.playerDamaged` tags the victim and, when a different valid player is the attacker, tags the attacker. Feature plugins consume this state instead of each inventing combat tracking.

### Raid
Barricade/structure damage requests tag the instigating player for the configured raid duration. TPA/Home therefore cannot be used to immediately escape after damaging raid targets.

### Teleport gate
A TPA/Home teleport can fail for:
1. invalid/offline/dead player
2. permission/rate limit/cooldown
3. vehicle restriction
4. combat tag
5. raid tag
6. restricted origin
7. restricted destination
8. movement during warmup
9. damage during warmup
10. death/disconnect
11. failure during final revalidation/teleport

The gate is checked before warmup and immediately before teleport.

### Zones
Static zones are configuration data. Runtime systems may add dynamic zones.

Flags:
- `BlockTPA`
- `BlockHome`
- `BlockBuild`
- `BlockKit`
- `HighTier`
- `Deadzone`
- `Safezone`
- `AirdropObjective`

California 2 POIs/deadzones should be entered through configuration after inspecting the live map. They are intentionally not embedded as coordinates in C#.

### Dynamic objectives
`Kanomjeen.Airdrops` registers a temporary `AirdropObjective` zone with Core. This allows other plugins to consume the restriction without a direct plugin-to-plugin dependency.

## Persistence
- Core feature cooldowns: XML, batched flush.
- Homes: XML, batched flush.
- Stats: XML, batched flush plus online-session accumulation.
- Moderation: XML, batched flush.
- Audit: monthly JSONL, buffered flush.

Critical gameplay writes use temp-file replacement where implemented. No plugin intentionally writes a full database on every normal player command.

## Player resolution
`PlayerResolver` follows:
1. Steam64 exact
2. display/Steam/character name exact, case-insensitive
3. partial match only if exactly one player matches
4. otherwise Ambiguous/NotFound

This prevents silently teleporting/moderating the wrong player from a first-match partial search.

## GUI contract
The client gets one Workshop Effect (default ID `51000`) containing screen containers:
- `main`
- `tpa`
- `homes`
- `kits`
- `stats`
- `airdrop`
- `admin`
- `toast`

`UiService` keeps the active screen per player. UI clicks are only requests; the server repeats permission/state/cooldown/target validation. Row indices are resolved against the current server-side data set before acting.

## Implemented gameplay baselines

### TPA
- request lifetime: 30s
- request command rate: 3s
- successful teleport cooldown: 600s
- warmup: 10s
- movement tolerance: 0.5m
- outgoing requests: 1
- incoming requests: 3

### Homes
- default homes: 2
- cooldown: 900s
- warmup: 10s
- persistent named positions
- permission-based higher home limits

### Kits
- default Starter: canned beans + bottled water + bandage
- 6h default cooldown
- no weapon/armor/explosive starter items

### Respawn
- default protection: 10s
- protection ends on movement beyond tolerance, attacking, expiry, death/offline state
- no home-spawn special immunity is implemented

### BuildGuard
Current launch implementation checks placement-time:
- restricted zones
- blocked item IDs
- per-owner barricade/structure counts
- sentry/bed/claim sub-limits

It does **not** claim road-spline detection, height limiting or group-wide aggregation in v1. Those are future extension points because they require additional map/API validation and should not be silently approximated.

### Airdrops
- configured map positions, no hardcoded California coordinates
- population gate
- configurable random schedule
- dynamic objective restriction radius
- GUI status

### Stats
- kills/deaths/KDR
- fatal player-kill headshots
- zombie kills
- playtime
- longest life
- airdrop capture field reserved for a verified capture signal; it is not fabricated from proximity.

### ServerManager
- autosave
- restart schedule/countdown
- safe save then shutdown
- rotating announcements

### Admin/Audit
- warning
- persistent mute
- kick
- temp/permanent ban
- unban
- inspect
- god/vanish
- buffered local JSONL audit

Discord webhook delivery is not enabled in v1. Local structured audit is the launch requirement; webhook transport can be added later without coupling moderation to an external network service.

### VehicleGuard
- owner/lock/state inspection
- entry/damage operational logging
- no public vehicle storage or teleport.

## Performance rules
- no per-frame global player scan
- no expensive LINQ in hot paths
- prefer event-driven state changes
- interval work only where necessary (warmups, persistence, restart timer, small protected-player set)
- all subscribed static events must be unsubscribed on unload
- coroutines/invokes cancelled on unload
- persistent disk writes are batched

## Security rules
- command/UI callback rate limiting
- exact or unambiguous target resolution
- server-side permission rechecks
- combat/raid/zone final revalidation
- disconnect cleanup
- persistent successful cooldowns
- kit cooldown committed before delivery to prevent repeated partial-delivery claims
- no trust in client GUI state/index

## Release rule
API-sensitive code must be compiled against the exact RocketModFix/Unturned redists and tested on a staging server. `QA_REPORT.md` distinguishes implemented/static-reviewed work from gates that require an actual server/Unity runtime.
