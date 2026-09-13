# Kanomjeen Commands

## Player / common
- `/menu` or `/kjmenu` — open Kanomjeen GUI.
- `/tpa <player>` — request teleport to a player.
- `/tpahere <player>` — invite a player to teleport to you.
- `/tpaccept [player]` / `/tpyes` — accept latest/matching TPA request.
- `/tpdeny [player]` / `/tpno` — deny latest/matching TPA request.
- `/tpcancel` — cancel outgoing TPA requests.
- `/tpatoggle` — toggle incoming TPA requests.
- `/home [name]` — teleport to a home (first home when name omitted).
- `/home set <name>` — create a home.
- `/home delete <name>` — delete a home.
- `/homes` — list homes and open Homes GUI.
- `/kit <name>` — claim a configured kit.
- `/kits` — list/open available kits.
- `/stats [player]` — show persistent statistics.
- `/whenairdrop` — show active/next airdrop status.
- `/serverstatus` — show uptime/player/restart status.
- `/vehicleinfo` — inspect the vehicle you occupy or look at.

## Airdrop administration
- `/setairdropspawn <name> <airdropId>` — store a spawn at the admin's current position.
- `/kjairdrop [spawn]` — force a configured airdrop.

## Server administration
- `/kjrestart [seconds]` — schedule safe save + shutdown countdown.
- `/kjcancelrestart` — cancel pending restart countdown.
- `/kjannounce <message>` — server announcement.

## Moderation / staff
- `/warn <player> <reason>`
- `/mute <player> <minutes> [reason]`
- `/unmute <player|steam64>`
- `/kick <player> [reason]`
- `/kjban <player|steam64> <minutes|0> [reason]`
- `/kjunban <steam64>`
- `/inspect <player>`
- `/kjgod [player]`
- `/kjvanish [player]`
- `/kjvehicleinspect`

## Important behavior
Commands do not bypass the same authority checks used by GUI buttons. Teleport/home/kit paths are gated by the configured permissions, rate limits, cooldowns, combat/raid state, vehicle state and restricted zones where applicable.

`/kjrestart` safely saves then shuts down the Unturned server process. The OS/service/container supervisor is responsible for starting the process again.

Do not run the old experimental `Tpa.dll` at the same time as `Kanomjeen.TPA.dll` because command names overlap.
