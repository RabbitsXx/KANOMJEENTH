# Kanomjeen UI Design System v1.0

## Product intent
The UI should feel like a restrained survival-server interface layered onto Unturned, not an MMO dashboard. Information density is intentionally low and every screen should answer one task quickly.

## Visual tokens
Reference values implemented by `KanomjeenUiBuilder.cs`:

- Backdrop: RGBA `8,12,15,220`
- Primary surface: `22,29,35,250`
- Secondary surface: `31,40,47,255`
- Accent amber: `229,172,71,255`
- Primary text: `241,245,247,255`
- Muted text: `157,169,176,255`
- Danger: `196,78,78,255`
- Success: `78,161,111,255`

## Canvas/layout
- Reference resolution: 1920×1080
- Canvas scaling: Scale With Screen Size
- Main shell: approximately 980×700
- Main content screen: approximately 900×500
- Keep critical controls in the central safe area.
- Avoid edge-anchored critical buttons that can become awkward on 21:9.

## Typography
- Workshop source uses a Unity built-in runtime font fallback so no font binary is redistributed from this repository.
- Headings: bold, short, uppercase English labels where useful.
- Body: concise and dynamic.
- Thai/English content must be tested in the actual Unity/Unturned runtime before Workshop release.
- Do not bake player names, cooldowns, server states or translated strings into textures.

## Interaction hierarchy
1. Main menu presents only major services.
2. Feature screen provides one primary action/state.
3. Destructive actions use the danger surface.
4. Server-side validation always determines the result.
5. Chat remains the fallback for complex text input and targeted moderation.

## Main menu
Cards:
- TPA
- Homes
- Kits
- Stats
- Airdrop
- Staff (permission-hidden)

Each card includes a title, two-line purpose and left accent marker.

## TPA
The request panel is hidden when browsing TPA normally and becomes visible for an incoming request. This keeps the UI from implying an actionable request when none exists.

Primary button hierarchy:
- Accept — success
- Deny — danger
- Cancel mine — neutral

## Homes
Six visible row slots are reserved even though the default server limit is two. This allows permission-based limits without changing the Workshop UI. The server hides unused rows.

Home creation remains `/home set <name>` in v1 because freeform text input through chat is simpler, more robust and auditable.

## Kits
Eight row slots. Cooldown state is displayed adjacent to the kit name. Claim is the only row action.

## Stats
Use summary cards for instantly comparable numeric values and horizontal rows for long-duration values.

## Airdrop
Airdrop UI is a status screen, not a teleport/map shortcut. It communicates:
- region/spawn label
- distance
- objective timer
- ACTIVE/STANDBY state
- travel restriction warning

## Staff
The Main Staff card is hidden when `kanomjeen.admin.inspect` is absent. The server still validates every admin action.

Only low-risk personal toggles are buttons in v1:
- God
- Vanish

Targeted moderation stays command-based because target/reason text should be explicit in audit records.

## Accessibility/QA targets
Before Workshop release test:
- 1920×1080
- 1920×1200
- 2560×1080 / wider equivalent
- UI scaling or display scaling variations available in the test environment
- long Steam/display names
- long home names up to configured 24 characters
- Thai strings
- color readability in dark and bright in-game scenes
- mouse focus/modal state closing correctly

## Change control
Element names beginning `KJ_` are API contracts, not cosmetic object names. Renaming one requires a UI contract/server code change and version bump.
