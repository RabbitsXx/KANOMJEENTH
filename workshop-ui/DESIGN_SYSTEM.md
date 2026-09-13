# Kanomjeen UI Design System v2.0

## Product intent
The UI should feel like a restrained survival-server interface layered onto Unturned, not an MMO
dashboard. Information density is intentionally low and every screen should answer one task quickly.

## Provenance of the v2 tokens
v1 used an amber accent and a custom dark-grey palette. v2 is measured from two commercial Effects
the project benchmarks against — `Supernovea Itemshop V2` (3477290482) and `Supernovea RankQuest V2`
(3478575975). Their shipped bundles were fetched with SteamCMD and read with the in-repo
`BundleInspector` editor tool, which prints every GameObject rect, Image colour, Text setting and
Button ColorBlock. See `AGENTS.md` §13.6 for the raw measurements.

No asset, font file or code was copied from those Effects. Their embedded fonts are all OFL, but
Kanit was downloaded from the upstream Google Fonts repository instead of being extracted from their
bundles, and the layout below is original work built on the same tokens.

## Visual tokens
Implemented by `KanomjeenUiBuilder.cs`:

| Token | Value | Source |
| --- | --- | --- |
| Backdrop dim | `#000000DD` | ref overlay |
| Table surface (`KJ_Root`) | `#1A1A1AE6` | ref table `#1A1A1AC5`, slightly more opaque for legibility |
| Header band | `#000000FF` | ref opaque header bar |
| Card / row surface | `#212121FF` | ref card background |
| Recessed surface | `#161616FF` | ref scroll/inset tone `#161616DE` |
| Neutral surface | `#3C3C3CFF` | ref secondary surface |
| Accent (affirmative) | `#4ADC43FF` | ref buy/confirm green |
| Danger | `#9F1B1BFF` | ref sell/remove red |
| Warning | `#FF9500FF` | ref warning orange |
| Text | `#FFFFFFFF` | ref primary text |
| Text dim | `#DBDBDBFF` | ref large-value text |
| Muted | `#9E9E9EFF` | neutral grey, WCAG-passing on `#212121` |
| Text on accent fill | `#101010FF` | contrast fix: white on `#4ADC43` is too low |
| Hairline rule | `#FFFFFF1E` | header/content separator |

Button ColorBlock follows the reference exactly: the sprite tint carries the colour, and the state
block only shades it — `normal #FFFFFF`, `highlighted #F5F5F5`, `pressed #C8C8C8`,
`disabled #C8C8C880`.

Panels, cards, rows and buttons use Unity's built-in nine-slice `UI/Skin/UISprite.psd` (sliced), the
same sprite the reference Effects draw their surfaces with.

## Typography
Kanit (SIL OFL 1.1, bundled in three weights — see `Assets/KanomjeenUI/Fonts/README.md`):

| Role | Size | Weight |
| --- | --- | --- |
| Screen title / hero number | 30–44 | Bold |
| Card title, row title | 19–24 | SemiBold |
| Body, help text | 15–16 | Regular |
| Button label, chip, meta | 13–14 | Bold / SemiBold |
| Footer, contract version | 13–14 | Regular |

Thai and Latin share the same family, so bilingual strings keep one voice. Headings stay short and
uppercase. Do not bake player names, cooldowns, server states or translated strings into textures.

## Canvas / layout
- Reference resolution 1920×1080, `ScaleWithScreenSize`.
- **Match width (`matchWidthOrHeight = 0`)**, as both reference Effects do. Check ultra-wide (21:9)
  and 16:10 before release; height scales with width in this mode.
- Shell `KJ_Root`: 1180×700, centred, 6px accent bar on the left edge.
- Header band: 1180×96, opaque black, holding brand, title, status and the close button, closed by a
  1px rule.
- Content band: 1100×496, centred at `y = -52`; screens never overlap the header band.
- Footer band: contract version (right) and a server-validation note (left).
- List rows are 1058 wide with an 8px vertical gap: 8 rows at 46px pitch 54 (waypoints, kits),
  6 rows at 56px pitch 64 (homes).
- Everything sits on an 8px grid.

## Interaction hierarchy
1. Main menu presents only major services, as 542×120 cards (two columns) plus a full-width staff card.
2. Feature screens provide one primary action/state.
3. Destructive actions use the danger surface; affirmative actions use the accent surface.
4. Server-side validation always determines the result; nothing in the UI grants authority.
5. Chat remains the fallback for complex text input and targeted moderation.

## Screen notes
- **TPA** — the request panel is hidden while browsing and appears for an incoming request; it has an
  accent edge marker so it reads as an interruption.
- **Homes** — six row slots are reserved regardless of the configured limit; the server hides unused
  rows and creation stays on `/home set <name>`.
- **Kits** — cooldown state is shown right-aligned in the row, claim is the only row action.
- **Stats** — six stat cards (44px values) plus two wide duration rows.
- **Airdrop** — a status screen, not a map shortcut: state chip, region, distance, objective timer and
  the travel-restriction warning in warning orange.
- **Staff** — the main-menu card is hidden without `kanomjeen.admin.inspect`; only god/vanish are
  buttons, every targeted action stays a command so the audit trail keeps its target and reason text.
- **Toast** — a 760×112 notification surface with an accent edge, reserved for short-lived notices.

## Accessibility / QA targets
Before Workshop release test: 1920×1080, 1920×1200, 2560×1080 and a 21:9 resolution; long Steam and
display names; home names up to the configured 24 characters; Thai strings; colour readability over
dark and bright scenes; mouse focus and modal-close behaviour.

## Change control
Element names beginning `KJ_` are API contracts, not cosmetic object names. v2 changes only geometry,
colour, typography and decoration, so the contract stays at **1.0**. Renaming or removing a bound
element requires a UI contract/server change and a version bump.
