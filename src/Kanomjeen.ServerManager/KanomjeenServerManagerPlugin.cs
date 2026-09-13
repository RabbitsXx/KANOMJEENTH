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
using UnityEngine;

namespace Kanomjeen.ServerManager
{
    public sealed class KanomjeenServerManagerPlugin : RocketPlugin<ServerManagerConfiguration>
    {
        private DateTime startedUtc;
        private DateTime nextRestartUtc;
        private DateTime restartAtUtc = DateTime.MinValue;
        private int lastCountdownAnnouncement = int.MinValue;
        private int announcementIndex;
        private Color MessageColor => UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor ?? "cyan", Color.cyan);

        protected override void Load()
        {
            Validate();
            startedUtc = DateTime.UtcNow;
            ScheduleAutomaticRestart();
            InvokeRepeating(nameof(Tick), 1f, 1f);
            if (Configuration.Instance.AutoSaveIntervalSeconds > 0f)
                InvokeRepeating(nameof(SafeSave), Math.Max(60f, Configuration.Instance.AutoSaveIntervalSeconds), Math.Max(60f, Configuration.Instance.AutoSaveIntervalSeconds));
            if (Configuration.Instance.AnnouncementIntervalSeconds > 0f)
                InvokeRepeating(nameof(Announcement), Math.Max(60f, Configuration.Instance.AnnouncementIntervalSeconds), Math.Max(60f, Configuration.Instance.AnnouncementIntervalSeconds));
            Logger.Log("[Kanomjeen.ServerManager] Loaded.");
        }

        protected override void Unload()
        {
            CancelInvoke();
            restartAtUtc = DateTime.MinValue;
            SafeSave();
            Logger.Log("[Kanomjeen.ServerManager] Unloaded.");
        }

        [RocketCommand("serverstatus", "Show Kanomjeen server uptime and restart status", "", AllowedCaller.Both)]
        public void CommandStatus(IRocketPlayer caller, string[] args)
        {
            var uptime = DateTime.UtcNow - startedUtc;
            var restart = restartAtUtc > DateTime.UtcNow ? restartAtUtc : nextRestartUtc;
            Say(caller, "Status", Format(uptime), Provider.clients.Count, Math.Max(0, Math.Ceiling((restart - DateTime.UtcNow).TotalSeconds)));
        }

        [RocketCommand("kjrestart", "Schedule a safe Kanomjeen restart", "[seconds]", AllowedCaller.Both)]
        public void CommandRestart(IRocketPlayer caller, string[] args)
        {
            if (!Admin(caller, "kanomjeen.admin.restart")) { Say(caller, "NoPermission"); return; }
            var seconds = 60;
            if (args != null && args.Length > 0 && (!int.TryParse(args[0], out seconds) || seconds < 0 || seconds > 3600)) { Say(caller, "RestartUsage"); return; }
            restartAtUtc = DateTime.UtcNow.AddSeconds(seconds);
            lastCountdownAnnouncement = int.MinValue;
            Broadcast("RestartScheduled", seconds);
        }

        [RocketCommand("kjcancelrestart", "Cancel pending Kanomjeen restart", "", AllowedCaller.Both)]
        public void CommandCancelRestart(IRocketPlayer caller, string[] args)
        {
            if (!Admin(caller, "kanomjeen.admin.restart")) { Say(caller, "NoPermission"); return; }
            if (restartAtUtc == DateTime.MinValue) { Say(caller, "NoRestart"); return; }
            restartAtUtc = DateTime.MinValue;
            lastCountdownAnnouncement = int.MinValue;
            ScheduleAutomaticRestart();
            Broadcast("RestartCancelled");
        }

        [RocketCommand("kjannounce", "Broadcast a server announcement", "<message>", AllowedCaller.Both)]
        public void CommandAnnounce(IRocketPlayer caller, string[] args)
        {
            if (!Admin(caller, "kanomjeen.admin.announce")) { Say(caller, "NoPermission"); return; }
            var text = args == null ? string.Empty : string.Join(" ", args).Trim();
            if (string.IsNullOrWhiteSpace(text)) { Say(caller, "AnnounceUsage"); return; }
            UnturnedChat.Say("[Kanomjeen] " + text, MessageColor);
        }

        private void Tick()
        {
            if (!Configuration.Instance.Enabled) return;
            var now = DateTime.UtcNow;
            if (restartAtUtc == DateTime.MinValue && Configuration.Instance.RestartEveryHours > 0f && now >= nextRestartUtc)
            {
                restartAtUtc = now.AddSeconds(Math.Max(0, Configuration.Instance.AutomaticRestartCountdownSeconds));
                lastCountdownAnnouncement = int.MinValue;
                Broadcast("RestartScheduled", Configuration.Instance.AutomaticRestartCountdownSeconds);
            }
            if (restartAtUtc == DateTime.MinValue) return;

            var seconds = (int)Math.Ceiling((restartAtUtc - now).TotalSeconds);
            if (seconds <= 0)
            {
                SafeSave();
                Logger.Log("[Kanomjeen.ServerManager] Safe save complete; shutting down for restart.");
                Provider.shutdown();
                restartAtUtc = DateTime.MinValue;
                return;
            }

            if (ShouldAnnounce(seconds) && seconds != lastCountdownAnnouncement)
            {
                lastCountdownAnnouncement = seconds;
                Broadcast("RestartCountdown", seconds);
            }
        }

        private static bool ShouldAnnounce(int seconds)
        {
            return seconds == 300 || seconds == 180 || seconds == 120 || seconds == 60 || seconds == 30 || seconds == 15 || seconds == 10 || seconds <= 5;
        }

        private void SafeSave()
        {
            try { SaveManager.save(); }
            catch (Exception ex) { Logger.LogError("[Kanomjeen.ServerManager] Save failed: " + ex.Message); }
        }

        private void Announcement()
        {
            var list = Configuration.Instance.Announcements;
            if (list == null || list.Count == 0) return;
            if (announcementIndex >= list.Count) announcementIndex = 0;
            var text = list[announcementIndex++];
            if (!string.IsNullOrWhiteSpace(text)) UnturnedChat.Say("[Kanomjeen] " + text, MessageColor);
        }

        private void ScheduleAutomaticRestart()
        {
            nextRestartUtc = Configuration.Instance.RestartEveryHours > 0f
                ? DateTime.UtcNow.AddHours(Configuration.Instance.RestartEveryHours)
                : DateTime.MaxValue;
        }

        private bool Admin(IRocketPlayer caller, string permission)
        {
            if (caller == null) return true;
            var player = caller as UnturnedPlayer;
            return player != null && GameplayGuard.Has(player, permission);
        }
        private void Broadcast(string key, params object[] args) => UnturnedChat.Say(Translate(key, args), MessageColor);
        private void Say(IRocketPlayer player, string key, params object[] args)
        {
            if (player == null) Logger.Log(Translate(key, args)); else UnturnedChat.Say(player, Translate(key, args), MessageColor, false);
        }
        private static string Format(TimeSpan span) => ((int)span.TotalDays > 0 ? ((int)span.TotalDays) + "d " : string.Empty) + span.Hours + "h " + span.Minutes + "m";
        private void Validate()
        {
            var c = Configuration.Instance;
            if (c.AutoSaveIntervalSeconds < 0f) c.AutoSaveIntervalSeconds = 900f;
            if (c.RestartEveryHours < 0f) c.RestartEveryHours = 6f;
            if (c.AutomaticRestartCountdownSeconds < 0) c.AutomaticRestartCountdownSeconds = 300;
            if (c.AnnouncementIntervalSeconds < 0f) c.AnnouncementIntervalSeconds = 1200f;
            if (c.Announcements == null) c.Announcements = new System.Collections.Generic.List<string>();
            Configuration.Save();
        }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "NoPermission", "You do not have permission for that server action." },
            { "Status", "Uptime {0} | Players {1} | Next restart/check in ~{2}s" },
            { "RestartUsage", "Usage: /kjrestart [0-3600 seconds]" },
            { "RestartScheduled", "[Kanomjeen] Server restart scheduled in {0}s. A safe save will run first." },
            { "RestartCountdown", "[Kanomjeen] Restart in {0}s." },
            { "RestartCancelled", "[Kanomjeen] Pending restart cancelled." },
            { "NoRestart", "No manual restart countdown is active." },
            { "AnnounceUsage", "Usage: /kjannounce <message>" }
        };
    }
}
