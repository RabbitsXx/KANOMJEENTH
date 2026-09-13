# Kanomjeen UI Contract v1.0

This contract is shared by the Workshop Effect prefab and the RocketModFix plugins. The server default is `UiContractVersion = 1.0`, Effect ID `51000`, UI key `23001`.

## Security model
GUI state is presentation only. A click is a request to the server, not authority.

Server requirements:
- keep the active screen per player
- ignore callbacks from screens not issued to that player
- repeat permission/cooldown/combat/raid/zone/target validation
- rate-limit interactive callbacks
- resolve row indexes against the player's current server-side list
- never infer moderation authority from whether an admin button was visible client-side

## Screen containers
Exactly one normal screen is shown at a time:
- `KJ_Screen_main`
- `KJ_Screen_tpa`
- `KJ_Screen_homes`
- `KJ_Screen_kits`
- `KJ_Screen_stats`
- `KJ_Screen_airdrop`
- `KJ_Screen_admin`
- `KJ_Screen_toast`

## Global elements
- `KJ_Root`
- `KJ_Dim`
- `KJ_Brand`
- `KJ_Title`
- `KJ_Status`
- `KJ_Close`
- `KJ_ContractVersion`
- `KJ_Accent`

`KJ_Close` is handled by `Kanomjeen.Core.UiService`.

## Main menu buttons
- `KJ_Main_TPA` -> TPA plugin opens TPA screen
- `KJ_Main_Homes` -> Homes plugin opens Home list
- `KJ_Main_Kits` -> Kits plugin opens available kits
- `KJ_Main_Stats` -> Stats plugin opens the player's own stats
- `KJ_Main_Airdrop` -> Airdrop plugin opens active/standby state
- `KJ_Main_Admin` -> AdminAudit opens staff screen after server-side permission check

`KJ_Main_Admin` is hidden by Core for players without `kanomjeen.admin.inspect`, but server authorization is still mandatory.

## TPA
Container:
- `KJ_TPA_RequestPanel`

Fields:
- `KJ_TPA_Requester`
- `KJ_TPA_Timer`

Buttons:
- `KJ_TPA_Accept` -> accept latest matching incoming request
- `KJ_TPA_Deny` -> deny incoming request
- `KJ_TPA_Cancel` -> cancel player's outgoing request

When opening the TPA screen from Main without a request, `KJ_TPA_RequestPanel` is hidden. An incoming request shows it.

## Homes
Rows 0..5:
- `KJ_Home_Row_{n}`
- `KJ_Home_Name_{n}`
- `KJ_Home_Teleport_{n}`
- `KJ_Home_Delete_{n}`

Other:
- `KJ_Home_Add`

`KJ_Home_Add` currently instructs the player to use `/home set <name>` so text entry remains server-chat based and auditable. Teleport/delete resolve `{n}` again against the current HomeStore before action.

## Kits
Rows 0..7:
- `KJ_Kit_Row_{n}`
- `KJ_Kit_Name_{n}`
- `KJ_Kit_Cooldown_{n}`
- `KJ_Kit_Claim_{n}`

The plugin rebuilds the available kit list from permissions before resolving the clicked index.

## Stats
- `KJ_Stats_Kills`
- `KJ_Stats_Deaths`
- `KJ_Stats_KDR`
- `KJ_Stats_Zombies`
- `KJ_Stats_Headshots`
- `KJ_Stats_Playtime`
- `KJ_Stats_LongestLife`
- `KJ_Stats_Airdrops`

## Airdrop
- `KJ_Airdrop_Region`
- `KJ_Airdrop_Distance`
- `KJ_Airdrop_Timer`
- `KJ_Airdrop_State`

States currently used:
- `ACTIVE`
- `STANDBY`

## Admin
- `KJ_Admin_Name`
- `KJ_Admin_State`
- `KJ_Admin_God`
- `KJ_Admin_Vanish`

Targeted moderation stays command-driven in v1 (`/warn`, `/mute`, `/kick`, `/kjban`, `/kjunban`, `/inspect`) so target/reason strings are explicit and local audit entries remain clear.

## Toast
- `KJ_Toast_Title`
- `KJ_Toast_Body`
- `KJ_Toast` container

The prefab contains the toast surface for future short-lived notifications. v1 gameplay continues to use chat for most transient notices.

## Server -> UI API
Core wraps the legacy-compatible EffectManager calls used by the suite:
- send effect
- set text
- set visibility
- clear effect
- receive button click callback

## Versioning
Current contract: **1.0**.

Any future change that renames/removes a bindable element is a contract change. Update both:
1. this document + Unity builder
2. server plugin code/config contract version

During staged upgrades, interactive UI should be disabled/fallback to chat rather than knowingly running mismatched callback names.
