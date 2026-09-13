# Kanomjeen Permission Matrix

All new permissions use the `kanomjeen.*` namespace.

## Suggested groups

### default
Recommended player permissions:
- `kanomjeen.waypoint.use`
- `kanomjeen.tpa.use`
- `kanomjeen.home.use`
- `kanomjeen.home.set`
- `kanomjeen.kit.starter`

Optional if you want `/tpahere` for everyone:
- `kanomjeen.tpa.here`

### vip
VIP should improve convenience without bypassing PvP/raid safety.

Possible additions:
- `kanomjeen.waypoint.limit.20`
- `kanomjeen.home.limit.3`
- custom additional kit permission for a non-P2W utility kit

Do **not** normally grant:
- `kanomjeen.tpa.bypass.combat`
- `kanomjeen.tpa.bypass.raid`
- `kanomjeen.home.bypass.combat`
- `kanomjeen.home.bypass.raid`
- unrestricted admin permissions

### moderator
Suggested:
- `kanomjeen.admin.warn`
- `kanomjeen.admin.mute`
- `kanomjeen.admin.kick`
- `kanomjeen.admin.inspect`
- `kanomjeen.admin.vehicle.inspect`

### admin
Suggested additionally:
- `kanomjeen.admin.ban`
- `kanomjeen.admin.airdrop`
- `kanomjeen.admin.restart`
- `kanomjeen.admin.announce`
- `kanomjeen.admin.god`
- `kanomjeen.admin.vanish`
- `kanomjeen.build.bypass`

Rocket admins are treated as bypass-capable by `GameplayGuard` where applicable.

## TPA
- `kanomjeen.tpa.use` — use normal TPA flow.
- `kanomjeen.tpa.here` — use `/tpahere`.
- `kanomjeen.tpa.bypass.cooldown` — bypass successful teleport cooldown.
- `kanomjeen.tpa.bypass.delay` — bypass warmup delay.
- `kanomjeen.tpa.bypass.combat` — bypass combat gate.
- `kanomjeen.tpa.bypass.raid` — bypass raid gate.
- `kanomjeen.tpa.bypass.zone` — bypass configured/dynamic zone gate.

Combat/raid/zone bypasses should be staff-only unless you intentionally want to weaken survival balance.

## Waypoints
- `kanomjeen.waypoint.use` — create, list, track, rename and delete personal waypoints.
- `kanomjeen.waypoint.limit.11` through `kanomjeen.waypoint.limit.30` — raises the saved waypoint limit.
- `kanomjeen.waypoint.staff` — view staff-visible/global markers.

The server-only deployment uses `FallbackNativeMarker`: one selected destination is sent to Unturned's native map marker. It does not provide a rotating client minimap or an M hotkey.

## Homes
- `kanomjeen.home.use` — list/use homes.
- `kanomjeen.home.set` — create/delete homes.
- `kanomjeen.home.limit.3` through `kanomjeen.home.limit.10` — higher home count. The plugin chooses the highest granted value.
- `kanomjeen.home.bypass.cooldown`
- `kanomjeen.home.bypass.delay`
- `kanomjeen.home.bypass.combat`
- `kanomjeen.home.bypass.raid`
- `kanomjeen.home.bypass.zone`

## Kits
Each kit has its own permission in XML configuration. Default:
- `kanomjeen.kit.starter`

Optional staff convenience:
- `kanomjeen.kit.bypass.cooldown`

`GameplayGuard` also recognizes feature bypass suffixes for combat/raid/zone if explicitly granted, but these should generally remain staff-only.

## BuildGuard
- `kanomjeen.build.bypass` — bypass Kanomjeen BuildGuard placement restrictions.

## Airdrops
- `kanomjeen.admin.airdrop` — force airdrops and save configured spawn locations.

## ServerManager
- `kanomjeen.admin.restart` — schedule/cancel safe restart.
- `kanomjeen.admin.announce` — staff broadcast.

## AdminAudit
- `kanomjeen.admin.warn`
- `kanomjeen.admin.mute`
- `kanomjeen.admin.kick`
- `kanomjeen.admin.ban`
- `kanomjeen.admin.inspect`
- `kanomjeen.admin.god`
- `kanomjeen.admin.vanish`

## VehicleGuard
- `kanomjeen.admin.vehicle.inspect`

## UI
`/menu` itself does not create authority. Feature buttons execute the same server-side permission checks as commands. The Staff card is hidden without `kanomjeen.admin.inspect`, but hiding is UX only, not security.

## Recommended policy
Convenience permissions may change limits/cooldowns conservatively. Avoid monetizing:
- combat bypass
- raid bypass
- zone/deadzone bypass
- end-game kits
- unlimited homes
- moderation/admin abilities

That policy keeps Kanomjeen semi-vanilla rather than pay-to-win.
