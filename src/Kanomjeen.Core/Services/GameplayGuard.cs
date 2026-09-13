using System;
using Rocket.API;
using Rocket.Unturned.Player;
using UnityEngine;

namespace Kanomjeen.Core.Services
{
    public enum GuardFailure
    {
        None,
        Dead,
        Vehicle,
        Combat,
        Raid,
        OriginZone,
        DestinationZone
    }

    public sealed class GameplayGuard
    {
        private readonly PlayerStateService states;
        private readonly ZoneService zones;

        public GameplayGuard(PlayerStateService states, ZoneService zones)
        {
            this.states = states;
            this.zones = zones;
        }

        public GuardFailure CheckTeleport(UnturnedPlayer player, ZoneFeature feature, Vector3 destination, string permissionPrefix, bool blockVehicle = true)
        {
            if (player == null || player.Player == null || player.Dead) return GuardFailure.Dead;
            if (blockVehicle && player.IsInVehicle) return GuardFailure.Vehicle;

            var now = DateTime.UtcNow;
            var state = states.GetOrCreate(player.Id);
            if (state.IsInCombat(now) && !Has(player, permissionPrefix + ".bypass.combat")) return GuardFailure.Combat;
            if (state.IsRaidTagged(now) && !Has(player, permissionPrefix + ".bypass.raid")) return GuardFailure.Raid;
            if (zones.IsBlocked(player.Position, feature) && !Has(player, permissionPrefix + ".bypass.zone")) return GuardFailure.OriginZone;
            if (zones.IsBlocked(destination, feature) && !Has(player, permissionPrefix + ".bypass.zone")) return GuardFailure.DestinationZone;
            return GuardFailure.None;
        }

        public static bool Has(UnturnedPlayer player, string permission)
        {
            if (player == null || string.IsNullOrEmpty(permission)) return false;
            if (player.IsAdmin) return true;
            try { return player.HasPermission(permission); }
            catch { return false; }
        }
    }
}
