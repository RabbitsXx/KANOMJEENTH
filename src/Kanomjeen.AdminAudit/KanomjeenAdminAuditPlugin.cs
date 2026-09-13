using System;
using System.Collections.Generic;
using System.IO;
using Kanomjeen.Core;
using Kanomjeen.Core.Services;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Commands;
using Logger = Rocket.Core.Logging.Logger;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Kanomjeen.AdminAudit
{
    public sealed class KanomjeenAdminAuditPlugin : RocketPlugin<AdminAuditConfiguration>
    {
        private ModerationStore store;
        private KanomjeenCorePlugin boundCore;
        private readonly List<string> auditBuffer = new List<string>();
        private string auditDirectory;
        private KanomjeenCorePlugin Core => KanomjeenCorePlugin.Instance;
        private KanomjeenAdminAuditUi ui;
        private KanomjeenAdminAuditUi StaffUi => ui ?? (ui = new KanomjeenAdminAuditUi(this));
        internal UiService Ui => Core?.Ui;
        private Color MessageColor => UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor ?? "cyan", Color.cyan);

        protected override void Load()
        {
            Validate();
            var dir = Path.GetDirectoryName(typeof(KanomjeenAdminAuditPlugin).Assembly.Location) ?? ".";
            store = new ModerationStore(Path.Combine(dir, "Kanomjeen.Moderation.xml"));
            auditDirectory = Path.Combine(dir, "audit");
            U.Events.OnPlayerConnected += OnConnected;
            U.Events.OnPlayerDisconnected += OnDisconnected;
            ChatManager.onChatted += OnChatted;
            if (!TryBindCore()) InvokeRepeating(nameof(BindCoreTick), 1f, 1f);
            InvokeRepeating(nameof(Flush), Math.Max(5f, Configuration.Instance.AuditFlushSeconds), Math.Max(5f, Configuration.Instance.AuditFlushSeconds));
            Audit("plugin_load", "system", null, "Kanomjeen.AdminAudit loaded");
            Logger.Log("[Kanomjeen.AdminAudit] Loaded.");
        }

        protected override void Unload()
        {
            CancelInvoke();
            U.Events.OnPlayerConnected -= OnConnected;
            U.Events.OnPlayerDisconnected -= OnDisconnected;
            ChatManager.onChatted -= OnChatted;
            UnbindCore();
            Audit("plugin_unload", "system", null, "Kanomjeen.AdminAudit unloaded");
            Flush();
            store?.SaveIfDirty(); store = null;
            Logger.Log("[Kanomjeen.AdminAudit] Unloaded.");
        }

        [RocketCommand("warn", "Warn a player", "<player> <reason>", AllowedCaller.Both)]
        public void CommandWarn(IRocketPlayer caller, string[] args)
        {
            if (!Admin(caller, "kanomjeen.admin.warn")) { Say(caller, "NoPermission"); return; }
            if (!TargetWithReason(args, out var target, out var reason)) { Say(caller, "WarnUsage"); return; }
            var record = store.Get(target.Id, target.DisplayName); record.Warnings++; record.LastReason = reason; store.MarkDirty();
            Say(target, "Warned", record.Warnings, reason); Say(caller, "WarnSuccess", target.DisplayName, record.Warnings);
            Audit("warn", Actor(caller), target.Id, reason);
            if (Configuration.Instance.MaxWarningsBeforeAction > 0 && record.Warnings >= Configuration.Instance.MaxWarningsBeforeAction)
            {
                var duration = Configuration.Instance.WarningAutoBanSeconds;
                target.Ban(CSteamID.Nil, "Automatic moderation threshold: " + reason, duration);
                Audit("auto_ban", "system", target.Id, "warnings=" + record.Warnings + "; duration=" + duration);
            }
        }

        [RocketCommand("mute", "Mute a player", "<player> <minutes> [reason]", AllowedCaller.Both)]
        public void CommandMute(IRocketPlayer caller, string[] args)
        {
            if (!Admin(caller, "kanomjeen.admin.mute")) { Say(caller, "NoPermission"); return; }
            if (args == null || args.Length < 2 || !int.TryParse(args[1], out var minutes) || minutes < 1 || minutes > 43200) { Say(caller, "MuteUsage"); return; }
            var resolved = Resolve(args[0]); if (resolved == null) { Say(caller, "NotFound"); return; }
            var reason = args.Length > 2 ? string.Join(" ", args, 2, args.Length - 2) : "Muted by staff";
            var record = store.Get(resolved.Id, resolved.DisplayName);
            record.MutedUntilUtc = DateTime.UtcNow.AddMinutes(minutes); record.LastReason = reason; store.MarkDirty();
            Say(resolved, "Muted", minutes, reason); Say(caller, "MuteSuccess", resolved.DisplayName, minutes);
            Audit("mute", Actor(caller), resolved.Id, "minutes=" + minutes + "; " + reason);
        }

        [RocketCommand("unmute", "Remove a player's mute", "<player|steam64>", AllowedCaller.Both)]
        public void CommandUnmute(IRocketPlayer caller, string[] args)
        {
            if (!Admin(caller, "kanomjeen.admin.mute")) { Say(caller, "NoPermission"); return; }
            if (args == null || args.Length < 1) { Say(caller, "UnmuteUsage"); return; }
            var target = Resolve(args[0]);
            var id = target?.Id ?? (ulong.TryParse(args[0], out _) ? args[0] : null);
            if (id == null) { Say(caller, "NotFound"); return; }
            var record = store.Get(id, target?.DisplayName); record.MutedUntilUtc = DateTime.MinValue; store.MarkDirty();
            Say(target, "Unmuted"); Say(caller, "UnmuteSuccess", target?.DisplayName ?? id);
            Audit("unmute", Actor(caller), id, string.Empty);
        }

        [RocketCommand("kick", "Kick a player", "<player> [reason]", AllowedCaller.Both)]
        public void CommandKick(IRocketPlayer caller, string[] args)
        {
            if (!Admin(caller, "kanomjeen.admin.kick")) { Say(caller, "NoPermission"); return; }
            if (args == null || args.Length < 1) { Say(caller, "KickUsage"); return; }
            var target = Resolve(args[0]); if (target == null) { Say(caller, "NotFound"); return; }
            var reason = args.Length > 1 ? string.Join(" ", args, 1, args.Length - 1) : "Removed by staff";
            Audit("kick", Actor(caller), target.Id, reason); Say(caller, "KickSuccess", target.DisplayName); target.Kick(reason);
        }

        [RocketCommand("kjban", "Ban online player or Steam64", "<player|steam64> <minutes|0> [reason]", AllowedCaller.Both)]
        public void CommandBan(IRocketPlayer caller, string[] args)
        {
            if (!Admin(caller, "kanomjeen.admin.ban")) { Say(caller, "NoPermission"); return; }
            if (args == null || args.Length < 2 || !uint.TryParse(args[1], out var minutes)) { Say(caller, "BanUsage"); return; }
            var durationSeconds = minutes == 0 ? SteamBlacklist.PERMANENT : (minutes > uint.MaxValue / 60u ? uint.MaxValue : minutes * 60u);
            var reason = args.Length > 2 ? string.Join(" ", args, 2, args.Length - 2) : "Banned by staff";
            var target = Resolve(args[0]);
            if (target != null)
            {
                Audit("ban", Actor(caller), target.Id, "seconds=" + durationSeconds + "; " + reason);
                Say(caller, "BanSuccess", target.DisplayName); target.Ban(CSteamID.Nil, reason, durationSeconds); return;
            }
            if (!ulong.TryParse(args[0], out var steam64)) { Say(caller, "NotFound"); return; }
            SteamBlacklist.ban(new CSteamID(steam64), 0u, null, CSteamID.Nil, reason, durationSeconds);
            Audit("ban_offline", Actor(caller), args[0], "seconds=" + durationSeconds + "; " + reason); Say(caller, "BanSuccess", args[0]);
        }

        [RocketCommand("kjunban", "Unban Steam64", "<steam64>", AllowedCaller.Both)]
        public void CommandUnban(IRocketPlayer caller, string[] args)
        {
            if (!Admin(caller, "kanomjeen.admin.ban")) { Say(caller, "NoPermission"); return; }
            if (args == null || args.Length < 1 || !ulong.TryParse(args[0], out var steam64)) { Say(caller, "UnbanUsage"); return; }
            SteamBlacklist.unban(new CSteamID(steam64)); Audit("unban", Actor(caller), args[0], string.Empty); Say(caller, "UnbanSuccess", args[0]);
        }

        [RocketCommand("inspect", "Inspect an online player", "<player>", AllowedCaller.Both)]
        public void CommandInspect(IRocketPlayer caller, string[] args)
        {
            if (!Admin(caller, "kanomjeen.admin.inspect")) { Say(caller, "NoPermission"); return; }
            if (args == null || args.Length < 1) { Say(caller, "InspectUsage"); return; }
            var target = Resolve(args[0]); if (target == null) { Say(caller, "NotFound"); return; }
            var state = Core?.PlayerStates.GetOrCreate(target.Id);
            var now = DateTime.UtcNow;
            var combat = state != null && state.IsInCombat(now);
            var raid = state != null && state.IsRaidTagged(now);
            Say(caller, "Inspect", target.DisplayName, target.Id, target.Health, target.Ping, target.IsInVehicle, combat, raid,
                Math.Round(target.Position.x), Math.Round(target.Position.y), Math.Round(target.Position.z));
            Audit("inspect", Actor(caller), target.Id, string.Empty);
        }

        [RocketCommand("kjgod", "Toggle god mode", "[player]", AllowedCaller.Both)]
        public void CommandGod(IRocketPlayer caller, string[] args) => ToggleFeature(caller, args, true);

        [RocketCommand("kjvanish", "Toggle vanish mode", "[player]", AllowedCaller.Both)]
        public void CommandVanish(IRocketPlayer caller, string[] args) => ToggleFeature(caller, args, false);

        private void ToggleFeature(IRocketPlayer caller, string[] args, bool god)
        {
            if (!Admin(caller, god ? "kanomjeen.admin.god" : "kanomjeen.admin.vanish")) { Say(caller, "NoPermission"); return; }
            UnturnedPlayer target = null;
            if (args != null && args.Length > 0) target = Resolve(args[0]);
            else target = caller as UnturnedPlayer;
            if (target == null) { Say(caller, "NotFound"); return; }
            if (god) target.GodMode = !target.GodMode; else target.VanishMode = !target.VanishMode;
            var enabled = god ? target.GodMode : target.VanishMode;
            Say(caller, "Feature", god ? "god" : "vanish", target.DisplayName, enabled);
            Audit(god ? "god" : "vanish", Actor(caller), target.Id, "enabled=" + enabled);
        }

        private bool TryBindCore()
        {
            var core = KanomjeenCorePlugin.Instance;
            if (core == null) return false;
            if (ReferenceEquals(boundCore, core)) return true;
            UnbindCore();
            boundCore = core;
            boundCore.Ui.ButtonClicked += OnUiButton;
            CancelInvoke(nameof(BindCoreTick));
            Logger.Log("[Kanomjeen.AdminAudit] Bound to Kanomjeen.Core.");
            return true;
        }

        private void BindCoreTick() { TryBindCore(); }

        private void UnbindCore()
        {
            if (boundCore?.Ui != null) boundCore.Ui.ButtonClicked -= OnUiButton;
            boundCore = null;
        }

        private void OnUiButton(object sender, UiButtonEventArgs e)
        {
            if (e?.Player == null) return;
            StaffUi.OnButton(e.Player, e.Screen, e.Button);
        }

        // Surface used by KanomjeenAdminAuditUi: permission gate, staff snapshot and the toggles it routes to.
        internal void ShowStaffUi(UnturnedPlayer player)
        {
            if (player?.Player == null || !AllowStaffUi(player)) return;
            StaffUi.Show(player);
        }

        internal bool AllowStaffUi(UnturnedPlayer player) => GameplayGuard.Has(player, "kanomjeen.admin.inspect");
        internal string AdminStateText(UnturnedPlayer player) => player == null ? string.Empty : "God=" + player.GodMode + "  Vanish=" + player.VanishMode;
        internal void ToggleGod(UnturnedPlayer player) => CommandGod(player, new string[0]);
        internal void ToggleVanish(UnturnedPlayer player) => CommandVanish(player, new string[0]);

        private void OnChatted(SteamPlayer steamPlayer, EChatMode mode, ref Color color, ref bool isRich, string message, ref bool isVisible)
        {
            if (!Configuration.Instance.Enabled || steamPlayer == null) return;
            var player = UnturnedPlayer.FromSteamPlayer(steamPlayer); if (player == null) return;
            Audit("chat", player.Id, null, "mode=" + mode + "; message=" + message);
            if (!store.IsMuted(player.Id, DateTime.UtcNow, out var remaining)) return;
            isVisible = false;
            Say(player, "MuteRemaining", Math.Ceiling(remaining.TotalSeconds));
        }

        private void OnConnected(UnturnedPlayer player) { if (player != null) { store.Get(player.Id, player.DisplayName); Audit("join", player.Id, null, player.DisplayName); } }
        private void OnDisconnected(UnturnedPlayer player) { if (player != null) Audit("leave", player.Id, null, player.DisplayName); }

        private UnturnedPlayer Resolve(string input)
        {
            var result = Core?.Players.Resolve(input);
            return result != null && result.Status == PlayerResolveStatus.Found ? result.Player : null;
        }

        private bool TargetWithReason(string[] args, out UnturnedPlayer target, out string reason)
        {
            target = null; reason = null;
            if (args == null || args.Length < 2) return false;
            target = Resolve(args[0]); if (target == null) return false;
            reason = string.Join(" ", args, 1, args.Length - 1).Trim(); return !string.IsNullOrWhiteSpace(reason);
        }

        private bool Admin(IRocketPlayer caller, string permission)
        {
            if (caller == null) return true;
            var player = caller as UnturnedPlayer;
            return player != null && GameplayGuard.Has(player, permission);
        }

        private static string Actor(IRocketPlayer caller) => caller?.Id ?? "console";

        private void Audit(string action, string actor, string target, string detail)
        {
            lock (auditBuffer)
            {
                auditBuffer.Add("{\"utc\":\"" + DateTime.UtcNow.ToString("O") + "\",\"action\":\"" + Json(action) + "\",\"actor\":\"" + Json(actor) + "\",\"target\":\"" + Json(target) + "\",\"detail\":\"" + Json(detail) + "\"}");
            }
        }

        private void Flush()
        {
            List<string> lines;
            lock (auditBuffer)
            {
                if (auditBuffer.Count == 0) { store?.SaveIfDirty(); return; }
                lines = new List<string>(auditBuffer); auditBuffer.Clear();
            }
            try
            {
                System.IO.Directory.CreateDirectory(auditDirectory);
                File.AppendAllLines(Path.Combine(auditDirectory, "audit-" + DateTime.UtcNow.ToString("yyyy-MM") + ".jsonl"), lines);
            }
            catch (Exception ex)
            {
                Logger.LogError("[Kanomjeen.AdminAudit] Audit flush failed: " + ex.Message);
                lock (auditBuffer) auditBuffer.InsertRange(0, lines);
            }
            store?.SaveIfDirty();
        }

        private static string Json(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private void Say(IRocketPlayer player, string key, params object[] args)
        {
            var text = Translate(key, args);
            if (player == null) Logger.Log(text); else UnturnedChat.Say(player, text, MessageColor, false);
        }
        private void Validate() { if (Configuration.Instance.AuditFlushSeconds < 5f) Configuration.Instance.AuditFlushSeconds = 10f; Configuration.Save(); }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "NoPermission", "You do not have permission for that moderation action." }, { "NotFound", "Player was not found or the name is ambiguous." },
            { "WarnUsage", "Usage: /warn <player> <reason>" }, { "Warned", "Warning #{0}: {1}" }, { "WarnSuccess", "Warned {0}. Total warnings: {1}." },
            { "MuteUsage", "Usage: /mute <player> <minutes> [reason]" }, { "Muted", "You are muted for {0} minutes. Reason: {1}" }, { "MuteSuccess", "Muted {0} for {1} minutes." },
            { "UnmuteUsage", "Usage: /unmute <player|steam64>" }, { "Unmuted", "Your mute has been removed." }, { "UnmuteSuccess", "Unmuted {0}." }, { "MuteRemaining", "You are muted for another {0}s." },
            { "KickUsage", "Usage: /kick <player> [reason]" }, { "KickSuccess", "Kicked {0}." },
            { "BanUsage", "Usage: /kjban <player|steam64> <minutes|0 permanent> [reason]" }, { "BanSuccess", "Banned {0}." }, { "UnbanUsage", "Usage: /kjunban <steam64>" }, { "UnbanSuccess", "Unbanned {0}." },
            { "InspectUsage", "Usage: /inspect <player>" }, { "Inspect", "{0} [{1}] HP={2} Ping={3} Vehicle={4} Combat={5} Raid={6} Pos=({7},{8},{9})" },
            { "Feature", "{0} for {1}: {2}" }
        };
    }
}
