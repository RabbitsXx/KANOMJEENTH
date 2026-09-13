using Kanomjeen.Core.Services;
using Rocket.Unturned.Player;

namespace Kanomjeen.Homes
{
    /// <summary>
    /// Every Homes client-UI operation lives here: entry, refresh and button routing. Gameplay stays
    /// in <see cref="KanomjeenHomesPlugin"/> and this class only presents state and forwards intents,
    /// which mirrors the UI-layer structure this suite follows (see AGENTS.md §13).
    /// </summary>
    internal sealed class KanomjeenHomesUi
    {
        private const string ScreenName = "homes";
        private const string RowPrefix = "KJ_Home_Row_";
        private const string NamePrefix = "KJ_Home_Name_";
        private const string TrackPrefix = "KJ_Home_Track_";
        private const string TeleportPrefix = "KJ_Home_Teleport_";
        private const string DeletePrefix = "KJ_Home_Delete_";
        private const string AddButton = "KJ_Home_Add";
        private const string MainCard = "KJ_Main_Homes";
        private const int RowCount = 6;

        private const string Title = "KANOMJEEN • HOMES";
        private const string Status = "Travel is disabled during combat, raids and restricted objectives.";

        private readonly KanomjeenHomesPlugin plugin;

        internal KanomjeenHomesUi(KanomjeenHomesPlugin plugin)
        {
            this.plugin = plugin;
        }

        internal void Show(UnturnedPlayer player)
        {
            UiGuard.Run("homes.show", () =>
            {
                var ui = plugin.Ui;
                if (ui == null || !ui.IsConfigured) return;

                ui.ShowScreen(player, ScreenName, Title, Status);
                Refresh(player);
            });
        }

        internal void Refresh(UnturnedPlayer player)
        {
            UiGuard.Run("homes.refresh", () =>
            {
                var ui = plugin.Ui;
                if (ui == null || !ui.IsOpen(player)) return;

                var homes = plugin.HomeNames(player.Id);
                ui.HideRows(player, RowPrefix, RowCount);
                for (var i = 0; i < RowCount && i < homes.Count; i++)
                {
                    ui.SetVisible(player, RowPrefix + i, true);
                    ui.SetText(player, NamePrefix + i, homes[i]);
                }
            });
        }

        internal void OnButton(UnturnedPlayer player, string screen, string button)
        {
            UiGuard.Run("homes.button:" + button, () =>
            {
                if (screen == "main" && button == MainCard)
                {
                    Show(player);
                    return;
                }

                if (screen != ScreenName) return;
                if (!plugin.AllowUiAction(player)) return;

                // Row index is resolved against freshly read server state, never against the client.
                var homes = plugin.HomeNames(player.Id);
                if (UiGuard.TryParseIndex(button, TrackPrefix, out var index) && InRange(index, homes.Count))
                {
                    plugin.TrackHome(player, homes[index]);
                    return;
                }

                if (UiGuard.TryParseIndex(button, TeleportPrefix, out index) && InRange(index, homes.Count))
                {
                    plugin.TeleportHome(player, homes[index]);
                    return;
                }

                if (UiGuard.TryParseIndex(button, DeletePrefix, out index) && InRange(index, homes.Count))
                {
                    plugin.DeleteHome(player, homes[index]);
                    Show(player);
                    return;
                }

                if (button == AddButton) plugin.HintAdd(player);
            });
        }

        private static bool InRange(int index, int count) => index >= 0 && index < count;
    }
}
