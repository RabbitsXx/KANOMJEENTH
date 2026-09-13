using Kanomjeen.Core.Services;
using Rocket.Unturned.Player;

namespace Kanomjeen.Core.Waypoints
{
    /// <summary>
    /// All Waypoints client UI: entry, refresh and button routing. The waypoint store and commands
    /// stay in <see cref="KanomjeenCorePlugin"/>; this class only presents state and forwards intents
    /// (see AGENTS.md §13 for the UI-layer rules this follows).
    /// </summary>
    internal sealed class WaypointUi
    {
        private const string ScreenName = "waypoints";
        private const string RowPrefix = "KJ_Waypoint_Row_";
        private const string NamePrefix = "KJ_Waypoint_Name_";
        private const string TrackPrefix = "KJ_Waypoint_Track_";
        private const string DeletePrefix = "KJ_Waypoint_Delete_";
        private const string StopButton = "KJ_Waypoint_Stop";
        private const string MainCard = "KJ_Main_Waypoints";
        private const int RowCount = 8;

        private const string Title = "KANOMJEEN • WAYPOINTS";
        private const string Status = "Fallback mode: the tracked destination uses Unturned's native map marker.";

        private readonly KanomjeenCorePlugin plugin;

        internal WaypointUi(KanomjeenCorePlugin plugin)
        {
            this.plugin = plugin;
        }

        internal void Show(UnturnedPlayer player)
        {
            UiGuard.Run("waypoints.show", () =>
            {
                var ui = plugin.Ui;
                if (ui == null || !ui.IsConfigured) return;

                ui.ShowScreen(player, ScreenName, Title, Status);
                Refresh(player);
            });
        }

        internal void Refresh(UnturnedPlayer player)
        {
            UiGuard.Run("waypoints.refresh", () =>
            {
                var ui = plugin.Ui;
                if (ui == null || !ui.IsOpen(player)) return;

                var lines = plugin.WaypointLines(player);
                ui.HideRows(player, RowPrefix, RowCount);
                for (var i = 0; i < RowCount && i < lines.Count; i++)
                {
                    ui.SetVisible(player, RowPrefix + i, true);
                    ui.SetText(player, NamePrefix + i, lines[i]);
                }
            });
        }

        internal void OnButton(UnturnedPlayer player, string screen, string button)
        {
            UiGuard.Run("waypoints.button:" + button, () =>
            {
                if (screen == "main" && button == MainCard)
                {
                    plugin.ShowWaypoints(player);
                    return;
                }

                if (screen != ScreenName) return;

                // Every index is resolved against freshly read server state, never the client value.
                if (button == StopButton)
                {
                    plugin.WaypointStop(player);
                    Refresh(player);
                    return;
                }

                if (UiGuard.TryParseIndex(button, TrackPrefix, out var index))
                {
                    plugin.WaypointTrack(player, index);
                    Refresh(player);
                    return;
                }

                if (UiGuard.TryParseIndex(button, DeletePrefix, out index))
                {
                    plugin.WaypointDelete(player, index);
                    Refresh(player);
                }
            });
        }
    }
}
