using Kanomjeen.Core.Services;
using Rocket.Unturned.Player;

namespace Kanomjeen.Airdrops
{
    /// <summary>Display-ready airdrop objective state, built by the plugin from its live schedule.</summary>
    internal sealed class AirdropView
    {
        internal bool Active { get; set; }
        internal string Region { get; set; }
        internal string Distance { get; set; }
        internal string Timer { get; set; }
        internal string State { get; set; }
    }

    /// <summary>
    /// All Airdrops client UI. The plugin owns the schedule, the objective zone and the live spawn;
    /// this class only presents the snapshot it is handed and routes the main-menu card
    /// (see AGENTS.md §13).
    /// </summary>
    internal sealed class KanomjeenAirdropsUi
    {
        private const string ScreenName = "airdrop";
        private const string MainCard = "KJ_Main_Airdrop";

        private const string RegionElement = "KJ_Airdrop_Region";
        private const string DistanceElement = "KJ_Airdrop_Distance";
        private const string TimerElement = "KJ_Airdrop_Timer";
        private const string StateElement = "KJ_Airdrop_State";

        private const string Title = "KANOMJEEN • AIRDROP";
        private const string ActiveStatus = "PvP objective — TPA/Home disabled inside objective radius.";
        private const string StandbyStatus = "No active objective. Automatic events depend on population and schedule.";

        private readonly KanomjeenAirdropsPlugin plugin;

        internal KanomjeenAirdropsUi(KanomjeenAirdropsPlugin plugin)
        {
            this.plugin = plugin;
        }

        /// <summary>Present the current objective state. The plugin always supplies a snapshot.</summary>
        internal void Show(UnturnedPlayer player, AirdropView view)
        {
            if (view == null) return;
            UiGuard.Run("airdrop.show", () =>
            {
                var ui = plugin.Ui;
                if (ui == null || !ui.IsConfigured) return;

                ui.ShowScreen(player, ScreenName, Title, view.Active ? ActiveStatus : StandbyStatus);
                ui.SetText(player, RegionElement, view.Region ?? string.Empty);
                ui.SetText(player, DistanceElement, view.Distance ?? string.Empty);
                ui.SetText(player, TimerElement, view.Timer ?? string.Empty);
                ui.SetText(player, StateElement, view.State ?? string.Empty);
            });
        }

        internal void OnButton(UnturnedPlayer player, string screen, string button)
        {
            UiGuard.Run("airdrop.button:" + button, () =>
            {
                if (screen == "main" && button == MainCard) plugin.RefreshAirdropUi(player);
            });
        }
    }
}
