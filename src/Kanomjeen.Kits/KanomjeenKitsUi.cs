using Kanomjeen.Core.Services;
using Rocket.Unturned.Player;

namespace Kanomjeen.Kits
{
    /// <summary>
    /// All Kits client UI: entry, refresh and button routing. Kit definitions, permissions and
    /// cooldown rules stay in <see cref="KanomjeenKitsPlugin"/>; this class presents rows and
    /// forwards claim intents (see AGENTS.md §13).
    /// </summary>
    internal sealed class KanomjeenKitsUi
    {
        private const string ScreenName = "kits";
        private const string RowPrefix = "KJ_Kit_Row_";
        private const string NamePrefix = "KJ_Kit_Name_";
        private const string CooldownPrefix = "KJ_Kit_Cooldown_";
        private const string ClaimPrefix = "KJ_Kit_Claim_";
        private const string MainCard = "KJ_Main_Kits";
        private const int RowCount = 8;

        private const string Title = "KANOMJEEN • KITS";
        private const string Status = "Survival utility only — no pay-to-win loadouts.";

        private readonly KanomjeenKitsPlugin plugin;

        internal KanomjeenKitsUi(KanomjeenKitsPlugin plugin)
        {
            this.plugin = plugin;
        }

        internal void Show(UnturnedPlayer player)
        {
            UiGuard.Run("kits.show", () =>
            {
                var ui = plugin.Ui;
                if (ui == null || !ui.IsConfigured) return;

                ui.ShowScreen(player, ScreenName, Title, Status);
                Refresh(player);
            });
        }

        internal void Refresh(UnturnedPlayer player)
        {
            UiGuard.Run("kits.refresh", () =>
            {
                var ui = plugin.Ui;
                if (ui == null || !ui.IsOpen(player)) return;

                var rows = plugin.KitRows(player);
                ui.HideRows(player, RowPrefix, RowCount);
                for (var i = 0; i < RowCount && i < rows.Count; i++)
                {
                    ui.SetVisible(player, RowPrefix + i, true);
                    ui.SetText(player, NamePrefix + i, rows[i].Name);
                    ui.SetText(player, CooldownPrefix + i, rows[i].Detail ?? string.Empty);
                }
            });
        }

        internal void OnButton(UnturnedPlayer player, string screen, string button)
        {
            UiGuard.Run("kits.button:" + button, () =>
            {
                if (screen == "main" && button == MainCard)
                {
                    Show(player);
                    return;
                }

                if (screen != ScreenName) return;
                if (!plugin.AllowUiAction(player)) return;

                if (!UiGuard.TryParseIndex(button, ClaimPrefix, out var index)) return;
                plugin.ClaimKitByIndex(player, index);
                Refresh(player);
            });
        }
    }
}
