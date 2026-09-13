# Install Kanomjeen Server Suite

## 1. Verify and build
Run the source verifier and build steps in `BUILD.md`. Do not deploy source files directly to Rocket and do not install a partial/mixed DLL set.

## 2. Plugin load order
Recommended deployment order:
1. `Kanomjeen.Core.dll`
2. `Kanomjeen.TPA.dll`
3. `Kanomjeen.Homes.dll`
4. `Kanomjeen.Kits.dll`
5. `Kanomjeen.Respawn.dll`
6. `Kanomjeen.BuildGuard.dll`
7. `Kanomjeen.Airdrops.dll`
8. `Kanomjeen.Stats.dll`
9. `Kanomjeen.ServerManager.dll`
10. `Kanomjeen.AdminAudit.dll`
11. `Kanomjeen.VehicleGuard.dll`

Core must be available before feature plugins that consume it.

For the current RocketModFix/LDM layout targeted by this project, the normal plugin path is:

```text
Servers/<ServerID>/Rocket/Plugins/
```

The included `install.ps1` / `install.sh` deploy the 11 Kanomjeen DLLs directly there. If your installed RocketModFix build has been customized to use another plugin layout, verify that layout from the running server/config before changing the installer; do not guess or duplicate DLLs across multiple plugin folders.

Do not keep both the old experimental `Tpa.dll` and `Kanomjeen.TPA.dll` enabled at the same time. They expose overlapping commands and would create conflicting behavior.

## 3. First boot
Start the server once so Rocket creates default configuration files.

Stop the server and review every generated configuration before opening the server publicly.

Important first-boot checks:
- Core `UiEffectId` matches Workshop Effect ID `51000`.
- Core `UiKey` remains unique for this UI session.
- Core `WaypointMode` remains `FallbackNativeMarker` unless a separately distributed, supported client module is installed.
- Core Zones are populated for any California 2 deadzones/high-tier/restricted areas you want to protect.
- Home/TPA cooldowns fit your wipe/travel balance.
- Kit definitions contain only intended item IDs.
- Build limits match your expected player count/server hardware.
- Airdrop automatic mode should remain harmless until valid map spawns are configured.

## 4. Configure California 2 zones
Do not copy arbitrary coordinates from another server.

For each protected area add a Core ZoneRule with:
- readable name
- X/Y/Z center
- radius
- relevant flags, such as Deadzone/HighTier/BlockTPA/BlockHome/BlockKit/BlockBuild

Dynamic airdrop objective zones are created automatically and should not be entered into static configuration.

## 5. Configure airdrop spawns
Join as an admin with `kanomjeen.admin.airdrop`.

Stand at each desired California 2 airdrop target and run:

```text
/setairdropspawn <name> <airdropId>
```

Example naming style:

```text
/setairdropspawn SF-North 1234
```

Use the actual airdrop asset ID configured for your content. The plugin intentionally does not invent a California 2 loot/airdrop asset ID.

Verify with:

```text
/kjairdrop <spawn name>
/whenairdrop
```

## 6. Configure permissions
Use `PERMISSIONS.md` as the policy matrix. At minimum normal players generally need:
- `kanomjeen.tpa.use`
- `kanomjeen.waypoint.use`
- `kanomjeen.home.use`
- `kanomjeen.home.set`
- `kanomjeen.kit.starter`

Add `/tpahere` permission if wanted.

## 7. Install Workshop UI
Follow `workshop-ui/WORKSHOP_RELEASE.md` to build and publish the UI.

Add that Workshop item to the dedicated server's Workshop content list so connecting clients download it through the normal Unturned Workshop workflow.

Test `/menu` only after the client content is installed.

If UI content is temporarily unavailable, chat commands remain the operational fallback for gameplay systems.

Waypoint commands: `/wp add <name>`, `/wp list`, `/wp track <number>`, `/wp stop`, `/wp rename <number> <new name>`, `/wp delete <number>`, and `/home track <home name>`.

## 8. Staging test
Use at least two non-admin test accounts plus one staff account.

Required scenarios are in `QA_REPORT.md`, especially:
- TPA movement/damage/combat/raid cancellation
- Home persistence after restart
- kit cooldown/full inventory
- respawn attacker/victim protection
- build restriction after plugin reload
- airdrop objective travel blocks
- moderation/mute/ban
- GUI permission enforcement

## 9. Production rollout
Before production:
- back up server data/config
- remove replaced/duplicate old plugins
- deploy all compiled DLLs from one build
- deploy the matching Workshop UI version
- restart the server; do not assume Rocket reload can replace already-loaded assembly code
- watch Rocket/Unturned logs for event/API errors
- confirm XML/JSONL storage directories are writable

## Upgrade rule
Treat server DLLs and Workshop UI as versioned release artifacts. For a breaking UI contract change, stage both together. Never rename GUI button objects on the Workshop side while leaving old server callbacks live.
