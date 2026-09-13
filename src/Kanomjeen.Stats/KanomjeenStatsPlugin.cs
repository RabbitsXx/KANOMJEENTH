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
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Kanomjeen.Stats
{
    public sealed class KanomjeenStatsPlugin : RocketPlugin<StatsConfiguration>
    {
        private StatsStore store;
        private KanomjeenCorePlugin boundCore;
        private KanomjeenStatsUi ui;
        private KanomjeenStatsUi StatsUi => ui ?? (ui = new KanomjeenStatsUi(this));
        internal UiService Ui => Core?.Ui;
        private readonly Dictionary<string, DateTime> connectedUtc = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        private readonly Dictionary<string, DateTime> lifeStartedUtc = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        private KanomjeenCorePlugin Core => KanomjeenCorePlugin.Instance;
        private Color MessageColor => UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor ?? "cyan", Color.cyan);

        protected override void Load()
        {
            Validate();
            var dir = Path.GetDirectoryName(typeof(KanomjeenStatsPlugin).Assembly.Location) ?? ".";
            store = new StatsStore(Path.Combine(dir, "Kanomjeen.Stats.xml"));
            U.Events.OnPlayerConnected += OnConnected;
            U.Events.OnPlayerDisconnected += OnDisconnected;
            UnturnedPlayerEvents.OnPlayerDeath += OnDeath;
            UnturnedPlayerEvents.OnPlayerRevive += OnRevive;
            UnturnedPlayerEvents.OnPlayerUpdateStat += OnUpdateStat;
            if (!TryBindCore()) InvokeRepeating(nameof(BindCoreTick), 1f, 1f);
            var save = Math.Max(10f, Configuration.Instance.SaveIntervalSeconds);
            InvokeRepeating(nameof(Save), save, save);
            Logger.Log("[Kanomjeen.Stats] Loaded.");
        }

        protected override void Unload()
        {
            CancelInvoke();
            U.Events.OnPlayerConnected -= OnConnected;
            U.Events.OnPlayerDisconnected -= OnDisconnected;
            UnturnedPlayerEvents.OnPlayerDeath -= OnDeath;
            UnturnedPlayerEvents.OnPlayerRevive -= OnRevive;
            UnturnedPlayerEvents.OnPlayerUpdateStat -= OnUpdateStat;
            UnbindCore();
            FlushOnlinePlaytime();
            store?.SaveIfDirty();
            connectedUtc.Clear(); lifeStartedUtc.Clear(); store = null;
            Logger.Log("[Kanomjeen.Stats] Unloaded.");
        }

        [RocketCommand("stats", "Show Kanomjeen player stats", "[player]", AllowedCaller.Player)]
        public void CommandStats(IRocketPlayer caller, string[] args)
        {
            var viewer = caller as UnturnedPlayer; if (viewer == null) return;
            UnturnedPlayer target = viewer;
            if (args != null && args.Length > 0)
            {
                var resolved = Core?.Players.Resolve(string.Join(" ", args));
                if (resolved == null || resolved.Status == PlayerResolveStatus.NotFound) { Say(viewer, "NotFound"); return; }
                if (resolved.Status == PlayerResolveStatus.Ambiguous) { Say(viewer, "Ambiguous", resolved.MatchCount); return; }
                target = resolved.Player;
            }
            Show(viewer, target);
        }

        private void OnUiButton(object sender, UiButtonEventArgs e)
        {
            if (e?.Player == null) return;
            StatsUi.OnButton(e.Player, e.Screen, e.Button);
        }

        /// <summary>Entry point used by the UI layer for the main-menu Stats card and /stats.</summary>
        internal void ShowStatsFor(UnturnedPlayer player) => Show(player, player);

        private bool TryBindCore()
        {
            var core = KanomjeenCorePlugin.Instance;
            if (core == null) return false;
            if (ReferenceEquals(boundCore, core)) return true;
            UnbindCore();
            boundCore = core;
            boundCore.Ui.ButtonClicked += OnUiButton;
            CancelInvoke(nameof(BindCoreTick));
            Logger.Log("[Kanomjeen.Stats] Bound to Kanomjeen.Core.");
            return true;
        }

        private void BindCoreTick() { TryBindCore(); }

        private void UnbindCore()
        {
            if (boundCore?.Ui != null) boundCore.Ui.ButtonClicked -= OnUiButton;
            boundCore = null;
        }

        public void RecordAirdropCapture(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return;
            store.Get(playerId).AirdropsCaptured++;
            store.MarkDirty();
        }

        private void OnConnected(UnturnedPlayer player)
        {
            if (!Configuration.Instance.Enabled || player == null) return;
            var now = DateTime.UtcNow;
            connectedUtc[player.Id] = now;
            lifeStartedUtc[player.Id] = now;
            store.Get(player.Id, player.DisplayName);
        }

        private void OnDisconnected(UnturnedPlayer player)
        {
            if (player == null) return;
            AccumulateSession(player.Id, player.DisplayName, DateTime.UtcNow);
            connectedUtc.Remove(player.Id);
            lifeStartedUtc.Remove(player.Id);
        }

        private void OnRevive(UnturnedPlayer player, Vector3 position, byte angle)
        {
            if (player != null) lifeStartedUtc[player.Id] = DateTime.UtcNow;
        }

        private void OnDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            if (!Configuration.Instance.Enabled || player == null) return;
            var now = DateTime.UtcNow;
            var victimStats = store.Get(player.Id, player.DisplayName);
            victimStats.Deaths++;
            if (lifeStartedUtc.TryGetValue(player.Id, out var started))
            {
                var life = Math.Max(0d, (now - started).TotalSeconds);
                if (life > victimStats.LongestLifeSeconds) victimStats.LongestLifeSeconds = life;
            }
            lifeStartedUtc.Remove(player.Id);

            if (murderer != CSteamID.Nil && murderer != player.CSteamID)
            {
                var killer = UnturnedPlayer.FromCSteamID(murderer);
                if (killer != null)
                {
                    var killerStats = store.Get(killer.Id, killer.DisplayName);
                    killerStats.Kills++;
                    if (limb == ELimb.SKULL) killerStats.Headshots++;
                }
            }
            store.MarkDirty();
        }

        private void OnUpdateStat(UnturnedPlayer player, EPlayerStat stat)
        {
            if (!Configuration.Instance.Enabled || player == null) return;
            if (stat != EPlayerStat.KILLS_ZOMBIES_NORMAL && stat != EPlayerStat.KILLS_ZOMBIES_MEGA) return;
            store.Get(player.Id, player.DisplayName).ZombieKills++;
            store.MarkDirty();
        }

        private void Show(UnturnedPlayer viewer, UnturnedPlayer target)
        {
            AccumulateSession(target.Id, target.DisplayName, DateTime.UtcNow, false);
            var data = store.Get(target.Id, target.DisplayName);
            var playtime = data.PlaytimeSeconds + CurrentSessionSeconds(target.Id);
            var kdr = data.Deaths == 0 ? data.Kills : (double)data.Kills / data.Deaths;
            Say(viewer, "Summary", target.DisplayName, data.Kills, data.Deaths, kdr.ToString("0.00"), data.ZombieKills, FormatDuration(playtime));

            StatsUi.Show(viewer, new StatSnapshot
            {
                DisplayName = target.DisplayName,
                Kills = data.Kills.ToString(),
                Deaths = data.Deaths.ToString(),
                Kdr = kdr.ToString("0.00"),
                Zombies = data.ZombieKills.ToString(),
                Headshots = data.Headshots.ToString(),
                Playtime = FormatDuration(playtime),
                LongestLife = FormatDuration(data.LongestLifeSeconds),
                Airdrops = data.AirdropsCaptured.ToString()
            });
        }

        private void AccumulateSession(string playerId, string name, DateTime now, bool consume = true)
        {
            if (!connectedUtc.TryGetValue(playerId, out var connected)) return;
            var seconds = Math.Max(0d, (now - connected).TotalSeconds);
            if (consume)
            {
                store.Get(playerId, name).PlaytimeSeconds += seconds;
                store.MarkDirty();
                connectedUtc[playerId] = now;
            }
        }

        private double CurrentSessionSeconds(string playerId)
        {
            return connectedUtc.TryGetValue(playerId, out var started) ? Math.Max(0d, (DateTime.UtcNow - started).TotalSeconds) : 0d;
        }

        private void FlushOnlinePlaytime()
        {
            var now = DateTime.UtcNow;
            var ids = new List<string>(connectedUtc.Keys);
            foreach (var id in ids)
            {
                var player = Core?.Players.FindById(id);
                AccumulateSession(id, player?.DisplayName, now);
            }
        }

        private void Save() { FlushOnlinePlaytime(); store?.SaveIfDirty(); }
        private static string FormatDuration(double seconds)
        {
            var span = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (span.TotalDays >= 1) return ((int)span.TotalDays) + "d " + span.Hours + "h";
            if (span.TotalHours >= 1) return ((int)span.TotalHours) + "h " + span.Minutes + "m";
            return span.Minutes + "m " + span.Seconds + "s";
        }
        private void Say(IRocketPlayer player, string key, params object[] args) { if (player != null) UnturnedChat.Say(player, Translate(key, args), MessageColor, false); }
        private void Validate() { if (Configuration.Instance.SaveIntervalSeconds < 10f) Configuration.Instance.SaveIntervalSeconds = 30f; Configuration.Save(); }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "NotFound", "Player was not found online." }, { "Ambiguous", "That name matches {0} players." },
            { "Summary", "{0} — K {1} / D {2} / KDR {3} / Zombies {4} / Playtime {5}" }
        };
    }
}
