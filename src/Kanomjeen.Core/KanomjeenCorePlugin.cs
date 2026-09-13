using System;
using System.IO;
using Kanomjeen.Core.Configuration;
using Kanomjeen.Core.Services;
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

        public KanomjeenCoreConfiguration Config => Configuration.Instance;
        public PlayerStateService PlayerStates { get; private set; }
        public CommandRateLimiter RateLimiter { get; private set; }
        public PersistentCooldownService Cooldowns { get; private set; }
        public TeleportLockService Teleports { get; private set; }
        public ZoneService Zones { get; private set; }
        public GameplayGuard Guard { get; private set; }
        public PlayerResolver Players { get; private set; }
        public UiService Ui { get; private set; }

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
            Ui = new UiService(Configuration.Instance.EnableUi ? Configuration.Instance.UiEffectId : (ushort)0, Configuration.Instance.UiKey, Configuration.Instance.UiContractVersion);
            Ui.Subscribe();

            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            DamageTool.playerDamaged += OnPlayerDamaged;
            BarricadeManager.onDamageBarricadeRequested += OnBarricadeDamage;
            StructureManager.onDamageStructureRequested += OnStructureDamage;

            var flush = Math.Max(10f, Configuration.Instance.PersistenceFlushSeconds);
            InvokeRepeating(nameof(FlushPersistence), flush, flush);
            Logger.Log("[Kanomjeen.Core] Loaded: combat, raid, zones, cooldown persistence, player resolver and UI bridge.");
        }

        protected override void Unload()
        {
            CancelInvoke();
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            DamageTool.playerDamaged -= OnPlayerDamaged;
            BarricadeManager.onDamageBarricadeRequested -= OnBarricadeDamage;
            StructureManager.onDamageStructureRequested -= OnStructureDamage;
            Ui?.Unsubscribe();
            Cooldowns?.SaveIfDirty();

            RateLimiter?.Clear();
            Teleports?.Clear();
            PlayerStates?.Clear();
            Ui = null;
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

        private void FlushPersistence() => Cooldowns?.SaveIfDirty();

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (player == null) return;
            PlayerDisconnected?.Invoke(player.Id);
            Ui?.Close(player);
            RateLimiter?.RemovePlayer(player.Id);
            Teleports?.Release(player.Id);
            PlayerStates?.Remove(player.Id);
        }

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
            if (config.Zones == null) config.Zones = new System.Collections.Generic.List<ZoneRule>();
            Configuration.Save();
        }
    }
}
