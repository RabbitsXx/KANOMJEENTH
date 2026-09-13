# Changelog

## 0.1.0-staging — 2026-09-13 compile/runtime certification

- Compiled a clean coherent 11-DLL Release set against Unturned `3.26.3.11`, RocketModFix `4.9.3.18`, and Unity redists `2022.3.62.3`.
- Resolved `UnityEngine.Logger`/`Rocket.Core.Logging.Logger` ambiguity and imported the Rocket permission extension used by `GameplayGuard.Has`.
- Qualified `System.Random` in Airdrops and `System.IO.Directory` in AdminAudit.
- Updated offline moderation to the actual `SteamBlacklist.ban/unban` API and mapped command duration `0` to the installed server's `SteamBlacklist.PERMANENT` value.
- Added explicit Core-binding log entries for Stats, Airdrops, and AdminAudit, then verified all six Core consumers bind during staging boot.
- Deployed the coherent DLL set to staging Server ID `Mywork`, removed the old experimental TPA plugin from the server tree, and completed two full startup cycles without Kanomjeen load exceptions.
- Unity prefab export, masterbundle/hash, Workshop publication, California 2 live zones/spawns, and multiplayer gameplay QA remain release blockers.

## 0.1.0-staging

Initial Kanomjeen integrated staging suite.

### Added
- Shared `Kanomjeen.Core` runtime services.
- Combat and raid tagging.
- Config/static + dynamic restricted zones.
- Persistent feature cooldowns.
- Cross-feature teleport lock.
- Exact/ambiguous-safe online player resolver.
- Command/UI rate limiting.
- Workshop UI server bridge and contract v1.0.
- TPA with request caps, warmup, movement/damage/death cancellation and persistent successful-teleport cooldown.
- Persistent homes with configurable limits, warmup and PvP/raid/zone restrictions.
- Permission-controlled utility kits with persistent cooldown.
- Respawn protection.
- BuildGuard placement limits and zone/item blocking.
- Scheduled/forced airdrops with dynamic objective restrictions.
- Persistent player statistics.
- Safe-save restart manager and announcements.
- Moderation commands, persistent mutes/warnings and buffered JSONL audit.
- Vehicle inspection and event logging without a virtual garage.
- Unity Editor Workshop UI prefab builder.
- Build/install/source-verification scripts for Windows and Linux.
- Release manifest, permissions guide, installation guide and QA matrix.

### Security / exploit hardening
- TPA/Home cannot run simultaneous teleport warmups for the same player.
- Teleport requests are revalidated immediately before teleport.
- GUI callbacks repeat server-side authority checks.
- Core disconnect cleanup releases teleport/rate/runtime state.
- Kit cooldown is committed before item delivery to prevent repeated partial-delivery duplication.

### Release note
This version is staging source. It requires local compile certification, Unity masterbundle export, dedicated-server runtime testing and California 2 coordinate configuration before production.
