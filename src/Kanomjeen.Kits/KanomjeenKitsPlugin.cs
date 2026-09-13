using System;
using System.Collections.Generic;
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

namespace Kanomjeen.Kits
{
    public sealed class KanomjeenKitsPlugin : RocketPlugin<KitConfiguration>
    {
        private KanomjeenCorePlugin boundCore;
        private KanomjeenCorePlugin Core => KanomjeenCorePlugin.Instance;
        private Color MessageColor => UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor ?? "cyan", Color.cyan);

        protected override void Load()
        {
            Validate();
            if (!TryBindCore())
            {
                Logger.LogWarning("[Kanomjeen.Kits] Waiting for Kanomjeen.Core...");
                InvokeRepeating(nameof(BindCoreTick), 1f, 1f);
            }
            Logger.Log("[Kanomjeen.Kits] Loaded.");
        }

        protected override void Unload()
        {
            CancelInvoke();
            UnbindCore();
            Logger.Log("[Kanomjeen.Kits] Unloaded.");
        }

        [RocketCommand("kit", "Claim a configured kit", "<name>", AllowedCaller.Player)]
        public void CommandKit(IRocketPlayer caller, string[] args)
        {
            var player = caller as UnturnedPlayer; if (player == null) return;
            if (Core == null) { Say(player, "CoreUnavailable"); return; }
            if (!Rate(player, "kit.command")) return;
            if (args == null || args.Length == 0) { Say(player, "Usage"); return; }
            Claim(player, string.Join(" ", args).Trim());
        }

        [RocketCommand("kits", "List available kits", "", AllowedCaller.Player)]
        public void CommandKits(IRocketPlayer caller, string[] args)
        {
            var player = caller as UnturnedPlayer; if (player == null) return;
            if (Core == null) { Say(player, "CoreUnavailable"); return; }
            var visible = AvailableFor(player);
            if (visible.Count == 0) { Say(player, "None"); return; }
            Say(player, "Header");
            foreach (var kit in visible)
            {
                var remaining = Core.Cooldowns.GetRemaining(player.Id, "kit:" + kit.Name.ToLowerInvariant(), DateTime.UtcNow);
                Say(player, "Line", kit.Name, remaining > TimeSpan.Zero ? Math.Ceiling(remaining.TotalSeconds) + "s" : "ready");
            }
            ShowUi(player, visible);
        }

        private void Claim(UnturnedPlayer player, string name)
        {
            if (!Configuration.Instance.Enabled || Core == null) { Say(player, "Disabled"); return; }
            var kit = Find(name); if (kit == null) { Say(player, "NotFound", name); return; }
            if (!GameplayGuard.Has(player, kit.Permission)) { Say(player, "Locked"); return; }

            var failure = Core.Guard.CheckTeleport(player, ZoneFeature.Kit, player.Position, "kanomjeen.kit", Configuration.Instance.BlockInVehicle);
            if (failure != GuardFailure.None) { Say(player, "Blocked", Reason(failure)); return; }

            var key = "kit:" + kit.Name.ToLowerInvariant();
            var remaining = Core.Cooldowns.GetRemaining(player.Id, key, DateTime.UtcNow);
            if (remaining > TimeSpan.Zero && !GameplayGuard.Has(player, "kanomjeen.kit.bypass.cooldown")) { Say(player, "Cooldown", Math.Ceiling(remaining.TotalSeconds)); return; }

            Core.Cooldowns.Set(player.Id, key, DateTime.UtcNow, TimeSpan.FromSeconds(Math.Max(0f, kit.CooldownSeconds)));
            var allGiven = true;
            foreach (var item in kit.Items)
            {
                if (item == null || item.Id == 0 || item.Amount == 0) continue;
                if (!player.GiveItem(item.Id, item.Amount)) allGiven = false;
            }
            Say(player, allGiven ? "Claimed" : "Partial", kit.DisplayName ?? kit.Name);
            Core.Ui.Close(player);
        }

        private List<KitDefinition> AvailableFor(UnturnedPlayer player)
        {
            var result = new List<KitDefinition>();
            foreach (var kit in Configuration.Instance.Kits)
                if (kit != null && !string.IsNullOrWhiteSpace(kit.Name) && GameplayGuard.Has(player, kit.Permission)) result.Add(kit);
            return result;
        }

        private KitDefinition Find(string name)
        {
            foreach (var kit in Configuration.Instance.Kits)
                if (kit != null && string.Equals(kit.Name, name, StringComparison.OrdinalIgnoreCase)) return kit;
            return null;
        }

        private void ShowUi(UnturnedPlayer player, List<KitDefinition> kits)
        {
            if (Core?.Ui == null || !Core.Ui.IsConfigured) return;
            Core.Ui.Open(player, "kits", "KANOMJEEN • KITS", "Survival utility only — no pay-to-win loadouts.");
            for (var i = 0; i < 8; i++)
            {
                var visible = i < kits.Count;
                Core.Ui.SetVisible(player, "KJ_Kit_Row_" + i, visible);
                if (!visible) continue;
                var kit = kits[i];
                var remaining = Core.Cooldowns.GetRemaining(player.Id, "kit:" + kit.Name.ToLowerInvariant(), DateTime.UtcNow);
                Core.Ui.SetText(player, "KJ_Kit_Name_" + i, kit.DisplayName ?? kit.Name);
                Core.Ui.SetText(player, "KJ_Kit_Cooldown_" + i, remaining > TimeSpan.Zero ? Math.Ceiling(remaining.TotalSeconds) + "s" : "READY");
            }
        }

        private void OnUiButton(object sender, UiButtonEventArgs e)
        {
            if (e?.Player == null) return;
            if (e.Screen == "main" && e.Button == "KJ_Main_Kits") { ShowUi(e.Player, AvailableFor(e.Player)); return; }
            if (e.Screen != "kits" || !Rate(e.Player, "kit.ui", 0.5f)) return;
            const string prefix = "KJ_Kit_Claim_";
            if (!e.Button.StartsWith(prefix, StringComparison.Ordinal) || !int.TryParse(e.Button.Substring(prefix.Length), out var index)) return;
            var kits = AvailableFor(e.Player); if (index < 0 || index >= kits.Count) return;
            Claim(e.Player, kits[index].Name);
        }

        private bool TryBindCore()
        {
            var core = KanomjeenCorePlugin.Instance;
            if (core == null) return false;
            if (ReferenceEquals(boundCore, core)) return true;
            UnbindCore();
            boundCore = core;
            boundCore.Ui.ButtonClicked += OnUiButton;
            Logger.Log("[Kanomjeen.Kits] Bound to Kanomjeen.Core.");
            return true;
        }

        private void BindCoreTick() { if (TryBindCore()) CancelInvoke(nameof(BindCoreTick)); }

        private void UnbindCore()
        {
            if (boundCore?.Ui != null) boundCore.Ui.ButtonClicked -= OnUiButton;
            boundCore = null;
        }

        private bool Rate(UnturnedPlayer player, string action, float? seconds = null)
        {
            if (Core == null) return false;
            if (Core.RateLimiter.TryConsume(player.Id, action, DateTime.UtcNow, TimeSpan.FromSeconds(seconds ?? Core.Config.CommandRateLimitSeconds), out var left)) return true;
            Say(player, "RateLimit", Math.Ceiling(left.TotalSeconds)); return false;
        }

        private string Reason(GuardFailure f) { switch (f) { case GuardFailure.Combat: return "combat tag"; case GuardFailure.Raid: return "raid tag"; case GuardFailure.Vehicle: return "vehicle"; case GuardFailure.OriginZone: return "restricted zone"; default: return "current state"; } }
        private void Say(IRocketPlayer player, string key, params object[] args) { if (player != null) UnturnedChat.Say(player, Translate(key, args), MessageColor, false); }
        private void Validate() { if (Configuration.Instance.Kits == null) Configuration.Instance.LoadDefaults(); foreach (var k in Configuration.Instance.Kits) if (k != null && k.Items == null) k.Items = new List<KitItem>(); Configuration.Save(); }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "Usage", "Usage: /kit <name> or /kits" }, { "Disabled", "Kits are currently disabled." }, { "CoreUnavailable", "Kanomjeen.Core is not ready yet. Try again shortly." }, { "NotFound", "Kit '{0}' was not found." }, { "Locked", "You do not have permission for that kit." },
            { "Blocked", "Kit claim blocked: {0}." }, { "Cooldown", "Kit cooldown: {0}s remaining." }, { "Claimed", "Claimed {0}." }, { "Partial", "{0} was partially delivered because your inventory is full. Cooldown was applied to prevent duplication." },
            { "None", "No kits are available to you." }, { "Header", "Available kits:" }, { "Line", "/kit {0} — {1}" }, { "RateLimit", "Please wait {0}s before doing that again." }
        };
    }
}
