using System;
using System.Collections;
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

namespace Kanomjeen.TPA
{
    public sealed class KanomjeenTpaPlugin : RocketPlugin<TpaConfiguration>
    {
        private readonly TpaRequestStore store = new TpaRequestStore();
        private readonly HashSet<string> damagedWarmups = new HashSet<string>(StringComparer.Ordinal);
        private KanomjeenCorePlugin boundCore;
        private KanomjeenCorePlugin Core => KanomjeenCorePlugin.Instance;
        private Color MessageColor => UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor ?? "cyan", Color.cyan);

        protected override void Load()
        {
            ValidateConfig();
            if (!TryBindCore())
            {
                Logger.LogWarning("[Kanomjeen.TPA] Waiting for Kanomjeen.Core...");
                InvokeRepeating(nameof(BindCoreTick), 1f, 1f);
            }
            InvokeRepeating(nameof(SweepExpired), 1f, 1f);
            Logger.Log("[Kanomjeen.TPA] Loaded.");
        }

        protected override void Unload()
        {
            CancelInvoke();
            StopAllCoroutines();
            UnbindCore();
            store.Clear();
            damagedWarmups.Clear();
            Logger.Log("[Kanomjeen.TPA] Unloaded.");
        }

        [RocketCommand("tpa", "Request teleport to a player", "<player>", AllowedCaller.Player)]
        public void CommandTpa(IRocketPlayer caller, string[] args) => CreateRequest(caller, args, TpaDirection.ToTarget);

        [RocketCommand("tpahere", "Ask a player to teleport to you", "<player>", AllowedCaller.Player)]
        public void CommandTpaHere(IRocketPlayer caller, string[] args)
        {
            if (!Configuration.Instance.AllowTpaHere) { Say(caller, "TpaHereDisabled"); return; }
            CreateRequest(caller, args, TpaDirection.ToRequester);
        }

        [RocketCommand("tpaccept", "Accept latest teleport request", "[player]", AllowedCaller.Player)]
        [RocketCommandAlias("tpyes")]
        public void CommandAccept(IRocketPlayer caller, string[] args)
        {
            var me = Player(caller); if (me == null || !RequirePermission(me, "kanomjeen.tpa.use")) return;
            if (!ConsumeRate(me, "tpa.accept")) return;
            var request = store.NewestIncoming(me.Id, Join(args));
            if (request == null) { Say(me, "NoRequest"); return; }
            store.Remove(request);

            var requester = Core?.Players.FindById(request.RequesterId);
            var target = Core?.Players.FindById(request.TargetId);
            if (requester == null || target == null) { Say(me, "RequestGone"); return; }

            var mover = request.Direction == TpaDirection.ToTarget ? requester : target;
            var destination = request.Direction == TpaDirection.ToTarget ? target : requester;
            if (!CanStartTeleport(mover, destination, out var failure)) { Say(mover, "BlockedReason", FailureText(failure)); return; }

            var cooldown = Core.Cooldowns.GetRemaining(mover.Id, "tpa", DateTime.UtcNow);
            if (cooldown > TimeSpan.Zero && !GameplayGuard.Has(mover, "kanomjeen.tpa.bypass.cooldown"))
            {
                Say(mover, "Cooldown", Math.Ceiling(cooldown.TotalSeconds));
                return;
            }

            if (!Core.Teleports.TryAcquire(mover.Id)) { Say(mover, "TeleportBusy"); return; }
            Say(requester, "Accepted", target.DisplayName);
            Say(target, "Accepted", requester.DisplayName);
            Core.Ui.Close(target);
            StartCoroutine(TeleportRoutine(mover, destination));
        }

        [RocketCommand("tpdeny", "Deny latest teleport request", "[player]", AllowedCaller.Player)]
        [RocketCommandAlias("tpno")]
        public void CommandDeny(IRocketPlayer caller, string[] args)
        {
            var me = Player(caller); if (me == null || !RequirePermission(me, "kanomjeen.tpa.use")) return;
            if (!ConsumeRate(me, "tpa.deny")) return;
            var request = store.NewestIncoming(me.Id, Join(args));
            if (request == null) { Say(me, "NoRequest"); return; }
            store.Remove(request);
            Say(Core.Players.FindById(request.RequesterId), "Denied", me.DisplayName);
            Say(me, "DeniedYou");
            Core.Ui.Close(me);
        }

        [RocketCommand("tpcancel", "Cancel outgoing teleport request", "", AllowedCaller.Player)]
        public void CommandCancel(IRocketPlayer caller, string[] args)
        {
            var me = Player(caller); if (me == null || !RequirePermission(me, "kanomjeen.tpa.use")) return;
            if (!ConsumeRate(me, "tpa.cancel")) return;
            var removed = store.RemoveOutgoing(me.Id);
            if (removed.Count == 0) { Say(me, "NoOutgoing"); return; }
            foreach (var request in removed)
            {
                var target = Core.Players.FindById(request.TargetId);
                if (target != null) { Say(target, "CancelledBy", me.DisplayName); Core.Ui.Close(target); }
            }
            Say(me, "Cancelled");
        }

        [RocketCommand("tpatoggle", "Toggle incoming teleport requests", "", AllowedCaller.Player)]
        public void CommandToggle(IRocketPlayer caller, string[] args)
        {
            var me = Player(caller); if (me == null || !RequirePermission(me, "kanomjeen.tpa.use")) return;
            var blocked = store.ToggleBlocked(me.Id);
            Say(me, blocked ? "ToggleOff" : "ToggleOn");
        }

        private void CreateRequest(IRocketPlayer caller, string[] args, TpaDirection direction)
        {
            if (!Configuration.Instance.Enabled || Core == null) { Say(caller, "Disabled"); return; }
            var me = Player(caller); if (me == null || !RequirePermission(me, direction == TpaDirection.ToRequester ? "kanomjeen.tpa.here" : "kanomjeen.tpa.use")) return;
            if (!ConsumeRate(me, "tpa.request", Configuration.Instance.RequestCommandCooldownSeconds)) return;

            var input = Join(args);
            if (string.IsNullOrWhiteSpace(input)) { Say(me, direction == TpaDirection.ToTarget ? "UsageTpa" : "UsageHere"); return; }
            var resolved = Core.Players.Resolve(input);
            if (resolved.Status == PlayerResolveStatus.NotFound) { Say(me, "NotFound", input); return; }
            if (resolved.Status == PlayerResolveStatus.Ambiguous) { Say(me, "Ambiguous", resolved.MatchCount); return; }
            var target = resolved.Player;
            if (target.Id == me.Id) { Say(me, "Self"); return; }
            if (store.IsBlocked(target.Id)) { Say(me, "TargetBlocked", target.DisplayName); return; }
            if (store.HasDuplicate(me.Id, target.Id)) { Say(me, "Duplicate"); return; }
            if (store.OutgoingCount(me.Id) >= Configuration.Instance.MaxOutgoingRequests) { Say(me, "OutgoingLimit"); return; }
            if (store.IncomingCount(target.Id) >= Configuration.Instance.MaxIncomingRequests) { Say(me, "IncomingLimit"); return; }

            if (direction == TpaDirection.ToTarget)
            {
                var cd = Core.Cooldowns.GetRemaining(me.Id, "tpa", DateTime.UtcNow);
                if (cd > TimeSpan.Zero && !GameplayGuard.Has(me, "kanomjeen.tpa.bypass.cooldown")) { Say(me, "Cooldown", Math.Ceiling(cd.TotalSeconds)); return; }
            }

            var timeout = Math.Max(5f, Configuration.Instance.RequestTimeoutSeconds);
            var request = new TpaRequest
            {
                RequesterId = me.Id,
                RequesterName = me.DisplayName,
                TargetId = target.Id,
                TargetName = target.DisplayName,
                Direction = direction,
                ExpiresUtc = DateTime.UtcNow.AddSeconds(timeout)
            };
            store.Add(request);
            Say(me, "Sent", target.DisplayName, Math.Round(timeout));
            Say(target, direction == TpaDirection.ToTarget ? "Received" : "ReceivedHere", me.DisplayName);
            ShowIncomingUi(target, request, timeout);
        }

        private IEnumerator TeleportRoutine(UnturnedPlayer mover, UnturnedPlayer destination)
        {
            var moverId = mover?.Id;
            try
            {
                if (string.IsNullOrEmpty(moverId)) yield break;
                damagedWarmups.Remove(moverId);
                var start = mover.Position;
                var delay = GameplayGuard.Has(mover, "kanomjeen.tpa.bypass.delay") ? 0f : Math.Max(0f, Configuration.Instance.TeleportDelaySeconds);
                var tick = Math.Max(0.1f, Configuration.Instance.WarmupCheckIntervalSeconds);
                var waited = 0f;
                if (delay > 0f) Say(mover, "Warmup", Math.Ceiling(delay));

                while (waited < delay)
                {
                    yield return new WaitForSeconds(tick);
                    waited += tick;
                    if (mover?.Player == null || destination?.Player == null) yield break;
                    if (mover.Dead) { Say(mover, "CancelledDeath"); yield break; }
                    if (Configuration.Instance.CancelOnDamage && damagedWarmups.Contains(moverId)) { Say(mover, "CancelledDamage"); yield break; }
                    if (Configuration.Instance.CancelOnMovement && Vector3.Distance(start, mover.Position) > Math.Max(0.1f, Configuration.Instance.MovementToleranceMeters)) { Say(mover, "CancelledMove"); yield break; }
                }

                if (mover?.Player == null || destination?.Player == null) yield break;
                if (!CanStartTeleport(mover, destination, out var failure)) { Say(mover, "BlockedReason", FailureText(failure)); yield break; }
                if (!mover.Player.teleportToLocation(destination.Position, destination.Rotation)) { Say(mover, "Failed"); yield break; }

                Core.Cooldowns.Set(moverId, "tpa", DateTime.UtcNow, TimeSpan.FromSeconds(Math.Max(0f, Configuration.Instance.TeleportCooldownSeconds)));
                Say(mover, "Success", destination.DisplayName);
                Say(destination, "Arrived", mover.DisplayName);
            }
            finally
            {
                if (!string.IsNullOrEmpty(moverId))
                {
                    damagedWarmups.Remove(moverId);
                    Core?.Teleports.Release(moverId);
                }
            }
        }

        private bool CanStartTeleport(UnturnedPlayer mover, UnturnedPlayer destination, out GuardFailure failure)
        {
            failure = Core.Guard.CheckTeleport(mover, ZoneFeature.TPA, destination.Position, "kanomjeen.tpa", Configuration.Instance.BlockInVehicle);
            if (failure != GuardFailure.None) return false;
            if (destination == null || destination.Player == null || destination.Dead) { failure = GuardFailure.Dead; return false; }
            var state = Core.PlayerStates.GetOrCreate(destination.Id);
            var now = DateTime.UtcNow;
            if (state.IsInCombat(now) && !GameplayGuard.Has(destination, "kanomjeen.tpa.bypass.combat")) { failure = GuardFailure.Combat; return false; }
            if (state.IsRaidTagged(now) && !GameplayGuard.Has(destination, "kanomjeen.tpa.bypass.raid")) { failure = GuardFailure.Raid; return false; }
            if (Core.Zones.IsBlocked(destination.Position, ZoneFeature.TPA) && !GameplayGuard.Has(destination, "kanomjeen.tpa.bypass.zone")) { failure = GuardFailure.DestinationZone; return false; }
            return true;
        }

        private void ShowIncomingUi(UnturnedPlayer target, TpaRequest request, float timeout)
        {
            if (Core?.Ui == null || !Core.Ui.IsConfigured) return;
            Core.Ui.Open(target, "tpa", "KANOMJEEN • TPA", request.Direction == TpaDirection.ToTarget ? "Teleport request" : "Teleport here request");
            Core.Ui.SetVisible(target, "KJ_TPA_RequestPanel", true);
            Core.Ui.SetText(target, "KJ_TPA_Requester", request.RequesterName);
            Core.Ui.SetText(target, "KJ_TPA_Timer", Math.Ceiling(timeout) + "s");
        }

        private void OnUiButton(object sender, UiButtonEventArgs e)
        {
            if (e == null || e.Player == null) return;
            if (e.Screen == "main" && e.Button == "KJ_Main_TPA")
            {
                Core.Ui.Open(e.Player, "tpa", "KANOMJEEN • TPA", "Use /tpa <player> or /tpahere <player>. Requests appear here.");
                Core.Ui.SetVisible(e.Player, "KJ_TPA_RequestPanel", false);
                return;
            }
            if (e.Screen != "tpa") return;
            if (!ConsumeRate(e.Player, "tpa.ui", 0.5f)) return;
            if (e.Button == "KJ_TPA_Accept") CommandAccept(e.Player, new string[0]);
            else if (e.Button == "KJ_TPA_Deny") CommandDeny(e.Player, new string[0]);
            else if (e.Button == "KJ_TPA_Cancel") CommandCancel(e.Player, new string[0]);
        }

        private bool TryBindCore()
        {
            var core = KanomjeenCorePlugin.Instance;
            if (core == null) return false;
            if (ReferenceEquals(boundCore, core)) return true;
            UnbindCore();
            boundCore = core;
            boundCore.PlayerDamaged += OnCorePlayerDamaged;
            boundCore.PlayerDisconnected += OnCorePlayerDisconnected;
            boundCore.Ui.ButtonClicked += OnUiButton;
            Logger.Log("[Kanomjeen.TPA] Bound to Kanomjeen.Core.");
            return true;
        }

        private void BindCoreTick()
        {
            if (TryBindCore()) CancelInvoke(nameof(BindCoreTick));
        }

        private void UnbindCore()
        {
            if (boundCore == null) return;
            boundCore.PlayerDamaged -= OnCorePlayerDamaged;
            boundCore.PlayerDisconnected -= OnCorePlayerDisconnected;
            if (boundCore.Ui != null) boundCore.Ui.ButtonClicked -= OnUiButton;
            boundCore = null;
        }

        private void OnCorePlayerDamaged(string playerId) { if (!string.IsNullOrEmpty(playerId)) damagedWarmups.Add(playerId); }
        private void OnCorePlayerDisconnected(string playerId)
        {
            store.RemoveTouching(playerId);
            damagedWarmups.Remove(playerId);
        }

        private void SweepExpired()
        {
            foreach (var request in store.Sweep(DateTime.UtcNow))
            {
                Say(Core?.Players.FindById(request.RequesterId), "Expired");
                var target = Core?.Players.FindById(request.TargetId);
                if (target != null) { Say(target, "Expired"); Core.Ui.Close(target); }
            }
        }

        private bool ConsumeRate(UnturnedPlayer player, string action, float? seconds = null)
        {
            if (Core == null || player == null) return false;
            if (Core.RateLimiter.TryConsume(player.Id, action, DateTime.UtcNow, TimeSpan.FromSeconds(seconds ?? Core.Config.CommandRateLimitSeconds), out var left)) return true;
            Say(player, "RateLimit", Math.Ceiling(left.TotalSeconds));
            return false;
        }

        private bool RequirePermission(UnturnedPlayer player, string permission)
        {
            if (GameplayGuard.Has(player, permission)) return true;
            Say(player, "NoPermission"); return false;
        }

        private static UnturnedPlayer Player(IRocketPlayer caller) => caller as UnturnedPlayer;
        private static string Join(string[] args) => args == null || args.Length == 0 ? null : string.Join(" ", args).Trim();

        private string FailureText(GuardFailure failure)
        {
            switch (failure)
            {
                case GuardFailure.Vehicle: return Translate("ReasonVehicle");
                case GuardFailure.Combat: return Translate("ReasonCombat");
                case GuardFailure.Raid: return Translate("ReasonRaid");
                case GuardFailure.OriginZone: return Translate("ReasonZone");
                case GuardFailure.DestinationZone: return Translate("ReasonDestinationZone");
                default: return Translate("ReasonUnavailable");
            }
        }

        private void Say(IRocketPlayer player, string key, params object[] args)
        {
            if (player != null) UnturnedChat.Say(player, Translate(key, args), MessageColor, false);
        }

        private void ValidateConfig()
        {
            var c = Configuration.Instance;
            if (c.RequestTimeoutSeconds < 5f) c.RequestTimeoutSeconds = 30f;
            if (c.RequestCommandCooldownSeconds < 0f) c.RequestCommandCooldownSeconds = 3f;
            if (c.TeleportCooldownSeconds < 0f) c.TeleportCooldownSeconds = 600f;
            if (c.TeleportDelaySeconds < 0f) c.TeleportDelaySeconds = 10f;
            if (c.MovementToleranceMeters < 0.1f) c.MovementToleranceMeters = 0.5f;
            if (c.WarmupCheckIntervalSeconds < 0.1f) c.WarmupCheckIntervalSeconds = 0.25f;
            if (c.MaxOutgoingRequests < 1) c.MaxOutgoingRequests = 1;
            if (c.MaxIncomingRequests < 1) c.MaxIncomingRequests = 3;
            Configuration.Save();
        }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "Disabled", "TPA is currently unavailable." },
            { "NoPermission", "You do not have permission for this action." },
            { "UsageTpa", "Usage: /tpa <player>" },
            { "UsageHere", "Usage: /tpahere <player>" },
            { "TpaHereDisabled", "/tpahere is disabled on this server." },
            { "NotFound", "Player '{0}' was not found." },
            { "Ambiguous", "That name matches {0} players. Type a more exact name." },
            { "Self", "You cannot send a TPA request to yourself." },
            { "TargetBlocked", "{0} is not accepting TPA requests." },
            { "Duplicate", "You already have a request pending for that player." },
            { "OutgoingLimit", "You already have the maximum number of outgoing requests." },
            { "IncomingLimit", "That player already has too many pending requests." },
            { "Sent", "TPA request sent to {0}. Expires in {1}s." },
            { "Received", "{0} wants to teleport to you. /tpaccept or /tpdeny" },
            { "ReceivedHere", "{0} wants you to teleport to them. /tpaccept or /tpdeny" },
            { "NoRequest", "You do not have a matching TPA request." },
            { "RequestGone", "That request is no longer valid." },
            { "Accepted", "TPA request accepted with {0}." },
            { "Denied", "{0} denied your TPA request." },
            { "DeniedYou", "TPA request denied." },
            { "Cancelled", "Outgoing TPA request cancelled." },
            { "CancelledBy", "{0} cancelled their TPA request." },
            { "NoOutgoing", "You have no outgoing TPA request." },
            { "ToggleOff", "Incoming TPA requests are now blocked." },
            { "ToggleOn", "Incoming TPA requests are now allowed." },
            { "Cooldown", "TPA cooldown: {0}s remaining." },
            { "RateLimit", "Please wait {0}s before doing that again." },
            { "TeleportBusy", "You already have a teleport warmup in progress." },
            { "Warmup", "Teleporting in {0}s. Do not move or take damage." },
            { "CancelledDamage", "Teleport cancelled because you took damage." },
            { "CancelledMove", "Teleport cancelled because you moved." },
            { "CancelledDeath", "Teleport cancelled because you died." },
            { "BlockedReason", "Teleport blocked: {0}" },
            { "Failed", "Teleport failed because the destination was not valid." },
            { "Success", "Teleported to {0}." },
            { "Arrived", "{0} teleported to you." },
            { "Expired", "TPA request expired." },
            { "ReasonVehicle", "leave your vehicle first" },
            { "ReasonCombat", "combat tag is active" },
            { "ReasonRaid", "raid tag is active" },
            { "ReasonZone", "teleporting is blocked in this area" },
            { "ReasonDestinationZone", "the destination blocks teleporting" },
            { "ReasonUnavailable", "player or destination is unavailable" }
        };
    }
}
