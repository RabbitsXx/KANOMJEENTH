using System;
using System.Collections.Generic;
using System.IO;
using Kanomjeen.Core.Configuration;
using Kanomjeen.Core.Services;
using Kanomjeen.Core.Waypoints;
using Rocket.API;
using Rocket.Core.Commands;
using Logger = Rocket.Core.Logging.Logger;
using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Kanomjeen.Core
{
    public sealed class KanomjeenCorePlugin : RocketPlugin<KanomjeenCoreConfiguration>
    {
        public static KanomjeenCorePlugin Instance { get; private set; }

        private WaypointUi waypointUi;
        private WaypointUi WaypointsUi => waypointUi ?? (waypointUi = new WaypointUi(this));

        public KanomjeenCoreConfiguration Config => Configuration.Instance;
        public PlayerStateService PlayerStates { get; private set; }
        public CommandRateLimiter RateLimiter { get; private set; }
        public PersistentCooldownService Cooldowns { get; private set; }
        public TeleportLockService Teleports { get; private set; }
        public ZoneService Zones { get; private set; }
        public GameplayGuard Guard { get; private set; }
        public PlayerResolver Players { get; private set; }
        public UiService Ui { get; private set; }
        public WaypointService Waypoints { get; private set; }

        public event Action<string> PlayerDamaged;
        public event Action<string> PlayerDisconnected;
        public event Action<string> RaidTriggered;

        protected override void Load()
        {
            Instance = this;
            ValidateConfiguration();

            PlayerStates = new PlayerStateService();
            RateLimiter = new CommandRateLimiter();
            Teleports = new TeleportLockService();
            Players = new PlayerResolver();
            Zones = new ZoneService(() => Configuration.Instance.Zones);
            Guard = new GameplayGuard(PlayerStates, Zones);

            var baseDir = Path.GetDirectoryName(typeof(KanomjeenCorePlugin).Assembly.Location) ?? ".";
            Cooldowns = new PersistentCooldownService(Path.Combine(baseDir, "Kanomjeen.Core.cooldowns.xml"));
            if (Configuration.Instance.EnableWaypoints)
                Waypoints = new WaypointService(Path.Combine(baseDir, "Kanomjeen.Core.waypoints.xml"), () => Configuration.Instance);
            Ui = new UiService(Configuration.Instance.EnableUi ? Configuration.Instance.UiEffectId : (ushort)0, Configuration.Instance.UiKey, Configuration.Instance.UiContractVersion);
            Ui.Subscribe();
            Ui.ButtonClicked += OnUiButton;

            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            U.Events.OnPlayerConnected += OnPlayerConnected;
            DamageTool.playerDamaged += OnPlayerDamaged;
            BarricadeManager.onDamageBarricadeRequested += OnBarricadeDamage;
            StructureManager.onDamageStructureRequested += OnStructureDamage;

            var flush = Math.Max(10f, Configuration.Instance.PersistenceFlushSeconds);
            InvokeRepeating(nameof(FlushPersistence), flush, flush);
            if (Waypoints != null) InvokeRepeating(nameof(SweepWaypoints), 5f, 5f);
            Logger.Log("[Kanomjeen.Core] Waypoints enabled in " + Configuration.Instance.WaypointMode + " mode. Native Unturned map markers are used; no client minimap module is installed.");
        }

        protected override void Unload()
        {
            CancelInvoke();
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            DamageTool.playerDamaged -= OnPlayerDamaged;
            BarricadeManager.onDamageBarricadeRequested -= OnBarricadeDamage;
            StructureManager.onDamageStructureRequested -= OnStructureDamage;

            // Clear the client UI before tearing down the service. Without this, a
            // Rocket/plugin reload can leave the old Effect instance on connected
            // clients and the next /menu would stack another copy on top of it.
            if (Ui != null)
            {
                foreach (var steamPlayer in Provider.clients)
                {
                    var player = UnturnedPlayer.FromSteamPlayer(steamPlayer);
                    if (player?.Player != null) Ui.Close(player);
                }
                Ui.Unsubscribe();
                Ui.ButtonClicked -= OnUiButton;
            }

            Cooldowns?.SaveIfDirty();
            Waypoints?.Save();

            RateLimiter?.Clear();
            Teleports?.Clear();
            PlayerStates?.Clear();
            Ui = null;
            Waypoints = null;
            Cooldowns = null;
            Teleports = null;
            Guard = null;
            Zones = null;
            Players = null;
            RateLimiter = null;
            PlayerStates = null;
            Instance = null;
            Logger.Log("[Kanomjeen.Core] Unloaded cleanly.");
        }

        [RocketCommand("kjmenu", "Open the Kanomjeen server menu", "", AllowedCaller.Player)]
        [RocketCommandAlias("menu")]
        public void CommandMenu(IRocketPlayer caller, string[] args)
        {
            var player = caller as UnturnedPlayer;
            if (player?.Player == null || Ui == null) return;
            Ui.Open(player, "main", "KANOMJEEN", "Semi-Vanilla Survival • California 2");
            Ui.SetVisible(player, "KJ_Main_Admin", GameplayGuard.Has(player, "kanomjeen.admin.inspect"));
        }

        [RocketCommand("waypoint", "Manage personal waypoints", "add <name> | list | track <number> | stop | rename <number> <name> | delete <number>", AllowedCaller.Player)]
        [RocketCommandAlias("wp")]
        public void CommandWaypoint(IRocketPlayer caller, string[] args)
        {
            var player = caller as UnturnedPlayer;
            if (player?.Player == null || Waypoints == null) return;
            if (!GameplayGuard.Has(player, "kanomjeen.waypoint.use")) { Say(player, "You do not have waypoint permission."); return; }
            var action = args != null && args.Length > 0 ? args[0].ToLowerInvariant() : "list";
            if (action == "add")
            {
                var name = JoinTail(args, 1);
                var created = Waypoints.CreatePlayer(player, name);
                Say(player, created == null ? "Could not create waypoint. Check the name and your saved limit." : "Waypoint created: " + created.Name);
                ShowWaypoints(player); return;
            }
            if (action == "stop") { Waypoints.StopTracking(player); Say(player, "Waypoint tracking stopped."); return; }
            if (action == "track" || action == "delete" || action == "rename")
            {
                if (args.Length < 2 || !int.TryParse(args[1], out var number)) { Say(player, "Use /wp " + action + " <number>" + (action == "rename" ? " <name>" : "")); return; }
                var list = Waypoints.GetVisible(player);
                var index = number - 1;
                if (index < 0 || index >= list.Count) { Say(player, "Waypoint number not found."); return; }
                var ok = action == "track" ? Waypoints.Track(player, list[index].Id)
                    : action == "delete" ? Waypoints.Delete(player, list[index].Id)
                    : Waypoints.Rename(player, list[index].Id, JoinTail(args, 2));
                Say(player, ok ? "Waypoint updated." : "Waypoint action was rejected.");
                ShowWaypoints(player); return;
            }
            ShowWaypoints(player);
        }

        internal void ShowWaypoints(UnturnedPlayer player)
        {
            WaypointsUi.Show(player);
            SayWaypoints(player);
        }

        private void OnUiButton(object sender, UiButtonEventArgs e)
        {
            if (e?.Player == null || Waypoints == null) return;
            WaypointsUi.OnButton(e.Player, e.Screen, e.Button);
        }

        // Surface used by WaypointUi: display lines and the actions it routes to.
        internal List<string> WaypointLines(UnturnedPlayer player)
        {
            var list = Waypoints.GetVisible(player);
            var lines = new List<string>();
            for (var i = 0; i < list.Count; i++)
                lines.Add((i + 1) + ". " + list[i].Name + (Waypoints.IsTracked(player.Id, list[i].Id) ? "  [TRACKED]" : ""));
            return lines;
        }

        internal bool WaypointTrack(UnturnedPlayer player, int index)
        {
            var list = Waypoints.GetVisible(player);
            return index >= 0 && index < list.Count && Waypoints.Track(player, list[index].Id);
        }

        internal bool WaypointDelete(UnturnedPlayer player, int index)
        {
            var list = Waypoints.GetVisible(player);
            return index >= 0 && index < list.Count && Waypoints.Delete(player, list[index].Id);
        }

        internal void WaypointStop(UnturnedPlayer player) => Waypoints?.StopTracking(player);

        internal void SayWaypoints(UnturnedPlayer player)
        {
            var list = Waypoints.GetVisible(player);
            Say(player, "Waypoints: " + list.Count + "/" + Waypoints.EffectiveLimit(player));
            for (var i = 0; i < list.Count; i++) Say(player, (i + 1) + ". " + list[i].Name + (Waypoints.IsTracked(player.Id, list[i].Id) ? " [tracked]" : ""));
        }
        private static string JoinTail(string[] args, int start) => args == null || args.Length <= start ? null : string.Join(" ", args, start, args.Length - start).Trim();
        private static void Say(UnturnedPlayer player, string text) { if (player != null) Rocket.Unturned.Chat.UnturnedChat.Say(player, text, Color.cyan); }

        private void FlushPersistence() { Cooldowns?.SaveIfDirty(); Waypoints?.SaveIfDirty(); }
        private void SweepWaypoints() { if (Waypoints == null) return; Waypoints.SweepExpired(); foreach (var steamPlayer in Provider.clients) Waypoints.Sync(UnturnedPlayer.FromSteamPlayer(steamPlayer)); }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (player == null) return;
            PlayerDisconnected?.Invoke(player.Id);
            Ui?.Close(player);
            RateLimiter?.RemovePlayer(player.Id);
            Teleports?.Release(player.Id);
            PlayerStates?.Remove(player.Id);
        }

        private void OnPlayerConnected(UnturnedPlayer player) { Waypoints?.Sync(player); }

        private void OnPlayerDamaged(Player nativePlayer, ref EDeathCause cause, ref ELimb limb, ref CSteamID killerId,
            ref Vector3 direction, ref float damage, ref float times, ref bool canDamage)
        {
            if (!canDamage || damage <= 0f || nativePlayer == null) return;
            var victim = UnturnedPlayer.FromPlayer(nativePlayer);
            if (victim == null) return;

            var now = DateTime.UtcNow;
            PlayerStates.TagCombat(victim.Id, now, TimeSpan.FromSeconds(Configuration.Instance.CombatCooldownSeconds));
            PlayerDamaged?.Invoke(victim.Id);

            if (killerId == CSteamID.Nil || killerId == victim.CSteamID) return;
            var attacker = UnturnedPlayer.FromCSteamID(killerId);
            if (attacker != null)
                PlayerStates.TagCombat(attacker.Id, now, TimeSpan.FromSeconds(Configuration.Instance.CombatCooldownSeconds));
        }

        private void OnBarricadeDamage(CSteamID instigatorSteamID, Transform barricadeTransform, ref ushort pendingTotalDamage,
            ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            TagRaider(instigatorSteamID, pendingTotalDamage, shouldAllow);
        }

        private void OnStructureDamage(CSteamID instigatorSteamID, Transform structureTransform, ref ushort pendingTotalDamage,
            ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            TagRaider(instigatorSteamID, pendingTotalDamage, shouldAllow);
        }

        private void TagRaider(CSteamID instigatorSteamID, ushort pendingTotalDamage, bool shouldAllow)
        {
            if (!shouldAllow || pendingTotalDamage == 0 || instigatorSteamID == CSteamID.Nil) return;
            var player = UnturnedPlayer.FromCSteamID(instigatorSteamID);
            if (player == null) return;
            PlayerStates.TagRaid(player.Id, DateTime.UtcNow, TimeSpan.FromSeconds(Configuration.Instance.RaidCooldownSeconds));
            RaidTriggered?.Invoke(player.Id);
        }

        private void ValidateConfiguration()
        {
            var config = Configuration.Instance;
            if (config.CombatCooldownSeconds < 0f) config.CombatCooldownSeconds = 30f;
            if (config.RaidCooldownSeconds < 0f) config.RaidCooldownSeconds = 180f;
            if (config.CommandRateLimitSeconds < 0.1f) config.CommandRateLimitSeconds = 1.5f;
            if (config.TeleportMovementToleranceMeters < 0.1f) config.TeleportMovementToleranceMeters = 0.5f;
            if (config.PersistenceFlushSeconds < 10f) config.PersistenceFlushSeconds = 30f;
            if (config.WaypointSavedLimit < 1) config.WaypointSavedLimit = 10;
            if (config.WaypointMaximumPermissionLimit < config.WaypointSavedLimit) config.WaypointMaximumPermissionLimit = config.WaypointSavedLimit;
            if (config.WaypointSaveIntervalSeconds < 10f) config.WaypointSaveIntervalSeconds = 30f;
            config.WaypointMode = "FallbackNativeMarker";
            if (config.Zones == null) config.Zones = new System.Collections.Generic.List<ZoneRule>();
            Configuration.Save();
        }
    }
}
