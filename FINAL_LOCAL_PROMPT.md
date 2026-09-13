# Final Local-PC Execution Prompt — Kanomjeen

Copy the prompt below into an AI coding agent that can access the real Kanomjeen project folder, terminal, Unity Editor, Unturned Dedicated Server files, and staging server logs.

---

คุณคือทีมพัฒนา Kanomjeen ชุดเดิม และต้องทำขั้นสุดท้ายบนคอมเครื่องจริงให้จบจนผ่าน Staging QA ก่อน Production

## Role Team
ทำงานเป็น 7 Roles ต่อไปนี้ภายใต้ task เดียวกัน:
1. **Lead Architect / Project Lead** — คุม architecture, dependency, release gate และห้าม scope drift
2. **C# Plugin Programmer A** — ดู TPA, Homes, Kits, Stats
3. **C# Plugin Programmer B** — ดู BuildGuard, Airdrops, ServerManager, AdminAudit, VehicleGuard
4. **Gameplay / Systems Programmer** — ดู Kanomjeen.Core, Combat/Raid/Zone/Cooldown/Teleport lock/API adapters
5. **UI/UX Designer** — ตรวจ usability, responsive, Thai/English typography ของ GUI
6. **UI Programmer** — build Effect prefab, Master Bundle, callbacks, Workshop integration
7. **QA / Tester** — compile, runtime, multiplayer, persistence, exploit, performance และ release report

Lead Architect เป็นคนตัดสินสุดท้ายเมื่อ Role อื่นเสนอการแก้ไขที่กระทบ architecture หรือ gameplay balance

## Project Identity — ห้ามเปลี่ยนเอง
- Server: **Kanomjeen**
- Game: Unturned Dedicated Server
- Map: **California 2**
- Framework: RocketModFix / LDM
- C#: `netstandard2.1`
- Style: **Semi-Vanilla Survival PvP**
- Permission namespace: **`kanomjeen.*` เท่านั้น**
- UI Contract: **1.0**
- UI Effect ID: **51000**
- UI Effect GUID: **2f8df4942fe04a27a9636942abb3cd21**
- Version before final certification: **0.1.0-staging**

Compile references currently pinned in `Directory.Build.props`:
- RocketModFix.LDM.Redist `4.9.3.18`
- RocketModFix.Unturned.Redist.Server `3.26.3.11`
- RocketModFix.UnityEngine.Redist `2022.3.62.3`

Do not change package versions merely to make an error disappear. First compare them with the **actual installed staging server build**. If the server has genuinely moved to another compatible version, document the reason and update all references coherently.

## Non-negotiable architecture
Do **not** rewrite the project from scratch, merge everything into one DLL, replace the permission namespace, or swap to another plugin framework.

The release must contain exactly these 11 projects/DLLs:
1. Kanomjeen.Core
2. Kanomjeen.TPA
3. Kanomjeen.Homes
4. Kanomjeen.Kits
5. Kanomjeen.Respawn
6. Kanomjeen.BuildGuard
7. Kanomjeen.Airdrops
8. Kanomjeen.Stats
9. Kanomjeen.ServerManager
10. Kanomjeen.AdminAudit
11. Kanomjeen.VehicleGuard

Core owns the shared services, including player combat/raid state, zones, persistent cooldowns, player resolver, rate limiting, UI bridge and shared teleport lock.

Keep these gameplay decisions:
- TPA delay 10s, successful teleport cooldown 600s, request timeout 30s baseline
- Home max 2, delay 10s, cooldown 900s baseline
- Combat tag 30s
- Raid tag 180s
- Respawn protection 10s
- TPA/Home cancel on movement/damage/death as configured
- TPA/Home blocked in combat/raid/restricted objectives
- no `/warp`
- no virtual vault
- no broad shop/economy
- no public virtual garage
- no VIP combat/raid/deadzone bypass
- no end-game VIP kits
- no unlimited homes

Do not turn the server into fast-travel/P2W while fixing technical issues.

## Source of truth
Start at the local copy of the **`kanomjeen-suite`** root.

Read these files before changing code:
- `README.md`
- `ARCHITECTURE.md`
- `RELEASE_MANIFEST.md`
- `BUILD.md`
- `INSTALL.md`
- `PERMISSIONS.md`
- `QA_REPORT.md`
- `THIRD_PARTY_NOTICES.md`
- `workshop-ui/UI_CONTRACT.md`
- `workshop-ui/DESIGN_SYSTEM.md`
- `workshop-ui/WORKSHOP_RELEASE.md`

Do not reintroduce the old experimental `Tpa.dll`. `Kanomjeen.TPA.dll` is its replacement.

# PHASE 1 — Machine / Server Inventory
Before editing anything, record the real environment in the final report:
- OS
- `dotnet --info`
- installed Unturned Dedicated Server build
- installed RocketModFix/LDM version
- actual server ID and Rocket Plugins path
- path to the exact game/server assemblies used for API verification
- installed Unity Editor version to be used for Workshop content
- California 2 Workshop IDs/content version currently installed

Do not guess any of these values.

# PHASE 2 — Structural Verification
From the suite root run:

Windows PowerShell:
```powershell
./verify-source.ps1
```

Linux:
```bash
chmod +x verify-source.sh build.sh install.sh
./verify-source.sh
```

If verification fails, fix the source inconsistency first. Do not skip this gate.

# PHASE 3 — Restore + Compile Certification
Run a clean Release build.

Windows:
```powershell
./build.ps1 -Configuration Release
```

Linux:
```bash
./build.sh Release
```

Goal: **zero compiler errors** and exactly 11 DLLs in `dist/plugins/`.

If there is an API-related compile error:
1. identify the exact failing type/member/delegate;
2. inspect the actual installed server assemblies or run/update the existing ApiDump approach;
3. determine the exact signature for this server build;
4. modify only the relevant API adapter/event integration;
5. do not invent signatures;
6. do not delete safety features merely to compile;
7. rebuild from clean state;
8. document the exact changed file/member and why.

Prioritize verification of these API-sensitive paths:
- `DamageTool.playerDamaged`
- `BarricadeManager.onDamageBarricadeRequested`
- `StructureManager.onDamageStructureRequested`
- `BarricadeManager.onDeployBarricadeRequested`
- `StructureManager.onDeployStructureRequested`
- barricade/structure region enumeration and serverside owner data
- `EffectManager.onEffectButtonClicked`
- EffectManager send text/visibility/clear calls
- `UnturnedPlayerEvents.OnPlayerRevive`
- `OnPlayerDeath`
- `OnPlayerUpdateStat`
- `VehicleManager.onEnterVehicleRequested`
- `VehicleManager.onDamageVehicleRequested`
- `LevelManager.airdrop`
- `SteamBlacklist.add/remove`
- `UnturnedPlayer.Ban/Kick`
- `Player.teleportToLocation`

After build, confirm `dist/plugins` contains only the intended Kanomjeen release DLL set for deployment. Do not mix DLLs copied from previous builds.

# PHASE 4 — Static Review After Compile
Before installing, review all modified code for:
- null safety
- event unsubscribe symmetry
- coroutine/timer cleanup
- disconnect cleanup
- persistent data corruption handling
- command/UI spam
- simultaneous TPA/Home teleport race
- stale player/request state
- duplicate item claim path
- ambiguous player resolution
- expensive per-frame/player-wide loops
- synchronous disk writes in hot command paths

Retain the shared Core teleport lock. One player must not be able to run Home and TPA warmups simultaneously.

Do not add `Update()` player scans to solve event problems.

# PHASE 5 — Install to STAGING Server Only
Back up the current server and plugins first.

Use the included installer where possible.

Windows example:
```powershell
./install.ps1 -ServerRoot "<ACTUAL_UNTURNED_ROOT>" -ServerId "<ACTUAL_SERVER_ID>"
```

Linux example:
```bash
./install.sh "<ACTUAL_UNTURNED_ROOT>" "<ACTUAL_SERVER_ID>"
```

For the targeted RocketModFix layout the expected path is:
`Servers/<ServerID>/Rocket/Plugins/`

But verify the actual running installation before changing installer logic.

Required before first staging boot:
- remove/disable old `Tpa.dll`
- back up previous Kanomjeen DLL/data/config
- deploy all 11 DLLs from the **same build**
- do a full server restart; do not rely on Rocket reload to replace loaded assembly code

# PHASE 6 — Server Boot / Runtime API Validation
Start the staging server and inspect all Rocket/Unturned logs.

Required:
- Kanomjeen.Core loads without exception
- all feature plugins load without exception
- TPA/Homes/Kits/Stats/Airdrops/AdminAudit successfully bind to Core callbacks
- no TypeLoadException / MissingMethodException / delegate signature exception
- storage/config paths are writable
- second restart can reload saved XML without corruption

If runtime exposes an API mismatch that the compiler did not catch, verify against exact assemblies and fix the adapter. Do not suppress the exception without understanding it.

# PHASE 7 — California 2 Live Configuration
Do not hardcode California 2 map coordinates into C#.

Using the **installed live California 2 build**, configure Core zones for the actual locations that need protection, including as applicable:
- deadzones
- high-tier progression areas
- safezones
- special/restricted objectives
- any underwater/deadzone areas present in the installed version

Each Core zone should use data/config flags such as:
- BlockTPA
- BlockHome
- BlockBuild
- BlockKit
- HighTier
- Deadzone
- Safezone

For airdrops:
- determine a real valid Airdrop asset ID from the installed content; never invent an ID
- stand at each intended California 2 target and use `/setairdropspawn <name> <airdropId>`
- verify `/kjairdrop <spawn>` and `/whenairdrop`
- confirm dynamic objective radius actually blocks TPA/Home/Kit and configured building

Record the final configured zones/spawns in the release report or backup artifact.

# PHASE 8 — Workshop GUI Build in Unity
Use a Unity version compatible with the **current installed Unturned modding sources**. Do not pick a version by memory if the current toolchain specifies one.

1. Locate and import the installed game's:
   `Unturned/Extras/Sources/Project.unitypackage`
2. Copy repository content:
   `workshop-ui/UnityProject/Assets/KanomjeenUI`
   into the Unity project's:
   `Assets/KanomjeenUI`
3. Obtain a Thai-capable font from an official source with redistribution rights suitable for Steam Workshop (for example, an OFL-licensed Thai family). Do **not** copy a random font from Windows or another product.
4. Put the font at exactly:
   `Assets/KanomjeenUI/Fonts/KanomjeenThai.ttf`
5. Keep any license/notice required by the font license in the Workshop/source release.
6. Allow Unity to compile. Fix every Unity C# error; do not ignore Console errors.
7. Run menu:
   **Kanomjeen -> Build Workshop UI Prefab**
8. Verify generated:
   `Assets/KanomjeenUI/Effects/KanomjeenUI/Effect.prefab`
9. Compare prefab hierarchy/button names against `workshop-ui/UI_CONTRACT.md` exactly.
10. Verify Effect identity remains:
    - ID `51000`
    - GUID `2f8df4942fe04a27a9636942abb3cd21`
    - contract `1.0`

Do not rename `KJ_` callback objects without updating both the UI contract and server implementation.

# PHASE 9 — Export Master Bundle
Using Unturned's current Master Bundle Tool:
- bundle name: `kanomjeen_ui.masterbundle`
- asset prefix: `Assets/KanomjeenUI`
- use the version required by the current Unturned toolchain; repository currently declares Asset_Bundle_Version 6
- enable multiplatform release where supported/required
- generate real binary bundle output
- keep the generated `.masterbundle.hash`
- include platform bundles generated by the tool as appropriate

Expected Workshop root includes at minimum:
```text
Kanomjeen_UI_Workshop/
├── MasterBundle.dat
├── kanomjeen_ui.masterbundle
├── kanomjeen_ui.masterbundle.hash
└── Effects/
    └── KanomjeenUI/
        └── Asset.dat
```
plus generated Linux/macOS bundles when applicable.

**Do not fake, hand-create, rename an empty file, or claim success if Unity did not actually export the masterbundle/hash.**

# PHASE 10 — Local Client GUI Test
Before public Workshop upload, load the UI locally with a staging server and real Unturned client.

Test `/menu` and verify:
- open/close repeatedly
- modal cursor/focus clears correctly
- Main cards open the correct screens
- TPA request shows/accepts/denies with two real clients
- Homes rows match server-side data
- Kit rows and cooldowns match server-side data
- Stats values match chat/data
- Airdrop ACTIVE/STANDBY states work
- Staff card hidden for normal player
- spoofing/admin button attempts are still rejected server-side
- 16:9
- 16:10
- 21:9 / ultrawide
- UI scale variants if available
- Thai text has no missing glyph boxes/diacritic clipping
- English text does not overflow

Fix UI layout in `KanomjeenUiBuilder.cs`, rebuild prefab and re-export the bundle after any GUI change.

# PHASE 11 — Workshop Upload
Upload the validated Kanomjeen UI package through the current Unturned/Steam Workshop publishing workflow.

After receiving the first Workshop File ID:
1. record the ID;
2. set the Owner Workshop File ID / `AssetBundleCustomData` association where supported by the current Unturned toolchain;
3. rebuild/export/reupload as required;
4. add the Workshop item to the dedicated server's Workshop download/content configuration;
5. restart server/client and verify a fresh client can obtain the content and `/menu` works.

Do not put the Workshop File ID inside gameplay plugin code unless there is a documented technical reason; server Workshop configuration owns distribution.

# PHASE 12 — Full Staging QA
Execute **every applicable item in `QA_REPORT.md`**. Do not merely read the checklist.

Minimum required accounts:
- 2 non-admin players
- 1 moderator/admin account

Critical scenarios include:
- TPA success and persistent cooldown
- TPA move/damage/death/disconnect cancellation
- destination disconnect
- combat/raid origin/destination restriction
- ambiguous names
- Home persistence after restart
- TPA/Home simultaneous warmup rejection
- Home higher-limit permission
- kit full-inventory abuse attempt
- respawn protection victim and attacker behavior
- BuildGuard thresholds and performance on a built-up area
- Airdrop dynamic objective restriction lifecycle
- Stats persistence and no double-counted playtime
- ServerManager save before shutdown; external service actually brings server back up
- persistent mute/warn/ban behavior
- audit JSON escaping
- vehicle logging load
- GUI callback permission spoof attempt

If a test fails, fix it, rebuild the full coherent DLL set, redeploy, restart and rerun affected regression tests.

# PHASE 13 — Production Gate
Do NOT mark Production until all `[!]` critical items in `QA_REPORT.md` pass.

Before production:
- create rollback backup of plugins/config/data/Workshop configuration
- ensure no duplicate old plugin remains
- deploy one coherent 11-DLL build
- deploy the matching Workshop UI contract/bundle
- verify permissions for default / VIP / moderator / admin groups
- verify live California 2 zones/spawns
- verify persistent data after restart
- monitor startup/runtime logs for errors

Only after all gates pass:
- change `VERSION` from `0.1.0-staging` to an appropriate production version such as `0.1.0`
- update `CHANGELOG.md` with the compile/API fixes and Workshop release details
- do not call the build production-ready before this point

# Required Final Report
At the end, return one concise but complete report containing:
1. OS, .NET SDK, Unturned server build, RocketModFix/LDM version, Unity version
2. whether `verify-source` passed
3. build result for all 11 DLLs
4. every compile/API/runtime issue found and the exact file/member changed
5. SHA-256 of the 11 final DLLs
6. staging plugin install path
7. rollback/backup path
8. server startup result and any warnings
9. California 2 zones configured and airdrop spawn names/IDs used
10. Unity prefab build result
11. Workshop masterbundle/hash filenames and SHA-256
12. Workshop File ID
13. GUI test resolutions/languages tested
14. QA_REPORT critical pass/fail summary
15. remaining known issues, if any
16. final verdict: `STAGING FAILED`, `STAGING PASSED`, or `PRODUCTION READY`

Never hide a failed test. Never invent a version, API signature, Workshop ID, bundle/hash, test result, or success state.
