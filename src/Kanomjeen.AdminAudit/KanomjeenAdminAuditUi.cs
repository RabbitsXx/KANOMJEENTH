using Kanomjeen.Core.Services;
using Rocket.Unturned.Player;

namespace Kanomjeen.AdminAudit
{
    /// <summary>
    /// All Staff client UI: entry, state refresh and button routing. Moderation state and permission
    /// checks stay in <see cref="KanomjeenAdminAuditPlugin"/>; this class only presents the staff
    /// snapshot and forwards god/vanish intents (see AGENTS.md §13).
    /// </summary>
    internal sealed class KanomjeenAdminAuditUi
    {
        private const string ScreenName = "admin";
        private const string MainCard = "KJ_Main_Admin";
        private const string NameElement = "KJ_Admin_Name";
        private const string StateElement = "KJ_Admin_State";
        private const string GodButton = "KJ_Admin_God";
        private const string VanishButton = "KJ_Admin_Vanish";

        private const string Title = "KANOMJEEN • STAFF";
        private const string Status = "Fast tools. Targeted actions remain chat commands for auditability.";

        private readonly KanomjeenAdminAuditPlugin plugin;

        internal KanomjeenAdminAuditUi(KanomjeenAdminAuditPlugin plugin)
        {
            this.plugin = plugin;
        }

        internal void Show(UnturnedPlayer player)
        {
            UiGuard.Run("admin.show", () =>
            {
                var ui = plugin.Ui;
                if (ui == null || !ui.IsConfigured) return;

                ui.ShowScreen(player, ScreenName, Title, Status);
                Apply(player, ui);
            });
        }

        internal void OnButton(UnturnedPlayer player, string screen, string button)
        {
            UiGuard.Run("admin.button:" + button, () =>
            {
                if (screen == "main" && button == MainCard)
                {
                    plugin.ShowStaffUi(player);
                    return;
                }

                if (screen != ScreenName) return;
                if (!plugin.AllowStaffUi(player)) return;

                if (button == GodButton) plugin.ToggleGod(player);
                else if (button == VanishButton) plugin.ToggleVanish(player);
                else return;

                // Toggle actions are chat commands; mirror the resulting state so the panel is honest.
                var ui = plugin.Ui;
                if (ui != null) Apply(player, ui);
            });
        }

        private void Apply(UnturnedPlayer player, UiService ui)
        {
            if (!ui.IsOpen(player)) return;
            ui.SetText(player, NameElement, player.DisplayName);
            ui.SetText(player, StateElement, plugin.AdminStateText(player));
        }
    }
}
