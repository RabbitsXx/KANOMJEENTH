using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Kanomjeen.Core;
using Kanomjeen.Core.Services;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Commands;
using Logger = Rocket.Core.Logging.Logger;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using UnityEngine;

namespace Kanomjeen.Homes
{
    public sealed class KanomjeenHomesPlugin : RocketPlugin<HomeConfiguration>
    {
        private HomeStore store;
        private readonly HashSet<string> damagedWarmups = new HashSet<string>(StringComparer.Ordinal);
        private KanomjeenCorePlugin boundCore;
        private KanomjeenCorePlugin Core => KanomjeenCorePlugin.Instance;
        private Color MessageColor => UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor ?? "cyan", Color.cyan);

        protected override void Load()
        {
            ValidateConfig();
            var dir = Path.GetDirectoryName(typeof(KanomjeenHomesPlugin).Assembly.Location) ?? ".";
            store = new HomeStore(Path.Combine(dir, "Kanomjeen.Homes.xml"));
            if (!TryBindCore())
            {
                Logger.LogWarning("[Kanomjeen.Homes] Waiting for Kanomjeen.Core...");
                InvokeRepeating(nameof(BindCoreTick), 1f, 1f);
            }
            var save = Math.Max(10f, Configuration.Instance.SaveIntervalSeconds);
            InvokeRepeating(nameof(SaveStore), save, save);
            Logger.Log("[Kanomjeen.Homes] Loaded.");
        }

        protected override void Unload()
        {
            CancelInvoke(); StopAllCoroutines();
            UnbindCore();
            store?.SaveIfDirty(); store = null; damagedWarmups.Clear();
            Logger.Log("[Kanomjeen.Homes] Unloaded.");
        }

        [RocketCommand("home", "Teleport to or manage homes", "[name] | set <name> | delete <name>", AllowedCaller.Player)]
        public void CommandHome(IRocketPlayer caller, string[] args)
        {
            var player = caller as UnturnedPlayer; if (player == null) return;
            if (Core == null) { Say(player, "CoreUnavailable"); return; }
            if (!Require(player, "kanomjeen.home.use")) return;
            if (!Rate(player, "home.command")) return;
            if (args != null && args.Length > 0 && string.Equals(args[0], "set", StringComparison.OrdinalIgnoreCase)) { SetHome(player, Tail(args)); return; }
            if (args != null && args.Length > 0 && (string.Equals(args[0], "delete", StringComparison.OrdinalIgnoreCase) || string.Equals(args[0], "del", StringComparison.OrdinalIgnoreCase))) { DeleteHome(player, Tail(args)); return; }
            TeleportHome(player, args == null || args.Length == 0 ? null : string.Join(" ", args));
        }

        [RocketCommand("homes", "List your homes", "", AllowedCaller.Player)]
        public void CommandHomes(IRocketPlayer caller, string[] args)
        {
            var player = caller as UnturnedPlayer; if (player == null || !Require(player, "kanomjeen.home.use")) return;
            var homes = store.Get(player.Id);
            if (homes.Count == 0) { Say(player, "NoHomes"); return; }
            Say(player, "HomesHeader", homes.Count, EffectiveLimit(player));
            for (var i = 0; i < homes.Count; i++) Say(player, "HomeLine", i + 1, homes[i].Name);
            ShowHomesUi(player);
        }

        private void SetHome(UnturnedPlayer player, string name)
        {
            if (!Require(player, "kanomjeen.home.set")) return;
            if (string.IsNullOrWhiteSpace(name) || name.Length > 24) { Say(player, "UsageSet"); return; }
            var failure = Core.Guard.CheckTeleport(player, ZoneFeature.Home, player.Position, "kanomjeen.home", Configuration.Instance.BlockInVehicle);
            if (failure != GuardFailure.None) { Say(player, "Blocked", Reason(failure)); return; }
            var record = new HomeRecord { Name = name.Trim(), X = player.Position.x, Y = player.Position.y, Z = player.Position.z, Rotation = player.Rotation, CreatedUtc = DateTime.UtcNow };
            if (!store.Add(player.Id, record, EffectiveLimit(player), out var error)) { Say(player, error == "duplicate" ? "Duplicate" : "Limit", EffectiveLimit(player)); return; }
            Say(player, "Created", record.Name);
        }

        private void DeleteHome(UnturnedPlayer player, string name)
        {
            if (!Require(player, "kanomjeen.home.set")) return;
            if (string.IsNullOrWhiteSpace(name)) { Say(player, "UsageDelete"); return; }
            if (!store.Delete(player.Id, name.Trim())) { Say(player, "NotFound", name); return; }
            Say(player, "Deleted", name.Trim());
        }

        private void TeleportHome(UnturnedPlayer player, string name)
        {
            var home = store.Find(player.Id, name);
            if (home == null) { Say(player, "NotFound", name ?? "default"); return; }
            var destination = new Vector3(home.X, home.Y, home.Z);
            var failure = Core.Guard.CheckTeleport(player, ZoneFeature.Home, destination, "kanomjeen.home", Configuration.Instance.BlockInVehicle);
            if (failure != GuardFailure.None) { Say(player, "Blocked", Reason(failure)); return; }
            var remaining = Core.Cooldowns.GetRemaining(player.Id, "home", DateTime.UtcNow);
            if (remaining > TimeSpan.Zero && !GameplayGuard.Has(player, "kanomjeen.home.bypass.cooldown")) { Say(player, "Cooldown", Math.Ceiling(remaining.TotalSeconds)); return; }
            if (!Core.Teleports.TryAcquire(player.Id)) { Say(player, "TeleportBusy"); return; }
            StartCoroutine(TeleportRoutine(player, home));
        }

        private IEnumerator TeleportRoutine(UnturnedPlayer player, HomeRecord home)
        {
            var playerId = player?.Id;
            try
            {
                if (string.IsNullOrEmpty(playerId)) yield break;
                damagedWarmups.Remove(playerId);
                var start = player.Position;
                var delay = GameplayGuard.Has(player, "kanomjeen.home.bypass.delay") ? 0f : Math.Max(0f, Configuration.Instance.TeleportDelaySeconds);
                var tick = Math.Max(0.1f, Configuration.Instance.WarmupCheckIntervalSeconds);
                var waited = 0f;
                if (delay > 0f) Say(player, "Warmup", home.Name, Math.Ceiling(delay));
                while (waited < delay)
                {
                    yield return new WaitForSeconds(tick); waited += tick;
                    if (player?.Player == null) yield break;
                    if (player.Dead) { Say(player, "CancelDeath"); yield break; }
                    if (Configuration.Instance.CancelOnDamage && damagedWarmups.Contains(playerId)) { Say(player, "CancelDamage"); yield break; }
                    if (Configuration.Instance.CancelOnMovement && Vector3.Distance(start, player.Position) > Math.Max(0.1f, Configuration.Instance.MovementToleranceMeters)) { Say(player, "CancelMove"); yield break; }
                }
                var destination = new Vector3(home.X, home.Y, home.Z);
                var failure = Core.Guard.CheckTeleport(player, ZoneFeature.Home, destination, "kanomjeen.home", Configuration.Instance.BlockInVehicle);
                if (failure != GuardFailure.None) { Say(player, "Blocked", Reason(failure)); yield break; }
                if (!player.Player.teleportToLocation(destination, home.Rotation)) { Say(player, "Failed"); yield break; }
                Core.Cooldowns.Set(playerId, "home", DateTime.UtcNow, TimeSpan.FromSeconds(Configuration.Instance.TeleportCooldownSeconds));
                Say(player, "Teleported", home.Name); Core.Ui.Close(player);
            }
            finally
            {
                if (!string.IsNullOrEmpty(playerId))
                {
                    damagedWarmups.Remove(playerId);
                    Core?.Teleports.Release(playerId);
                }
            }
        }

        private void ShowHomesUi(UnturnedPlayer player)
        {
            if (Core?.Ui == null || !Core.Ui.IsConfigured) return;
            Core.Ui.Open(player, "homes", "KANOMJEEN • HOMES", "Travel is disabled during combat, raids and restricted objectives.");
            var homes = store.Get(player.Id);
            for (var i = 0; i < 6; i++)
            {
                var visible = i < homes.Count;
                Core.Ui.SetVisible(player, "KJ_Home_Row_" + i, visible);
                if (visible) Core.Ui.SetText(player, "KJ_Home_Name_" + i, homes[i].Name);
            }
        }

        private void OnUiButton(object sender, UiButtonEventArgs e)
        {
            if (e?.Player == null) return;
            if (e.Screen == "main" && e.Button == "KJ_Main_Homes") { ShowHomesUi(e.Player); return; }
            if (e.Screen != "homes" || !Rate(e.Player, "home.ui", 0.5f)) return;
            if (TryIndex(e.Button, "KJ_Home_Teleport_", out var index))
            {
                var homes = store.Get(e.Player.Id); if (index >= 0 && index < homes.Count) TeleportHome(e.Player, homes[index].Name); return;
            }
            if (TryIndex(e.Button, "KJ_Home_Delete_", out index))
            {
                var homes = store.Get(e.Player.Id); if (index >= 0 && index < homes.Count) { DeleteHome(e.Player, homes[index].Name); ShowHomesUi(e.Player); } return;
            }
            if (e.Button == "KJ_Home_Add") Say(e.Player, "UiAddHint");
        }

        private int EffectiveLimit(UnturnedPlayer player)
        {
            for (var n = 10; n > Configuration.Instance.MaxHomes; n--) if (GameplayGuard.Has(player, "kanomjeen.home.limit." + n)) return n;
            return Math.Max(1, Configuration.Instance.MaxHomes);
        }

        private bool Rate(UnturnedPlayer player, string action, float? seconds = null)
        {
            if (Core.RateLimiter.TryConsume(player.Id, action, DateTime.UtcNow, TimeSpan.FromSeconds(seconds ?? Core.Config.CommandRateLimitSeconds), out var left)) return true;
            Say(player, "RateLimit", Math.Ceiling(left.TotalSeconds)); return false;
        }

        private bool TryBindCore()
        {
            var core = KanomjeenCorePlugin.Instance;
            if (core == null) return false;
            if (ReferenceEquals(boundCore, core)) return true;
            UnbindCore();
            boundCore = core;
            boundCore.PlayerDamaged += OnDamaged;
            boundCore.PlayerDisconnected += OnDisconnected;
            boundCore.Ui.ButtonClicked += OnUiButton;
            Logger.Log("[Kanomjeen.Homes] Bound to Kanomjeen.Core.");
            return true;
        }

        private void BindCoreTick() { if (TryBindCore()) CancelInvoke(nameof(BindCoreTick)); }

        private void UnbindCore()
        {
            if (boundCore == null) return;
            boundCore.PlayerDamaged -= OnDamaged;
            boundCore.PlayerDisconnected -= OnDisconnected;
            if (boundCore.Ui != null) boundCore.Ui.ButtonClicked -= OnUiButton;
            boundCore = null;
        }

        private bool Require(UnturnedPlayer player, string permission) { if (GameplayGuard.Has(player, permission)) return true; Say(player, "NoPermission"); return false; }
        private void OnDamaged(string playerId) { if (!string.IsNullOrEmpty(playerId)) damagedWarmups.Add(playerId); }
        private void OnDisconnected(string playerId) { if (!string.IsNullOrEmpty(playerId)) damagedWarmups.Remove(playerId); }
        private void SaveStore() => store?.SaveIfDirty();
        private static string Tail(string[] args) => args == null || args.Length < 2 ? null : string.Join(" ", args, 1, args.Length - 1).Trim();
        private static bool TryIndex(string value, string prefix, out int index) { index = -1; return value != null && value.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(value.Substring(prefix.Length), out index); }

        private string Reason(GuardFailure failure)
        {
            switch (failure) { case GuardFailure.Combat: return "combat tag"; case GuardFailure.Raid: return "raid tag"; case GuardFailure.Vehicle: return "vehicle"; case GuardFailure.OriginZone: return "restricted origin"; case GuardFailure.DestinationZone: return "restricted destination"; default: return "unavailable state"; }
        }

        private void Say(IRocketPlayer player, string key, params object[] args) { if (player != null) UnturnedChat.Say(player, Translate(key, args), MessageColor, false); }
        private void ValidateConfig()
        {
            var c = Configuration.Instance; if (c.MaxHomes < 1) c.MaxHomes = 2; if (c.TeleportCooldownSeconds < 0f) c.TeleportCooldownSeconds = 900f; if (c.TeleportDelaySeconds < 0f) c.TeleportDelaySeconds = 10f; if (c.MovementToleranceMeters < 0.1f) c.MovementToleranceMeters = 0.5f; if (c.WarmupCheckIntervalSeconds < 0.1f) c.WarmupCheckIntervalSeconds = 0.25f; if (c.SaveIntervalSeconds < 10f) c.SaveIntervalSeconds = 30f; Configuration.Save();
        }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "NoPermission", "You do not have permission for that home action." }, { "NoHomes", "You have no homes. Use /home set <name>." }, { "HomesHeader", "Homes: {0}/{1}" }, { "HomeLine", "{0}. {1}" },
            { "UsageSet", "Usage: /home set <name> (max 24 characters)" }, { "UsageDelete", "Usage: /home delete <name>" }, { "Created", "Home '{0}' created." }, { "Deleted", "Home '{0}' deleted." },
            { "Duplicate", "A home with that name already exists." }, { "Limit", "You reached your home limit ({0})." }, { "NotFound", "Home '{0}' was not found." }, { "Blocked", "Home action blocked: {0}." },
            { "Cooldown", "Home cooldown: {0}s remaining." }, { "TeleportBusy", "You already have a teleport warmup in progress." }, { "CoreUnavailable", "Kanomjeen.Core is not ready yet. Try again shortly." }, { "Warmup", "Teleporting to '{0}' in {1}s. Do not move or take damage." }, { "CancelDamage", "Home teleport cancelled because you took damage." },
            { "CancelMove", "Home teleport cancelled because you moved." }, { "CancelDeath", "Home teleport cancelled because you died." }, { "Failed", "Home teleport failed." }, { "Teleported", "Teleported to home '{0}'." },
            { "RateLimit", "Please wait {0}s before doing that again." }, { "UiAddHint", "Create a home with /home set <name>." }
        };
    }
}
