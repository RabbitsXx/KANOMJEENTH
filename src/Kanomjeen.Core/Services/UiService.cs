using System;
using System.Collections.Generic;
using Rocket.Unturned.Player;
using SDG.Unturned;

namespace Kanomjeen.Core.Services
{
    public sealed class UiButtonEventArgs : EventArgs
    {
        public UnturnedPlayer Player { get; }
        public string Screen { get; }
        public string Button { get; }

        public UiButtonEventArgs(UnturnedPlayer player, string screen, string button)
        {
            Player = player;
            Screen = screen;
            Button = button;
        }
    }

    public sealed class UiService
    {
        private readonly ushort effectId;
        private readonly short key;
        private readonly string contractVersion;
        private readonly Dictionary<string, string> activeScreens = new Dictionary<string, string>(StringComparer.Ordinal);

        public event EventHandler<UiButtonEventArgs> ButtonClicked;

        public UiService(ushort effectId, short key, string contractVersion)
        {
            this.effectId = effectId;
            this.key = key;
            this.contractVersion = string.IsNullOrWhiteSpace(contractVersion) ? "1.0" : contractVersion;
        }

        public bool IsConfigured => effectId != 0;

        public void Subscribe() => EffectManager.onEffectButtonClicked += OnEffectButtonClicked;
        public void Unsubscribe() => EffectManager.onEffectButtonClicked -= OnEffectButtonClicked;

        public bool Open(UnturnedPlayer player, string screen, string title = null, string status = null)
        {
            if (!IsConfigured || player?.Player == null) return false;
            EffectManager.sendUIEffect(effectId, key, player.CSteamID, true);
            player.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, true);
            screen = string.IsNullOrWhiteSpace(screen) ? "main" : screen.Trim().ToLowerInvariant();
            activeScreens[player.Id] = screen;
            SetText(player, "KJ_Title", title ?? string.Empty);
            SetText(player, "KJ_Status", status ?? string.Empty);
            SetText(player, "KJ_ContractVersion", contractVersion);
            var screens = new[] { "main", "tpa", "homes", "kits", "stats", "airdrop", "admin", "toast" };
            for (var i = 0; i < screens.Length; i++) SetVisible(player, "KJ_Screen_" + screens[i], screens[i] == screen);
            return true;
        }

        public void Close(UnturnedPlayer player)
        {
            if (player?.Player == null) return;
            if (effectId != 0) EffectManager.askEffectClearByID(effectId, player.CSteamID);
            player.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, false);
            activeScreens.Remove(player.Id);
        }

        public void SetText(UnturnedPlayer player, string childName, string text)
        {
            if (!IsConfigured || player?.Player == null || string.IsNullOrEmpty(childName)) return;
            EffectManager.sendUIEffectText(key, player.CSteamID, true, childName, text ?? string.Empty);
        }

        public void SetVisible(UnturnedPlayer player, string childName, bool visible)
        {
            if (!IsConfigured || player?.Player == null || string.IsNullOrEmpty(childName)) return;
            EffectManager.sendUIEffectVisibility(key, player.CSteamID, true, childName, visible);
        }

        public string GetActiveScreen(string playerId)
        {
            return playerId != null && activeScreens.TryGetValue(playerId, out var screen) ? screen : null;
        }

        public void ClearPlayer(string playerId) => activeScreens.Remove(playerId);

        private void OnEffectButtonClicked(Player nativePlayer, string buttonName)
        {
            if (nativePlayer == null || string.IsNullOrEmpty(buttonName)) return;
            var player = UnturnedPlayer.FromPlayer(nativePlayer);
            if (player == null || !activeScreens.TryGetValue(player.Id, out var screen)) return;
            if (buttonName == "KJ_Close")
            {
                Close(player);
                return;
            }
            ButtonClicked?.Invoke(this, new UiButtonEventArgs(player, screen, buttonName));
        }
    }
}
