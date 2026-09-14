# Changelog

## 0.1.0-staging — 2026-09-14 HUD overlay anchoring fix

- Fixed the HUD and minimap builder resetting both overlays to the canvas center after their intended positions were assigned.
- Re-exported the Unity 2022.3.62f3 Workshop bundle set; the client must receive the updated Workshop item before visual QA.

## 0.1.0-staging — 2026-09-14 immediate HUD snapshot

- Pushes the first live HUD snapshot immediately when a player connects, before the repeating update loop begins.

## 0.1.0-staging — 2026-09-14 HUD diagnostics

- Added throttled server logs for HUD initialization, active sessions, snapshot values and update counts during local integration testing.

## 0.1.0-staging — 2026-09-14 native HUD bridge prototype

- Added a server-driven HUD snapshot for Health, Food, Water, Virus, Stamina and Oxygen.
- Added continuous HUD session handling so the Effect is sent when a player connects and remains visible while menus open/close.
- Added server-driven minimap bearing, compass direction and player coordinates.
- Rebuilt and re-hashed the Workshop bundle; live client/server QA remains required.

## 0.1.0-staging — 2026-09-14 removed combined GUI package

- Removed the old combined GUI prefab, editor builders, fonts, exported bundles, and Workshop upload folder.
- Kept gameplay commands and server-side systems intact; GUI remains disabled until a new per-plugin design is briefed.
- Verification no longer requires the retired combined GUI assets.

## 0.1.0-staging — 2026-09-14 GUI disabled for rebuild

- Disabled all runtime Workshop GUI entry points while the interface is rebuilt incrementally.
- Preserved the existing Effect ID, GUID, contract metadata, gameplay systems, and command paths.
- GUI button event subscriptions are skipped when the UI service is not configured.

## 0.1.0-staging — 2026-09-14 UI overhaul measured from the reference Effects

- Rebuilt the whole Workshop UI on tokens measured from the two commercial Effects this suite benchmarks against (`Supernovea Itemshop V2` 3477290482, `Supernovea RankQuest V2` 3478575975): `#212121` cards on a translucent `#1A1A1A` table, an opaque black header band, green `#4ADC43` affirmative and red `#9F1B1B` destructive actions, the reference `normal/highlighted/pressed/disabled` ColorBlock, Unity's built-in nine-slice `UISprite`, 1920×1080 canvas matched by width, and a 1180×700 shell whose content band can no longer overlap the header.
- Adopted **Kanit** (Regular/SemiBold/Bold, SIL OFL 1.1) as the UI family. Kanit is the body font of both reference Effects and covers Thai and Latin in one family; it is downloaded from the upstream Google Fonts repository and the three blobs are verified by git hash. `Oswald`/`Anton` were rejected as display roles because google/fonts ships them as variable fonts only.
- The font binaries are now **tracked in git** — the old `.gitignore` rules for a single `KanomjeenThai.ttf` meant a fresh clone could export a bundle with no Thai coverage and no visible error.
- Added `BundleInspector.cs`, an editor tool that dumps any Unturned Effect master bundle as readable text (asset list, fonts reachable from Text components, textures/sprites, every GameObject rect, Image colour, Text setting and Button ColorBlock). It is how the reference tokens above were measured, and how the shipped bundle is now verified.
- Re-exported the master bundle set with Unity 2022.3.62f3 and refreshed `Kanomjeen_UI_Workshop`: windows 155,304 B, linux 155,474 B, mac 155,596 B, `.hash` 61 B (re-verified against all three bundles), package total 466,611 B.
- Documented the measured reference contents in `AGENTS.md` §13.7, rewrote `DESIGN_SYSTEM.md` as v2.0 with sources for every token, and recorded that the recurring 1-byte `Object.meta` is produced by Unturned's own upload step (both reference items contain it).

## 0.1.0-staging — 2026-09-14 Workshop UI reliability pass

- Fixed the Waypoints screen rendering stacked on top of the main menu: `UiService.Screens` did not list `waypoints`, so the container was never hidden. The list is now one static field, checked against the Unity prefab by `verify-source.sh` / `verify-source.ps1`, and an unknown screen name logs an error and falls back to `main`.
- Split UI entry from navigation: `UiService.Open` now always clear+sends a fresh Effect (reference-plugin behaviour that survives stale client copies), and the new `UiService.ShowScreen` drives in-menu navigation with visibility/text only. Feature plugins follow that convention.
- Layout fixes from the in-game screenshot: the Waypoints help line no longer collides with the shell status line, rows moved to `146 - i*54`, `STOP TRACKING` to `y -300`, and the main-menu cards were resized to 370×112 and re-spaced so the STAFF card stays inside the shell.
- Re-exported the master bundle set with Unity 2022.3.62f3 (Noto Sans Thai still embedded) and refreshed `Kanomjeen_UI_Workshop`; `.hash` re-verified against all three bundles.
- Restructured every feature's client presentation into its own `*Ui` class (Homes, Kits, Stats, TPA, Airdrops, AdminAudit, Core Waypoints) with a small internal plugin surface, so gameplay classes no longer build screens, rows or button handlers inline. Shared helpers are `UiGuard` (error containment + prefix/index parsing) and `UiRow`.
- Documented the Unturned GUI reliability rules, the per-feature UI layer map and the study of a reference plugin in `AGENTS.md` §13.

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

### UI layout hotfix
- Fixed all left/right-aligned uGUI labels using a center pivot, which displaced titles/subtitles outside their cards in the exported Effect.
- Text RectTransform pivots now follow `TextAnchor` alignment and the Unity builder validates every Text pivot before saving the prefab.
- `UiService.Open` now sends one Effect instance per active UI session and switches screens with visibility updates instead of stacking another Effect instance.
- First open clears stale copies left by plugin reloads, and Core unload clears the Effect from connected clients before disposing the UI service.
- Requires rebuilding/exporting the Workshop UI and rebuilding `Kanomjeen.Core.dll` for the complete fix.

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
