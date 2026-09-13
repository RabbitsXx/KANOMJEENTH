using System;
using Kanomjeen.Core.Services;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Commands;
using Logger = Rocket.Core.Logging.Logger;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Kanomjeen.VehicleGuard
{
    public sealed class KanomjeenVehicleGuardPlugin : RocketPlugin<VehicleGuardConfiguration>
    {
        private Color MessageColor => UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor ?? "cyan", Color.cyan);

        protected override void Load()
        {
            Validate();
            VehicleManager.onEnterVehicleRequested += OnEnterVehicle;
            VehicleManager.onDamageVehicleRequested += OnDamageVehicle;
            Logger.Log("[Kanomjeen.VehicleGuard] Loaded. Virtual garage is intentionally disabled.");
        }

        protected override void Unload()
        {
            VehicleManager.onEnterVehicleRequested -= OnEnterVehicle;
            VehicleManager.onDamageVehicleRequested -= OnDamageVehicle;
            Logger.Log("[Kanomjeen.VehicleGuard] Unloaded.");
        }

        [RocketCommand("vehicleinfo", "Inspect vehicle ownership and state", "", AllowedCaller.Player)]
        public void CommandVehicleInfo(IRocketPlayer caller, string[] args)
        {
            var player = caller as UnturnedPlayer; if (player == null) return;
            var vehicle = player.CurrentVehicle ?? RayVehicle(player, Configuration.Instance.InspectDistance);
            if (vehicle == null) { Say(player, "NoVehicle"); return; }
            Say(player, "Info", vehicle.asset.name, vehicle.isLocked, Id(vehicle.lockedOwner), Id(vehicle.lockedGroup), vehicle.health, vehicle.fuel);
        }

        [RocketCommand("kjvehicleinspect", "Admin vehicle inspection", "", AllowedCaller.Player)]
        public void CommandAdminInspect(IRocketPlayer caller, string[] args)
        {
            var player = caller as UnturnedPlayer; if (player == null || !GameplayGuard.Has(player, "kanomjeen.admin.vehicle.inspect")) { Say(player, "NoPermission"); return; }
            var vehicle = player.CurrentVehicle ?? RayVehicle(player, Math.Max(Configuration.Instance.InspectDistance, 30f));
            if (vehicle == null) { Say(player, "NoVehicle"); return; }
            Say(player, "AdminInfo", vehicle.asset.id, vehicle.asset.name, vehicle.isLocked, Id(vehicle.lockedOwner), Id(vehicle.lockedGroup), vehicle.health, vehicle.fuel, vehicle.transform.position.x, vehicle.transform.position.y, vehicle.transform.position.z);
        }

        private void OnEnterVehicle(Player nativePlayer, InteractableVehicle vehicle, ref bool shouldAllow)
        {
            if (!Configuration.Instance.Enabled || !Configuration.Instance.LogVehicleEntry || nativePlayer == null || vehicle == null) return;
            var player = UnturnedPlayer.FromPlayer(nativePlayer);
            if (player != null) Logger.Log("[Kanomjeen.VehicleGuard] ENTER " + player.Id + " -> vehicle " + vehicle.asset.id + " owner=" + Id(vehicle.lockedOwner) + " allowed=" + shouldAllow);
        }

        private void OnDamageVehicle(CSteamID instigatorSteamID, InteractableVehicle vehicle, ref ushort pendingTotalDamage, ref bool canRepair, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (!Configuration.Instance.Enabled || !Configuration.Instance.LogVehicleDamage || vehicle == null || pendingTotalDamage == 0) return;
            Logger.Log("[Kanomjeen.VehicleGuard] DAMAGE actor=" + Id(instigatorSteamID) + " vehicle=" + vehicle.asset.id + " owner=" + Id(vehicle.lockedOwner) + " amount=" + pendingTotalDamage + " origin=" + damageOrigin + " allowed=" + shouldAllow);
        }

        private static InteractableVehicle RayVehicle(UnturnedPlayer player, float distance)
        {
            if (player?.Player == null) return null;
            var ray = new Ray(player.Player.look.aim.position, player.Player.look.aim.forward);
            var info = DamageTool.raycast(ray, Math.Max(2f, distance), RayMasks.VEHICLE);
            return info.vehicle;
        }

        private static string Id(CSteamID id) => id == CSteamID.Nil ? "none" : id.m_SteamID.ToString();
        private void Say(IRocketPlayer player, string key, params object[] args) { if (player != null) UnturnedChat.Say(player, Translate(key, args), MessageColor, false); }
        private void Validate() { if (Configuration.Instance.InspectDistance < 2f) Configuration.Instance.InspectDistance = 12f; Configuration.Save(); }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "NoVehicle", "Look at a nearby vehicle or sit in one first." },
            { "NoPermission", "You do not have permission to inspect vehicles as staff." },
            { "Info", "{0} | Locked={1} | Owner={2} | Group={3} | HP={4} | Fuel={5}" },
            { "AdminInfo", "ID={0} {1} | Locked={2} Owner={3} Group={4} HP={5} Fuel={6} Pos=({7:0},{8:0},{9:0})" }
        };
    }
}
