using System;
using Kanomjeen.Core;
using Kanomjeen.Core.Configuration;
using Kanomjeen.Core.Services;
using Kanomjeen.Core.Waypoints;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core.Commands;
using Logger = Rocket.Core.Logging.Logger;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace Kanomjeen.Airdrops
{
    public sealed class KanomjeenAirdropsPlugin : RocketPlugin<AirdropConfiguration>
    {
        private readonly System.Random random = new System.Random();
        private KanomjeenCorePlugin boundCore;
        private DateTime nextDropUtc;
        private DateTime activeUntilUtc = DateTime.MinValue;
        private AirdropSpawn activeSpawn;
        private const string DynamicZoneKey = "kanomjeen.airdrop.active";
        private KanomjeenCorePlugin Core => KanomjeenCorePlugin.Instance;
        private KanomjeenAirdropsUi ui;
        private KanomjeenAirdropsUi AirdropsUi => ui ?? (ui = new KanomjeenAirdropsUi(this));
        internal UiService Ui => Core?.Ui;
        private Color MessageColor => UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor ?? "yellow", Color.yellow);

        protected override void Load()
        {
            Validate();
            ScheduleNext();
            if (!TryBindCore()) InvokeRepeating(nameof(BindCoreTick), 1f, 1f);
            InvokeRepeating(nameof(Tick), 1f, 1f);
            Logger.Log("[Kanomjeen.Airdrops] Loaded. Configure California 2 spawns with /setairdropspawn <name> <airdropId>.");
        }

        protected override void Unload()
        {
            CancelInvoke();
            UnbindCore();
            Core?.Zones.RemoveDynamic(DynamicZoneKey);
            Core?.Waypoints?.RemoveFeature(null, WaypointSource.Airdrop, DynamicZoneKey);
            activeSpawn = null;
            Logger.Log("[Kanomjeen.Airdrops] Unloaded.");
        }

        [RocketCommand("whenairdrop", "Show next/active Kanomjeen airdrop", "", AllowedCaller.Player)]
        public void CommandWhenAirdrop(IRocketPlayer caller, string[] args)
        {
            var player = caller as UnturnedPlayer;
            if (player == null) return;
            if (activeSpawn != null && activeUntilUtc > DateTime.UtcNow)
            {
                var distance = Vector3.Distance(player.Position, Position(activeSpawn));
                Say(player, "Active", activeSpawn.Name, Math.Round(distance), Math.Ceiling((activeUntilUtc - DateTime.UtcNow).TotalSeconds));
                RefreshAirdropUi(player);
                return;
            }
            if (!Configuration.Instance.AutoEnabled || Configuration.Instance.Spawns.Count == 0) { Say(player, "NotScheduled"); return; }
            Say(player, "Next", Math.Max(0, Math.Ceiling((nextDropUtc - DateTime.UtcNow).TotalSeconds)));
        }

        [RocketCommand("kjairdrop", "Force a configured Kanomjeen airdrop", "[spawn]", AllowedCaller.Both)]
        public void CommandForce(IRocketPlayer caller, string[] args)
        {
            var player = caller as UnturnedPlayer;
            if (!IsAdmin(caller, "kanomjeen.admin.airdrop")) { Say(player, "NoPermission"); return; }
            if (Core == null) { Say(player, "CoreUnavailable"); return; }
            if (Configuration.Instance.Spawns.Count == 0) { Say(player, "NoSpawns"); return; }
            AirdropSpawn spawn = null;
            if (args != null && args.Length > 0)
            {
                var name = string.Join(" ", args).Trim();
                foreach (var candidate in Configuration.Instance.Spawns)
                    if (candidate != null && string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)) { spawn = candidate; break; }
                if (spawn == null) { Say(player, "SpawnNotFound", name); return; }
            }
            else spawn = RandomSpawn();
            StartAirdrop(spawn, "admin");
        }

        [RocketCommand("setairdropspawn", "Save an airdrop spawn at your position", "<name> <airdropId>", AllowedCaller.Player)]
        public void CommandSetSpawn(IRocketPlayer caller, string[] args)
        {
            var player = caller as UnturnedPlayer;
            if (player == null || !IsAdmin(player, "kanomjeen.admin.airdrop")) { Say(player, "NoPermission"); return; }
            if (args == null || args.Length < 2 || !ushort.TryParse(args[args.Length - 1], out var id) || id == 0) { Say(player, "SetUsage"); return; }
            var name = string.Join(" ", args, 0, args.Length - 1).Trim();
            if (string.IsNullOrWhiteSpace(name)) { Say(player, "SetUsage"); return; }

            AirdropSpawn spawn = null;
            foreach (var existing in Configuration.Instance.Spawns)
                if (existing != null && string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase)) { spawn = existing; break; }
            if (spawn == null)
            {
                spawn = new AirdropSpawn();
                Configuration.Instance.Spawns.Add(spawn);
            }
            spawn.Name = name;
            spawn.AirdropId = id;
            spawn.X = player.Position.x;
            spawn.Y = player.Position.y;
            spawn.Z = player.Position.z;
            Configuration.Save();
            Say(player, "SpawnSaved", name, id);
        }

        private void Tick()
        {
            var now = DateTime.UtcNow;
            if (activeSpawn != null && activeUntilUtc <= now)
            {
                Core?.Zones.RemoveDynamic(DynamicZoneKey);
                Core?.Waypoints?.RemoveFeature(null, WaypointSource.Airdrop, DynamicZoneKey);
                activeSpawn = null;
                activeUntilUtc = DateTime.MinValue;
                UnturnedChat.Say(Translate("ObjectiveEnded"), MessageColor);
            }

            if (!Configuration.Instance.Enabled || !Configuration.Instance.AutoEnabled || activeSpawn != null || now < nextDropUtc) return;
            if (Core == null) return;
            if (Configuration.Instance.Spawns.Count == 0) { ScheduleNext(); return; }
            if (Provider.clients.Count < Configuration.Instance.MinPlayers) { ScheduleNext(); return; }
            StartAirdrop(RandomSpawn(), "automatic");
        }

        private void StartAirdrop(AirdropSpawn spawn, string source)
        {
            if (spawn == null || spawn.AirdropId == 0) return;
            var position = Position(spawn);
            LevelManager.airdrop(position, spawn.AirdropId, Math.Max(1f, Configuration.Instance.AirdropSpeed));
            activeSpawn = spawn;
            activeUntilUtc = DateTime.UtcNow.AddSeconds(Math.Max(60f, Configuration.Instance.ObjectiveLifetimeSeconds));

            Core?.Zones.AddOrUpdateDynamic(DynamicZoneKey, new ZoneRule
            {
                Name = "Airdrop Objective: " + spawn.Name,
                X = spawn.X,
                Y = spawn.Y,
                Z = spawn.Z,
                Radius = Math.Max(50f, Configuration.Instance.ObjectiveRadius),
                BlockTPA = true,
                BlockHome = true,
                BlockKit = true,
                BlockBuild = Configuration.Instance.BlockBuildInObjective,
                AirdropObjective = true
            });
            Core?.Waypoints?.UpsertFeature(null, "Airdrop: " + spawn.Name, position, WaypointIcon.Crate, WaypointColor.Red, WaypointVisibility.Everyone, WaypointSource.Airdrop, DynamicZoneKey, activeUntilUtc, WaypointOwnerType.Event);

            UnturnedChat.Say(Translate("Incoming", spawn.Name), MessageColor);
            ScheduleNext();
            Logger.Log("[Kanomjeen.Airdrops] Airdrop started at '" + spawn.Name + "' by " + source + ".");
        }

        private AirdropSpawn RandomSpawn()
        {
            var valid = Configuration.Instance.Spawns.FindAll(x => x != null && x.AirdropId != 0);
            return valid.Count == 0 ? null : valid[random.Next(valid.Count)];
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
            Logger.Log("[Kanomjeen.Airdrops] Bound to Kanomjeen.Core.");
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
            AirdropsUi.OnButton(e.Player, e.Screen, e.Button);
        }

        /// <summary>Entry point used by the UI layer: present whatever the schedule currently holds.</summary>
        internal void RefreshAirdropUi(UnturnedPlayer player)
        {
            if (player == null) return;
            AirdropsUi.Show(player, BuildView(player));
        }

        private AirdropView BuildView(UnturnedPlayer player)
        {
            var now = DateTime.UtcNow;
            if (activeSpawn != null && activeUntilUtc > now)
            {
                return new AirdropView
                {
                    Active = true,
                    Region = activeSpawn.Name,
                    Distance = Math.Round(Vector3.Distance(player.Position, Position(activeSpawn))) + "m",
                    Timer = Math.Ceiling((activeUntilUtc - now).TotalSeconds) + "s",
                    State = "ACTIVE"
                };
            }

            return new AirdropView
            {
                Active = false,
                Region = "NO ACTIVE DROP",
                Distance = "-",
                Timer = Math.Max(0, Math.Ceiling((nextDropUtc - now).TotalSeconds)) + "s",
                State = "STANDBY"
            };
        }

        private void ScheduleNext()
        {
            var min = Math.Max(60f, Configuration.Instance.MinIntervalSeconds);
            var max = Math.Max(min, Configuration.Instance.MaxIntervalSeconds);
            nextDropUtc = DateTime.UtcNow.AddSeconds(min + (random.NextDouble() * (max - min)));
        }

        private static Vector3 Position(AirdropSpawn spawn) => new Vector3(spawn.X, spawn.Y, spawn.Z);
        private static bool IsAdmin(IRocketPlayer caller, string permission)
        {
            if (caller == null) return true;
            var player = caller as UnturnedPlayer;
            return player != null && GameplayGuard.Has(player, permission);
        }
        private void Say(IRocketPlayer player, string key, params object[] args) { if (player != null) UnturnedChat.Say(player, Translate(key, args), MessageColor, false); }
        private void Validate()
        {
            var c = Configuration.Instance;
            if (c.MinPlayers < 0) c.MinPlayers = 5;
            if (c.MinIntervalSeconds < 60f) c.MinIntervalSeconds = 2400f;
            if (c.MaxIntervalSeconds < c.MinIntervalSeconds) c.MaxIntervalSeconds = c.MinIntervalSeconds;
            if (c.AirdropSpeed < 1f) c.AirdropSpeed = 128f;
            if (c.ObjectiveRadius < 50f) c.ObjectiveRadius = 350f;
            if (c.ObjectiveLifetimeSeconds < 60f) c.ObjectiveLifetimeSeconds = 900f;
            if (c.Spawns == null) c.Spawns = new System.Collections.Generic.List<AirdropSpawn>();
            Configuration.Save();
        }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "Incoming", "Airdrop inbound: {0}. TPA/Home are blocked around the objective." },
            { "ObjectiveEnded", "Airdrop objective restrictions have ended." },
            { "Active", "Active airdrop at {0}: {1}m away, objective ends in {2}s." },
            { "Next", "Next automatic airdrop check is in about {0}s." },
            { "NotScheduled", "No automatic airdrop is currently scheduled." },
            { "NoPermission", "You do not have permission to manage airdrops." },
            { "CoreUnavailable", "Kanomjeen.Core is not ready yet. Try again shortly." },
            { "NoSpawns", "No Kanomjeen airdrop spawns are configured yet." },
            { "SpawnNotFound", "Airdrop spawn '{0}' was not found." },
            { "SetUsage", "Usage: /setairdropspawn <name> <airdropId>" },
            { "SpawnSaved", "Airdrop spawn '{0}' saved with airdrop ID {1}." }
        };
    }
}
