# AGENTS.md — Working Instructions For AI Coding Agents

This file is the entry point for any AI agent (or new human contributor) who continues work on the
**Kanomjeen** Unturned server suite. Read it fully before editing anything.

The repository is intentionally documentation-heavy. This file tells you *which* document is
authoritative for *what*, and which actions are safe to take alone versus which require a human
decision.

---

## 1. What this project is

Kanomjeen is a **custom RocketModFix / LDM plugin suite for an Unturned dedicated server** plus the
matching **client-side Workshop UI**.

| Item | Value |
| --- | --- |
| Game | Unturned Dedicated Server |
| Map target | California 2 |
| Gameplay | Semi-Vanilla Survival PvP |
| Framework | RocketModFix / LDM |
| C# target | `netstandard2.1` (see `Directory.Build.props`) |
| Repo version | `VERSION` = `0.1.0-staging` |
| UI contract | `1.0` (`workshop-ui/UI_CONTRACT.md`) |
| Workshop Effect ID | `51000` |
| Workshop Effect GUID | `2f8df4942fe04a27a9636942abb3cd21` |
| Git remote | `https://github.com/RabbitXx/KANOMJEENTH.git` (branch `main`) |

### Two halves that must stay versioned together

```text
src/**  (Rocket plugin DLLs, 11 projects)  ── talks to ──  workshop-ui/** (Unity Effect bundle)
```

* The **server DLLs** own authority, validation, persistence and gameplay logic.
* The **Workshop bundle** is only presentation. Every GUI click is a *request*; the server
  re-validates permission, state, cooldown and target before acting.
* A breaking UI change means staging both halves together. Never rename a GUI element and leave
  old server callbacks live.

### Authority documents (do not duplicate their content here — update them instead)

| Question | Authoritative document |
| --- | --- |
| Dependency direction, service ownership, gameplay baselines, performance/security rules | `ARCHITECTURE.md` |
| Release artifact list, included/excluded launch features, version references | `RELEASE_MANIFEST.md` |
| Which files/identities must exist (machine-checkable gate) | `verify-source.sh` / `verify-source.ps1` |
| Compile steps and the API/version gate | `BUILD.md` |
| Deploying DLLs to a Rocket server, first boot, config checks | `INSTALL.md` |
| Permission matrix | `PERMISSIONS.md` |
| In-game command surface | `COMMANDS.md` |
| Test matrix and release gates | `QA_REPORT.md` |
| Unity build + Workshop publication steps | `workshop-ui/WORKSHOP_RELEASE.md` |
| Full end-to-end runbook on the real deployment PC | `FINAL_LOCAL_PROMPT.md` |
| Client/server element names and API | `workshop-ui/UI_CONTRACT.md` |
| Visual rules (colors, type, spacing, screen hierarchy) | `workshop-ui/DESIGN_SYSTEM.md` |
| Third-party/licensing obligations | `THIRD_PARTY_NOTICES.md` |

---

## 2. Repository map

```text
KANOMJEENTH/
├── src/Kanomjeen.*/                 # 11 plugin projects (Core + 10 features)
│   ├── Kanomjeen.Core/              # shared services + UI session bridge + Waypoints
│   │   ├── Configuration/KanomjeenCoreConfiguration.cs   # contains UiEffectId = 51000
│   │   ├── Services/                # PlayerStateService, ZoneService, GameplayGuard, UiService, ...
│   │   └── Waypoints/               # WaypointModel, WaypointService, WaypointStore
│   └── Kanomjeen.{TPA,Homes,Kits,Respawn,BuildGuard,Airdrops,Stats,ServerManager,AdminAudit,VehicleGuard}/
├── dist/plugins/                    # build output: the 11 release DLLs (gitignored)
├── workshop-ui/                     # Workshop-facing UI source + Unity project
│   ├── MasterBundle.dat             # bundle identity: name / prefix / version 6
│   ├── Effects/KanomjeenUI/Asset.dat# Effect GUID + ID 51000
│   ├── UI_CONTRACT.md, DESIGN_SYSTEM.md, WORKSHOP_RELEASE.md, README.md
│   └── UnityProject/                # Unity 2022.3.62f3 project
│       ├── Assets/KanomjeenUI/Editor/KanomjeenUiBuilder.cs   # builds + validates the prefab
│       ├── Assets/KanomjeenUI/Fonts/                          # KanomjeenThai.ttf (gitignored) + OFL.txt
│       ├── Assets/KanomjeenUI/Effects/KanomjeenUI/Effect.prefab
│       ├── Assets/Editor/Assembly-CSharp-Editor/Tools/        # Unturned master bundle helpers
│       ├── WorkshopExport/          # generated bundle + hash + manifest (tracked on purpose)
│       └── Logs/                    # Unity batchmode logs (gitignored)
├── Kanomjeen_UI_Workshop/           # THE FOLDER YOU UPLOAD TO STEAM WORKSHOP
├── certification/                   # dated compile/runtime evidence
├── dev/ApiDump/                     # helper to dump Rocket/Unturned API surface
├── build.sh / build.ps1             # build all 11 DLLs → dist/plugins
├── install.sh / install.ps1         # deploy DLLs to the server's Rocket/Plugins
├── verify-source.sh / .ps1          # structural gate (must pass before release)
└── AGENTS.md                        # this file
```

`Kanomjeen_UI_Workshop/` is a **release artifact**, not a source folder. It is the exact folder
steam Workshop should receive:

```text
Kanomjeen_UI_Workshop/
├── MasterBundle.dat
├── kanomjeen_ui.masterbundle
├── kanomjeen_ui.masterbundle.hash
├── kanomjeen_ui_linux.masterbundle
├── kanomjeen_ui_mac.masterbundle
└── Effects/KanomjeenUI/Asset.dat
```

`.manifest` files stay in `workshop-ui/UnityProject/WorkshopExport/`; they are build metadata, not
Workshop content.

---

## 3. Hard invariants — never change these without an explicit human instruction

1. **Effect identity is frozen.** `Asset.dat` must keep `GUID 2f8df4942fe04a27a9636942abb3cd21` and
   `ID 51000`. `Kanomjeen.Core` must keep `UiEffectId = 51000`. `verify-source.sh` greps both.
2. **`MasterBundle.dat` identity is frozen.** `Asset_Bundle_Name kanomjeen_ui.masterbundle`,
   `Asset_Prefix Assets/KanomjeenUI`, `Asset_Bundle_Version 6` (Unity 2022 LTS generation).
3. **UI element names are a contract.** Every `KJ_*` object in the prefab is referenced by server
   code. Renaming requires: bump `UiContractVersion` → update `UI_CONTRACT.md` → update the Unity
   builder → update server callbacks → stage both sides → only then publish.
4. **Never invent binary bundles.** `*.masterbundle` / `*.hash` must come from Unity's Master Bundle
   export. Do not hand-edit, rename, or fabricate them.
5. **Permission namespace is `kanomjeen.*` only.**
6. **Interaction rules stay in Core.** Feature plugins must not re-implement combat/raid tagging,
   cooldowns, zone checks, player resolution, rate limiting or UI session state.
7. **No per-frame global player scans, no expensive LINQ in hot paths**; unsubscribe static events
   and cancel coroutines/invokes on plugin unload.
8. **Map data lives in configuration.** No California 2 coordinates, airdrop asset IDs or spawn
   points hardcoded in C#.
9. **Release DLL set is exactly 11 assemblies**; never mix DLLs from different builds.
10. **Launched-and-excluded features stay excluded** (no public `/warp`, no virtual vault/garage, no
    item/vehicle economy shop, no VIP combat/raid bypass, no end-game starter kit). See
    `RELEASE_MANIFEST.md`.

---

## 4. Toolchain on the current machine (verified 2026-09-14)

| Tool | Location / version |
| --- | --- |
| .NET SDK | `10.0.203` (`dotnet --list-sdks`) |
| Unity Hub | `C:\Program Files\Unity Hub`; `secondaryInstallPath` = `F:\Unity\Hub\Editor` |
| **Unity for this project** | `F:\Unity\Hub\Editor\2022.3.62f3-x86_64\Editor\Unity.exe` |
| Orphaned Unity install | `F:\Unity\Hub\Editor\2022.3.62f3\` — incomplete (no `modules.json`); safe to delete, do not use |
| Unity 6 installs | `C:\Program Files\Unity\Hub\Editor\6000.3.15f1`, `6000.4.6f1` — **must never open `workshop-ui/UnityProject`** |
| Unturned client/server | `F:\SteamLibrary\steamapps\common\Unturned` (provides `Extras/Sources/Project.unitypackage`) |
| Unity project | `workshop-ui/UnityProject` (`ProjectVersion.txt` = `2022.3.62f3`) |

### Unity version trap (this has already bitten the project once)

Opening `workshop-ui/UnityProject` with Unity 6 rewrites `Library/` (the batchmode log shows
`Clearing Bee directory 'Library/Bee' ... previous hash was ... (Unity version: 6000.3.15f1)`) and
Unity 6 does **not** support this Unturned SDK generation, so bundles exported by it are not
valid for `Asset_Bundle_Version 6`. Always export with `2022.3.62f3`. If a Unity 6 session touched
the project, just run the export again with 2022.3.62f3 — the builder regenerates everything.

---

## 5. Build the server plugins

```bash
cd /f/KANOMJEENTH          # repo root
./verify-source.sh         # structural gate: required files, 11 projects, identity greps
./build.sh                 # Release build of Core first, then features → dist/plugins/*.dll
```

Windows PowerShell equivalents: `./verify-source.ps1`, `./build.ps1`.

Manual single-project build:

```bash
dotnet build src/Kanomjeen.Core/Kanomjeen.Core.csproj -c Release
```

Rules:

* `verify-source.sh` is a **structural** check only. A pass does not prove compilation or runtime
  correctness.
* If a compile error comes from an Unturned/Rocket API member, **do not guess a signature**. Compare
  against the installed server build, inspect the assemblies (or use `dev/ApiDump`), then update the
  adapter/event code and re-run staging tests. See `BUILD.md` §3.
* After an Unturned stable update, re-run the whole API gate: compile → staging boot → `QA_REPORT.md`
  critical scenarios.

---

## 6. Build and export the Workshop UI

Prerequisites: the Unity project already contains the Unturned `Project.unitypackage` sources
(`Assets/Runtime/Assembly-CSharp/Unturned/**` and `Assets/Editor/Assembly-CSharp-Editor/Tools/**`) and
`Assets/Plugins/UnityEngine.UI.dll`. Do not delete them.

### 6.1 The font slot (release gate)

The builder resolves the Thai font by exact path:

```text
workshop-ui/UnityProject/Assets/KanomjeenUI/Fonts/KanomjeenThai.ttf
```

* Expected content: **Noto Sans Thai** (SIL OFL 1.1), official source
  <https://github.com/google/fonts/tree/main/ofl/notosansthai>. `OFL.txt` beside it already carries
  the matching license text — keep them together.
* The font binary and its `.meta` are **deliberately gitignored** (`.gitignore` lines 35–36), so a
  fresh clone does not have it. Re-create it with:

  ```bash
  curl -L -o "workshop-ui/UnityProject/Assets/KanomjeenUI/Fonts/KanomjeenThai.ttf" \
    "https://raw.githubusercontent.com/google/fonts/main/ofl/notosansthai/NotoSansThai%5Bwdth%2Cwght%5D.ttf"

  # integrity check — must print the upstream git blob hash
  git hash-object workshop-ui/UnityProject/Assets/KanomjeenUI/Fonts/KanomjeenThai.ttf
  # 34b48ab6f74867dbfce19410a2f452abef34e3ff
  ```

  Anything other than the upstream hash means a modified/incorrect font — stop and investigate.
* Consequence of the ignore rule: the tracked `Effect.prefab` contains a font GUID that only exists
  in the local (ignored) `.meta`. This is safe because `ExportMasterBundle()` **rebuilds** the prefab
  before every export, re-resolving the font by path. A clone that opens the prefab *before*
  re-exporting will show a missing font reference — re-export instead of fixing it by hand.
* The font is a variable font (`[wdth,wght]`); Unity 2022 imports the default instance (Regular), so
  bold text is synthesized. Validate Thai rendering on a real client before publishing.
* While the font is missing, the builder still produces a prefab using a Unity fallback font and logs:
  `[Kanomjeen] Thai font asset not found ...`. **That warning is a release blocker**, not a cosmetic
  issue — `WORKSHOP_RELEASE.md` requires Thai coverage when Thai text ships.

### 6.2 The two Unity menu items

| Menu item | Method | Effect |
| --- | --- | --- |
| `Kanomjeen/Build Workshop UI Prefab` | `Kanomjeen.EditorTools.KanomjeenUiBuilder.Build` | Regenerates the whole prefab programmatically, validates Text pivots, saves `Effect.prefab` |
| `Kanomjeen/Export Workshop UI Master Bundle` | `Kanomjeen.EditorTools.KanomjeenUiBuilder.ExportMasterBundle` | Calls `Build()` first, assigns the bundle name, then exports multiplatform bundles + hash |

Always use the **export** entry point. Never export a stale prefab.

### 6.3 Headless export (the command that actually produced the current release)

```bash
"/f/Unity/Hub/Editor/2022.3.62f3-x86_64/Editor/Unity.exe" -batchmode -nographics -quit -accept-apiupdate \
  -projectPath "F:/KANOMJEENTH/workshop-ui/UnityProject" \
  -executeMethod Kanomjeen.EditorTools.KanomjeenUiBuilder.ExportMasterBundle \
  -logFile "F:/KANOMJEENTH/workshop-ui/UnityProject/Logs/export-workshop-ui-<reason>.log"
```

Expected: exit code `0`, log ends with `Exiting batchmode successfully now!`, and contains both
`[Kanomjeen] Workshop UI prefab created: ...` and
`[Kanomjeen] Workshop UI master bundle export completed: ...`.

Harmless noise you can ignore in batchmode logs: `APIUpdater.Framework.Configuration` type-initializer
exception, `Clearing Bee directory`, and shader/`PhysX` initialization lines.

### 6.4 Outputs

`workshop-ui/UnityProject/WorkshopExport/` receives, for each of Windows / Linux / macOS:

```text
kanomjeen_ui.masterbundle            kanomjeen_ui_linux.masterbundle     kanomjeen_ui_mac.masterbundle
kanomjeen_ui.masterbundle.hash       (*.manifest files are build metadata)
```

The `.hash` is generated by `EditorAssetBundleHelper.HashAssetBundle` and is exactly **61 bytes**:

```text
byte  0      : format version, always 0x02
bytes 1..20  : SHA1(windows bundle)
bytes 21..40 : SHA1(linux bundle)
bytes 41..60 : SHA1(mac bundle)
```

If Linux/macOS bundles are missing, the helper deletes any stale `.hash` instead of writing a
misleading one — a missing hash means "multiplatform export incomplete".

### 6.5 Refresh the Workshop package

```bash
cd /f/KANOMJEENTH
S=workshop-ui/UnityProject/WorkshopExport
D=Kanomjeen_UI_Workshop
cp -f "$S/kanomjeen_ui.masterbundle"       "$D/"
cp -f "$S/kanomjeen_ui.masterbundle.hash"  "$D/"
cp -f "$S/kanomjeen_ui_linux.masterbundle" "$D/"
cp -f "$S/kanomjeen_ui_mac.masterbundle"   "$D/"
```

`MasterBundle.dat` and `Effects/KanomjeenUI/Asset.dat` are hand-maintained metadata and must stay
byte-identical to `workshop-ui/MasterBundle.dat` and
`workshop-ui/Effects/KanomjeenUI/Asset.dat`.

---

## 7. Verification recipes (run these after every UI export)

```bash
cd /f/KANOMJEENTH
P=workshop-ui/UnityProject/Assets/KanomjeenUI/Effects/KanomjeenUI/Effect.prefab

# 1. every screen exists, including the newest one (waypoints)
grep -o "KJ_Screen_[a-z]*" "$P" | sort -u
#   expect: admin airdrop homes kits main stats toast tpa waypoints

# 2. the newest screen's elements are present (8 reusable rows)
grep -o "KJ_Waypoint_[A-Za-z_0-9]*" "$P" | sort -u | head

# 3. the prefab is bound to a real font, not the built-in fallback
grep -o "m_Font: {fileID: [0-9]*, guid: [a-f0-9]*, type: [0-9]*}" "$P" | sort | uniq -c
grep '^guid:' workshop-ui/UnityProject/Assets/KanomjeenUI/Fonts/KanomjeenThai.ttf.meta
#   the guid in m_Font must equal the font .meta guid (a guid of all zeroes = fallback font)

# 4. no Thai-font release-blocker warning in the export log
grep -c "Thai font asset not found" workshop-ui/UnityProject/Logs/export-workshop-ui-*.log   # must be 0

# 5. bundles are genuine Unity 2022 exports
for f in Kanomjeen_UI_Workshop/*.masterbundle; do strings -n 6 "$f" | head -2 | tr '\n' ' '; echo " <- $f"; done
#   expect: UnityFS 2022.3.62f3

# 6. hash matches the shipped bundles
xxd -p Kanomjeen_UI_Workshop/kanomjeen_ui.masterbundle.hash | tr -d '\n'
sha1sum Kanomjeen_UI_Workshop/kanomjeen_ui.masterbundle \
        Kanomjeen_UI_Workshop/kanomjeen_ui_linux.masterbundle \
        Kanomjeen_UI_Workshop/kanomjeen_ui_mac.masterbundle
#   hex must be 02 + sha1(win) + sha1(linux) + sha1(mac), concatenated

# 7. package contains exactly the six release files
find Kanomjeen_UI_Workshop -type f | sort
```

Sizes are useful smoke signals: a bundle without an embedded font is ~28 KB, with Noto Sans Thai
embedded it is ~139 KB. A sudden drop usually means the font slot is empty again.

---

## 8. Known traps and lessons learned

1. **Unity 6 must not open the Unity project** (see §4). Its `Library/` rewrite is not a source
   change, but any bundle it exports is invalid for this SDK generation.
2. **`.gitignore` is asymmetric on purpose.** `*.masterbundle` / `*.hash` are ignored globally, with
   an explicit whitelist for `workshop-ui/UnityProject/WorkshopExport/`. The files inside
   `Kanomjeen_UI_Workshop/` are tracked because they were committed before the ignore rules were
   added — if you ever add a *new* bundle filename there, you must adjust the ignore rules or use
   `git add -f`, otherwise the release package silently ships stale binaries.
3. **Stray files must not enter the Workshop folder.** A 1-byte `Object.meta` had accumulated in
   `Kanomjeen_UI_Workshop/` (a leftover, not part of the release); it was removed on 2026-09-14.
   Keep the folder to the six files listed above.
4. **Do not "fix" a stale bundle by editing it.** A prefab that looks correct in the Editor is not
   proof the *bundle* contains it — the bundle is only regenerated by the export step.
5. **`Effect.prefab` is a generated file.** Hand edits are lost on the next `Build()`. Change
   `KanomjeenUiBuilder.cs` instead.
6. **Text pivots matter.** `ValidateTextLayout` exists because aligned uGUI labels with a center
   pivot once displaced titles outside their cards. Keep validation in place.
7. **One Effect instance per UI session.** `UiService.Open` sends a single Effect and switches
   screens by visibility; do not revert to stacking Effect instances per screen.
8. **`Logs/`, `Library/`, `Temp/`, `dist/` are gitignored** — never commit them, never treat a
   missing `Library/` as a broken project.
9. **Server config and code must agree.** `UiEffectId` (Core config) must equal the Workshop
   `Asset.dat` ID. Waypoints run in `WaypointMode = FallbackNativeMarker` unless a supported client
   module is actually installed — do not flip it speculatively, and do not fabricate a capture signal.
10. **No silent gameplay changes.** Balance values in `ARCHITECTURE.md` and `COMMANDS.md` are
    release decisions; changing them is a product decision, not a refactor.

---

## 9. Publishing to Steam Workshop (where the item goes)

Unturned publishes mods from inside the game — there is no separate uploader tool.

1. Launch the **Unturned client** on the machine that holds the package (Steam account must own
   Unturned and have accepted the Steam Subscriber Agreement + Supplemental Workshop Terms).
2. Main menu → **Workshop** tab → **Submit**.
3. Fill the fields:

   | Field | Value for this project |
   | --- | --- |
   | Name | `Kanomjeen UI` (client UI for the Kanomjeen server) |
   | **Collection Path** | `F:\KANOMJEENTH\Kanomjeen_UI_Workshop` — the folder itself, containing `MasterBundle.dat`, the four bundle/hash files and `Effects/` |
   | Preview Image | a `.png`/`.jpg` path — `F:\KANOMJEENTH\logo.png` exists and can be used, or supply a 512×512+ UI screenshot |
   | Change Note | e.g. `Waypoints screen + Thai font (Noto Sans Thai, OFL)` |
   | Asset Type | mod/content type for this Effect (`Mods`); pick the closest UI/mod category |
   | Visibility | `Public` when releasing; keep `Private`/`Unlisted` for staging |
   | Allowed IPs | leave empty (only needed to restrict auto-download to specific servers) |
   | Workshop Section | `Ready-to-Use` |

4. Click **Create**. Updating later uses the same screen: fill the fields (Name/Preview may be left
   blank) and pick the existing upload from the list at the bottom.

After the first successful upload:

* Record the **Workshop File ID** in `RELEASE_MANIFEST.md` (and in the release/QA notes).
* If the Unity toolchain supports it, set the Owner Workshop File ID (`AssetBundleCustomData`) and
  re-export/re-upload so the bundle is associated with the item. Never put the file ID into plugin
  code.
* Add the item to the dedicated server's Workshop download list (`WorkshopDownloadConfig.json`).
  Verify the schema against the file the installed server generates instead of copying a schema from
  memory.
* Then test `/menu` (`/kjmenu`) with a client that has the Workshop content subscribed.

Documented reference: <https://docs.smartlydressedgames.com/en/latest/about/steam-workshop.html>

---

## 10. Definition of done for a UI release

`workshop-ui/WORKSHOP_RELEASE.md` holds the full checklist. The machine-verifiable gates are
§5 (source verification), §6.3 (clean export), §7 (verification recipes). The gates that
**require a human with a real client** are:

- `/menu` displays correctly on a real client with the Workshop item subscribed.
- Modal cursor/focus clears on `KJ_Close`.
- Every main card and feature callback works, including the Waypoints rows
  (`KJ_Waypoint_Row_0..7` track/delete) and `KJ_Waypoint_Stop`.
- TPA accept/deny with two clients; home rows after reconnect/restart; kit claim with a full
  inventory; stats after kill/death/reconnect; airdrop ACTIVE/STANDBY; staff button hidden and
  rejected server-side for non-staff.
- 16:9 / 16:10 / ultrawide pass and **Thai/English text pass**.
- Multiplayer/runtime scenarios in `QA_REPORT.md`.

Never describe the package as "production-ready" while any of those are unverified. Report status as
*staged / structurally verified* and list what remains.

---

## 11. Current release state (as of 2026-09-14)

* `Kanomjeen_UI_Workshop/` was re-exported with Unity **2022.3.62f3** and now contains the
  **Waypoints** screen (`KJ_Screen_waypoints`, rows `KJ_Waypoint_Row_0..7`) plus an embedded
  **Noto Sans Thai** font, so the Thai-font release-blocker warning is cleared.
* Bundle sizes: windows `138949`, linux `139016`, mac `139166` bytes; `.hash` 61 bytes (format `02` +
  three SHA1 values, verified against the shipped bundles).
* Still open: the in-client visual pass (Thai glyph rendering, bold synthesis, long-string wrapping)
  and everything in `QA_REPORT.md` that needs a live server.

---

## 12. Working agreement for agents

**Do without asking**

* Read any file, run `verify-source`, `build.sh`, the headless Unity export, and the §7 verification
  recipes.
* Fix compile errors, bugs, or documentation inconsistencies inside the existing architecture.
* Regenerate `Effect.prefab`, the bundles, the hash and the `Kanomjeen_UI_Workshop/` copies after any
  UI change, then re-verify.
* Update `CHANGELOG.md` and the affected authoritative document in the same change.

**Ask the human first**

* Changing the frozen identities (Effect ID/GUID, master bundle name/prefix/version, contract
  version) or any published `KJ_*` element name.
* Adding/removing launch features, changing balance values, or editing `.gitignore`.
* Downloading third-party assets, deleting Unity installs or any other large/irreversible cleanup.
* Publishing to Steam Workshop, `git push`, or any deploy touching the live server.

**Reporting style**

* State what you verified and how (command + observed result), and separate *verified* from
  *assumed*.
* When something still needs a human with a client or server, say so explicitly instead of implying
  it passed.
* Keep commits conventional and scoped (`feat:`, `fix:`, `docs:`, ...), matching the existing log.
