using Kanomjeen.Core.Services;
using Rocket.Unturned.Player;

namespace Kanomjeen.Stats
{
    /// <summary>Display-ready statistics for one player, built by the plugin from its store.</summary>
    internal sealed class StatSnapshot
    {
        internal string DisplayName { get; set; }
        internal string Kills { get; set; }
        internal string Deaths { get; set; }
        internal string Kdr { get; set; }
        internal string Zombies { get; set; }
        internal string Headshots { get; set; }
        internal string Playtime { get; set; }
        internal string LongestLife { get; set; }
        internal string Airdrops { get; set; }
    }

    /// <summary>
    /// All Stats client UI. The plugin owns the store and formats the snapshot; this class only
    /// presents it and routes the main-menu card (see AGENTS.md §13).
    /// </summary>
    internal sealed class KanomjeenStatsUi
    {
        private const string ScreenName = "stats";
        private const string MainCard = "KJ_Main_Stats";

        private const string KillsElement = "KJ_Stats_Kills";
        private const string DeathsElement = "KJ_Stats_Deaths";
        private const string KdrElement = "KJ_Stats_KDR";
        private const string ZombiesElement = "KJ_Stats_Zombies";
        private const string HeadshotsElement = "KJ_Stats_Headshots";
        private const string PlaytimeElement = "KJ_Stats_Playtime";
        private const string LongestLifeElement = "KJ_Stats_LongestLife";
        private const string AirdropsElement = "KJ_Stats_Airdrops";

        private const string Status = "Persistent survival statistics";

        private readonly KanomjeenStatsPlugin plugin;

        internal KanomjeenStatsUi(KanomjeenStatsPlugin plugin)
        {
            this.plugin = plugin;
        }

        internal void Show(UnturnedPlayer viewer, StatSnapshot snapshot)
        {
            UiGuard.Run("stats.show", () =>
            {
                var ui = plugin.Ui;
                if (ui == null || !ui.IsConfigured || snapshot == null) return;

                ui.ShowScreen(viewer, ScreenName, "KANOMJEEN • " + snapshot.DisplayName, Status);
                Apply(viewer, ui, snapshot);
            });
        }

        internal void OnButton(UnturnedPlayer player, string screen, string button)
        {
            UiGuard.Run("stats.button:" + button, () =>
            {
                if (screen == "main" && button == MainCard) plugin.ShowStatsFor(player);
            });
        }

        private static void Apply(UnturnedPlayer viewer, UiService ui, StatSnapshot snapshot)
        {
            if (!ui.IsOpen(viewer)) return;

            ui.SetText(viewer, KillsElement, snapshot.Kills);
            ui.SetText(viewer, DeathsElement, snapshot.Deaths);
            ui.SetText(viewer, KdrElement, snapshot.Kdr);
            ui.SetText(viewer, ZombiesElement, snapshot.Zombies);
            ui.SetText(viewer, HeadshotsElement, snapshot.Headshots);
            ui.SetText(viewer, PlaytimeElement, snapshot.Playtime);
            ui.SetText(viewer, LongestLifeElement, snapshot.LongestLife);
            ui.SetText(viewer, AirdropsElement, snapshot.Airdrops);
        }
    }
}
