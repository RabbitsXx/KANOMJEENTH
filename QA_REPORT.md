# Kanomjeen QA / Release Gate

Status legend:
- `[x]` source-level design implemented in this repository
- `[ ]` must be executed on a computer/server with the required runtime
- `[!]` high-risk gate: do not release if it fails

## Source review completed
- [x] Shared `kanomjeen.*` permission namespace.
- [x] No per-frame player scans in gameplay plugins.
- [x] TPA/Home share one Core teleport lock, preventing simultaneous warmups.
- [x] TPA/Home cancel warmup on configured movement/damage/death paths.
- [x] Core clears runtime teleport/rate/player state on disconnect.
- [x] Successful TPA/Home cooldowns persist through Core XML storage.
- [x] Home/Stats/Moderation data use persistent XML stores with temp-file replacement.
- [x] UI clicks are treated as requests and re-run server-side permission/state checks.
- [x] Ambiguous online player names are not silently resolved to the first match.
- [x] Dynamic airdrop zone blocks TPA/Home/Kit and optionally Build.
- [x] ServerManager performs save before shutdown.
- [x] Admin audit is buffered rather than sync-writing every command/chat line.
- [x] Feature modules that subscribe to Core callbacks retry binding if Core is not ready at their initial Load.
- [x] Workshop UI uses stable `KJ_` element names and contract `1.0`.
- [x] Workshop effect identity defaults to ID `51000` and GUID `2f8df4942fe04a27a9636942abb3cd21`.
- [x] Unity builder supports a user-supplied Thai-capable font slot and warns on fallback.

## Compile certification
- [x] [!] Run `./verify-source.ps1` (Windows) or `./verify-source.sh` (Linux). Passed on Linux 2026-09-13.
- [x] [!] Run `./build.ps1` or `./build.sh` with zero compiler errors. Clean Release build passed against 3.26.3.11 redists.
- [x] [!] Confirm exactly 11 DLLs exist in `dist/plugins`.
- [x] [!] Confirm there are no missing-method/type-load errors caused by the exact installed Unturned/RocketModFix build. Two staging boots passed plugin load on 3.26.3.11 / 4.9.3.18.
- [x] Confirm Release builds contain no unexpected dependency DLLs that should be provided by the server.

### API-sensitive compile/runtime checks
These must be verified against the actual server assemblies rather than guessed if an update changed them:
- [x] `DamageTool.playerDamaged` delegate signature.
- [x] `BarricadeManager.onDamageBarricadeRequested` delegate signature.
- [x] `StructureManager.onDamageStructureRequested` delegate signature.
- [x] `BarricadeManager.onDeployBarricadeRequested` delegate signature.
- [x] `StructureManager.onDeployStructureRequested` delegate signature.
- [x] `EffectManager.onEffectButtonClicked` and UI send/clear methods.
- [x] `UnturnedPlayerEvents.OnPlayerRevive`, `OnPlayerDeath`, `OnPlayerUpdateStat` signatures.
- [x] `VehicleManager.onEnterVehicleRequested`, `onDamageVehicleRequested` signatures.
- [x] `LevelManager.airdrop` signature.
- [x] `SteamBlacklist.ban/unban`, `SteamBlacklist.PERMANENT`, and `UnturnedPlayer.Ban/Kick` signatures. Live ban actions remain pending multiplayer QA.

If any one fails: inspect the exact installed assemblies/ApiDump, fix only the adapter/signature, rebuild, then rerun this entire section.

## Server boot/load test
- [x] [!] Back up server first. Full `Servers/Mywork` archive created before deployment.
- [x] Remove/disable old `Tpa.dll` or other plugins exposing overlapping commands. Old plugin moved outside server tree and stale command entries removed.
- [x] Copy one coherent Kanomjeen build to Rocket Plugins.
- [x] Restart server fully.
- [x] [!] Confirm `Kanomjeen.Core` loads without exception.
- [x] Confirm TPA/Homes/Kits/Stats/Airdrops/AdminAudit report they are bound to Core.
- [x] Confirm all 11 modules load without Rocket error stack traces.
- [x] Confirm configuration/data directories are writable. Rocket generated all 11 config/translation directories and AdminAudit wrote valid JSONL.
- [ ] Restart a second time and confirm persistent XML files reload successfully.

## TPA multiplayer tests
Use two non-admin clients A/B plus optional staff C.
- [ ] `/tpa B` -> B accepts -> A waits warmup -> teleport succeeds.
- [ ] Successful teleport starts persistent cooldown.
- [ ] Restart server -> cooldown still applies.
- [ ] A moves beyond tolerance -> warmup cancels.
- [ ] A takes damage -> warmup cancels.
- [ ] A dies -> warmup cancels.
- [ ] A disconnects -> pending/warmup state is cleaned.
- [ ] B disconnects -> request cannot complete.
- [ ] A is combat-tagged -> teleport blocked.
- [ ] A is raid-tagged -> teleport blocked.
- [ ] Origin in restricted/deadzone/high-tier zone -> blocked.
- [ ] Destination in restricted/deadzone/high-tier zone -> blocked.
- [ ] A starts Home warmup then tries TPA -> second teleport rejected as busy.
- [ ] `/tpahere` direction is correct.
- [ ] Duplicate and pending request caps work.
- [ ] Ambiguous partial player name is rejected.

## Homes tests
- [ ] Create up to default max 2.
- [ ] Duplicate name rejected case-insensitively.
- [ ] Third home rejected without higher-limit permission.
- [ ] `kanomjeen.home.limit.3` allows exactly the intended extra slot.
- [ ] Delete persists after restart.
- [ ] Position/rotation restore correctly after restart.
- [ ] Movement/damage/death cancels warmup.
- [ ] Combat/raid/origin/destination zone blocks work.
- [ ] Airdrop objective zone blocks home travel.
- [ ] Concurrent TPA/Home warmup cannot occur.

## Kits tests
- [ ] Default starter gives only intended items.
- [ ] Permission required.
- [ ] Cooldown persists through restart.
- [ ] Combat/raid/restricted-zone checks work.
- [ ] Full inventory cannot be abused to duplicate repeated claims.
- [ ] Review every configured item ID before production; no high-tier/P2W loadouts.

## Respawn protection tests
- [ ] Fresh revive gets protection for configured time.
- [ ] Incoming damage is prevented while protected.
- [ ] Moving beyond tolerance ends protection when enabled.
- [ ] Attacking another player ends attacker's protection.
- [ ] Disconnect cleans state.
- [ ] Verify normal PvP damage resumes immediately after cancellation/expiry.

## BuildGuard tests
- [ ] [!] Compile against actual current Unturned server API.
- [ ] Barricade/structure limits deny placement at threshold.
- [ ] Bed/sentry/claim subtype limits work.
- [ ] Blocked item IDs deny placement.
- [ ] Core static restricted zones deny placement.
- [ ] Dynamic airdrop objective denies placement when configured.
- [ ] Staff bypass permission works.
- [ ] Count cache updates after placement/removal within expected delay.
- [ ] Large-base performance test: no noticeable main-thread hitch from counting.

## Airdrop tests
- [ ] Configure real California 2 spawns using `/setairdropspawn`.
- [ ] Use real valid airdrop asset IDs; do not invent IDs.
- [ ] Minimum player gate works.
- [ ] Interval scheduling works.
- [ ] Forced airdrop command works for staff.
- [ ] Dynamic objective zone appears during event and disappears after lifetime.
- [ ] TPA/Home/Kit are blocked inside objective radius.
- [ ] Build block matches config.
- [ ] `/whenairdrop` and UI show sensible active/standby data.

## Stats tests
- [ ] Join/leave playtime accumulates without double-counting.
- [ ] Kill/death counts correct for PvP.
- [ ] Suicide does not grant kill.
- [ ] Headshot increments only when fatal limb is skull.
- [ ] Normal/mega zombie kills increment.
- [ ] Longest life persists.
- [ ] Restart/reconnect does not lose data.
- [ ] Stats UI fields match chat/data.

## ServerManager tests
- [ ] Autosave runs without error.
- [ ] Manual restart countdown announces expected thresholds.
- [ ] Cancel restart works.
- [ ] Automatic restart interval schedules correctly.
- [ ] Save occurs before shutdown.
- [ ] External process/service actually restarts the server after process exit; plugin itself only shuts down safely.

## Admin/Audit tests
- [ ] Warn persists.
- [ ] Mute hides chat and survives restart.
- [ ] Unmute online and Steam64 path work.
- [ ] Kick works and audit entry exists.
- [ ] Temporary ban expires as expected.
- [ ] Permanent ban semantics are verified for duration `0` on the actual API.
- [ ] Offline Steam64 ban/unban works.
- [ ] Inspect reports correct state.
- [ ] God/vanish permission checks and audit entries work.
- [ ] Non-staff cannot trigger admin GUI callbacks even if manually spoofed.
- [ ] Audit JSONL remains valid when messages contain quotes/newlines/backslashes.

## VehicleGuard tests
- [ ] `/vehicleinfo` works while seated and while looking at a vehicle.
- [ ] Staff inspect permission enforced.
- [ ] Vehicle entry/damage logging does not spam excessively under combat load.
- [ ] No garage/vehicle teleport/storage feature exists at launch.

## Workshop UI build
- [ ] [!] Import Unturned's current `Extras/Sources/Project.unitypackage` into a compatible Unity project.
- [ ] Add a legal Thai-capable `KanomjeenThai.ttf` to the documented font slot, or explicitly accept English-only UI after review.
- [ ] Run `Kanomjeen -> Build Workshop UI Prefab` with zero Unity console errors.
- [ ] Generated `Effect.prefab` contains every contract element.
- [ ] Export `kanomjeen_ui.masterbundle` with multiplatform enabled.
- [ ] [!] Include generated `.masterbundle.hash`.
- [ ] Include Linux/macOS bundles when generated/required.
- [ ] Test local client loading before Workshop upload.
- [ ] `/menu` opens and closes repeatedly without stuck modal cursor.
- [ ] TPA accept/deny works with two clients.
- [ ] Home/Kits row buttons map to the current server-side view model.
- [ ] Staff card hidden for non-staff.
- [ ] 16:9, 16:10, 21:9 and UI scale checks pass.
- [ ] Thai glyphs render without missing boxes/clipping.
- [ ] Upload Workshop item, set Owner Workshop File ID custom data where supported, rebuild/reupload, then retest.

## California 2 production configuration
- [ ] Add actual deadzone/high-tier/safezone/restricted zone coordinates to Core config based on the live map build.
- [ ] Do not hardcode map coordinates into C#.
- [ ] Validate underwater/deadzone areas from the installed California 2 version.
- [ ] Set real airdrop spawn coordinates by standing at intended locations in-game.
- [ ] Playtest travel cooldowns against real map travel times.

## Final production gate
Production release is allowed only when:
- [ ] [!] zero compile errors
- [ ] [!] zero plugin-load exceptions
- [ ] [!] no known teleport/kit/moderation duplication exploit
- [ ] [!] Workshop bundle + hash validated
- [ ] persistent storage survives restart
- [ ] permissions tested with default/VIP/mod/admin accounts
- [ ] California 2 zones and airdrop spawns configured from the actual live map
- [ ] rollback backup exists

Until every `[!]` item passes, label the build **staging**, not production.
