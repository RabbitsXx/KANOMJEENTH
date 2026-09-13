using System;
using System.Collections.Generic;
using Kanomjeen.Core;
using Rocket.API.Collections;
using Logger = Rocket.Core.Logging.Logger;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Kanomjeen.Respawn
{
    public sealed class KanomjeenRespawnPlugin : RocketPlugin<RespawnConfiguration>
    {
        private sealed class Protection
        {
            public DateTime UntilUtc;
            public Vector3 StartPosition;
        }

        private readonly Dictionary<string, Protection> protectedPlayers = new Dictionary<string, Protection>(StringComparer.Ordinal);
        private KanomjeenCorePlugin Core => KanomjeenCorePlugin.Instance;
        private Color MessageColor => UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor ?? "cyan", Color.cyan);

        protected override void Load()
        {
            Validate();
            UnturnedPlayerEvents.OnPlayerRevive += OnPlayerRevive;
            DamageTool.playerDamaged += OnPlayerDamaged;
            U.Events.OnPlayerDisconnected += OnDisconnected;
            InvokeRepeating(nameof(Sweep), Math.Max(0.25f, Configuration.Instance.CheckIntervalSeconds), Math.Max(0.25f, Configuration.Instance.CheckIntervalSeconds));
            Logger.Log("[Kanomjeen.Respawn] Loaded.");
        }

        protected override void Unload()
        {
            CancelInvoke();
            UnturnedPlayerEvents.OnPlayerRevive -= OnPlayerRevive;
            DamageTool.playerDamaged -= OnPlayerDamaged;
            U.Events.OnPlayerDisconnected -= OnDisconnected;
            protectedPlayers.Clear();
            Logger.Log("[Kanomjeen.Respawn] Unloaded.");
        }

        private void OnPlayerRevive(UnturnedPlayer player, Vector3 position, byte angle)
        {
            if (!Configuration.Instance.Enabled || player?.Player == null) return;
            protectedPlayers[player.Id] = new Protection
            {
                UntilUtc = DateTime.UtcNow.AddSeconds(Math.Max(0f, Configuration.Instance.ProtectionSeconds)),
                StartPosition = position
            };
            Say(player, "Started", Math.Ceiling(Configuration.Instance.ProtectionSeconds));
        }

        private void OnPlayerDamaged(Player nativePlayer, ref EDeathCause cause, ref ELimb limb, ref CSteamID killerId,
            ref Vector3 direction, ref float damage, ref float times, ref bool canDamage)
        {
            if (!Configuration.Instance.Enabled || nativePlayer == null || !canDamage) return;
            var victim = UnturnedPlayer.FromPlayer(nativePlayer);
            if (victim != null && IsProtected(victim.Id)) canDamage = false;

            if (!Configuration.Instance.CancelOnAttack || killerId == CSteamID.Nil) return;
            var attacker = UnturnedPlayer.FromCSteamID(killerId);
            if (attacker != null && victim != null && attacker.Id != victim.Id && protectedPlayers.Remove(attacker.Id)) Say(attacker, "CancelledAttack");
        }

        private void Sweep()
        {
            if (protectedPlayers.Count == 0 || Core == null) return;
            var now = DateTime.UtcNow;
            var remove = new List<string>();
            foreach (var pair in protectedPlayers)
            {
                var player = Core?.Players.FindById(pair.Key);
                if (player == null || player.Player == null || player.Dead || pair.Value.UntilUtc <= now) { remove.Add(pair.Key); continue; }
                if (Configuration.Instance.CancelOnMovement && Vector3.Distance(pair.Value.StartPosition, player.Position) > Math.Max(0.1f, Configuration.Instance.MovementToleranceMeters))
                {
                    remove.Add(pair.Key); Say(player, "CancelledMove");
                }
            }
            foreach (var id in remove) protectedPlayers.Remove(id);
        }

        private bool IsProtected(string playerId)
        {
            if (!protectedPlayers.TryGetValue(playerId, out var protection)) return false;
            if (protection.UntilUtc > DateTime.UtcNow) return true;
            protectedPlayers.Remove(playerId); return false;
        }

        private void OnDisconnected(UnturnedPlayer player) { if (player != null) protectedPlayers.Remove(player.Id); }
        private void Say(UnturnedPlayer player, string key, params object[] args) { if (player != null) UnturnedChat.Say(player, Translate(key, args), MessageColor, false); }
        private void Validate() { var c = Configuration.Instance; if (c.ProtectionSeconds < 0f) c.ProtectionSeconds = 10f; if (c.MovementToleranceMeters < 0.1f) c.MovementToleranceMeters = 1.5f; if (c.CheckIntervalSeconds < 0.25f) c.CheckIntervalSeconds = 0.5f; Configuration.Save(); }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "Started", "Spawn protection active for up to {0}s. Moving or attacking ends it." },
            { "CancelledAttack", "Spawn protection ended because you attacked." },
            { "CancelledMove", "Spawn protection ended because you moved away from spawn." }
        };
    }
}
