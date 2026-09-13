using Kanomjeen.Core.Services;
using Rocket.Unturned.Player;

namespace Kanomjeen.TPA
{
    /// <summary>
    /// All TPA client UI: the idle screen, the incoming-request panel and button routing. Request
    /// state, permission and teleport rules stay in <see cref="KanomjeenTpaPlugin"/>; this class only
    /// presents what the plugin decided and forwards accept/deny/cancel intents (see AGENTS.md §13).
    /// </summary>
    internal sealed class KanomjeenTpaUi
    {
        private const string ScreenName = "tpa";
        private const string MainCard = "KJ_Main_TPA";
        private const string RequestPanel = "KJ_TPA_RequestPanel";
        private const string RequesterElement = "KJ_TPA_Requester";
        private const string TimerElement = "KJ_TPA_Timer";
        private const string AcceptButton = "KJ_TPA_Accept";
        private const string DenyButton = "KJ_TPA_Deny";
        private const string CancelButton = "KJ_TPA_Cancel";

        private const string Title = "KANOMJEEN • TPA";
        private const string IdleStatus = "Use /tpa <player> or /tpahere <player>. Requests appear here.";
        private const string IncomingStatus = "Teleport request";
        private const string IncomingHereStatus = "Teleport here request";

        private readonly KanomjeenTpaPlugin plugin;

        internal KanomjeenTpaUi(KanomjeenTpaPlugin plugin)
        {
            this.plugin = plugin;
        }

        /// <summary>Enter the TPA screen with no request showing (main-menu card, /tpa screen entry).</summary>
        internal void ShowIdle(UnturnedPlayer player)
        {
            UiGuard.Run("tpa.idle", () =>
            {
                var ui = plugin.Ui;
                if (ui == null || !ui.IsConfigured) return;

                ui.ShowScreen(player, ScreenName, Title, IdleStatus);
                ui.SetVisible(player, RequestPanel, false);
            });
        }

        /// <summary>
        /// Surface an incoming request. This deliberately uses Open() rather than visibility-only
        /// navigation: the target is being interrupted by a new panel, so the client is handed a fresh
        /// Effect instance and cannot keep a stale screen rendered underneath it.
        /// </summary>
        internal void ShowRequest(UnturnedPlayer target, string requesterName, float seconds, bool teleportToRequester)
        {
            UiGuard.Run("tpa.request", () =>
            {
                var ui = plugin.Ui;
                if (ui == null || !ui.IsConfigured) return;

                ui.Open(target, ScreenName, Title, teleportToRequester ? IncomingHereStatus : IncomingStatus);
                ui.SetVisible(target, RequestPanel, true);
                ui.SetText(target, RequesterElement, requesterName ?? string.Empty);
                ui.SetText(target, TimerElement, System.Math.Ceiling(seconds) + "s");
            });
        }

        internal void OnButton(UnturnedPlayer player, string screen, string button)
        {
            UiGuard.Run("tpa.button:" + button, () =>
            {
                if (screen == "main" && button == MainCard)
                {
                    ShowIdle(player);
                    return;
                }

                if (screen != ScreenName) return;
                if (!plugin.AllowUiAction(player)) return;

                // Every action re-reads request state on the server; the button is only an intent.
                if (button == AcceptButton) plugin.AcceptLatest(player);
                else if (button == DenyButton) plugin.DenyLatest(player);
                else if (button == CancelButton) plugin.CancelOutgoing(player);
            });
        }
    }
}
