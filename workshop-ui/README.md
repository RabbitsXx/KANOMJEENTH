# Kanomjeen Workshop UI

Steam Workshop client UI source for the Kanomjeen Unturned server.

## What is included
- `MasterBundle.dat` — master bundle identity/version.
- `Effects/KanomjeenUI/Asset.dat` — Effect asset metadata; default ID `51000`.
- `UI_CONTRACT.md` — stable server/client element names and behavior.
- `DESIGN_SYSTEM.md` — visual/UX rules.
- `WORKSHOP_RELEASE.md` — Unity build and Workshop publishing steps.
- `UnityProject/Assets/KanomjeenUI/Editor/KanomjeenUiBuilder.cs` — Unity Editor generator for the actual UI Effect prefab.

## Architecture
Kanomjeen v1 uses **one Effect prefab** containing multiple screen containers instead of separate effect IDs for every feature. Benefits:
- one stable Effect ID and UI key
- lower client/server state complexity
- shared shell/typography/layout
- screen transitions use server-controlled visibility
- feature plugins still own their own callbacks/business rules

The generated prefab path is:

```text
Assets/KanomjeenUI/Effects/KanomjeenUI/Effect.prefab
```

Effect definition:

```text
GUID 2f8df4942fe04a27a9636942abb3cd21
Type Effect
ID 51000
Lifetime 0
```

The default server Core config must use `UiEffectId = 51000` unless you intentionally change both sides.

## Generate the prefab
1. Create/open the Unity project prepared for Unturned modding.
2. Import the current `Unturned/Extras/Sources/Project.unitypackage` from the installed game.
3. Copy/merge the contents of `UnityProject/Assets/KanomjeenUI` into the project's `Assets/KanomjeenUI` folder.
4. In Unity choose **Kanomjeen → Build Workshop UI Prefab**.
5. Confirm `Assets/KanomjeenUI/Effects/KanomjeenUI/Effect.prefab` exists.
6. Copy the root Workshop metadata (`MasterBundle.dat` and `Effects/KanomjeenUI/Asset.dat`) into the export/package source as described in `WORKSHOP_RELEASE.md`.

The Editor builder creates the Canvas, layout, buttons and dynamic text fields programmatically. This avoids shipping private font files and keeps player-facing text server/localization driven.

## Screens
- Main / Quick Actions
- TPA
- Homes
- Kits
- Stats
- Airdrop
- Staff/Admin
- Toast surface

Open the UI in-game with `/kjmenu` or `/menu`.

## Visual direction
- dark translucent graphite shell
- restrained amber accent
- high-contrast white/muted text
- no MMO-style decorative clutter
- 1920×1080 reference canvas with Scale With Screen Size
- centered content safe for 16:9, 16:10 and wider displays
- dynamic text rather than baked UI copy

## Workshop binary status
The repository contains **source and metadata**, not an already-exported `.masterbundle`.

A valid release must be exported from Unity with Unturned's current Master Bundle workflow, including multiplatform output and the generated `.hash`. Do not create an empty/fake `.masterbundle` just to satisfy a filename.

See `WORKSHOP_RELEASE.md` for the release gate.
