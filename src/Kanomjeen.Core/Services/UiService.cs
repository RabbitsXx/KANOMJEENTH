using System;
using System.Collections.Generic;
using Rocket.Unturned.Player;
using Logger = Rocket.Core.Logging.Logger;
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
        private readonly HashSet<string> activeHud = new HashSet<string>(StringComparer.Ordinal);

        public event EventHandler<UiButtonEventArgs> ButtonClicked;

        // Single source of truth for the prefab's KJ_Screen_* containers. Must stay in sync with
        // KanomjeenUiBuilder.Screen(...) in the Unity project; verify-source.sh/.ps1 checks both sides.
        internal static readonly string[] Screens =
        {
            "main", "waypoints", "tpa", "homes", "kits", "stats", "airdrop", "admin", "toast"
        };

        public UiService(ushort effectId, short key, string contractVersion)
        {
            this.effectId = effectId;
            this.key = key;
            this.contractVersion = string.IsNullOrWhiteSpace(contractVersion) ? "1.0" : contractVersion;
        }

        public bool IsConfigured => effectId != 0;

        public void Subscribe()
        {
            if (IsConfigured) EffectManager.onEffectButtonClicked += OnEffectButtonClicked;
        }

        public void Unsubscribe()
        {
            if (IsConfigured) EffectManager.onEffectButtonClicked -= OnEffectButtonClicked;
        }

        public bool Open(UnturnedPlayer player, string screen, string title = null, string status = null)
        {
            if (!IsConfigured || player?.Player == null) return false;

            // An explicit open always hands the client a fresh Effect instance. Visibility-only
            // navigation can desync when a client keeps a stale copy (plugin reload, missed packet,
            // older bundle), and a desynced copy renders two screen containers on top of each other.
            // The asset itself is cached client-side, so clear+send is cheap.
            UiGuard.Run("open.clear+send", () =>
            {
                EffectManager.askEffectClearByID(effectId, player.CSteamID);
                EffectManager.sendUIEffect(effectId, key, player.CSteamID, true);
            });

            player.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, true);
            activeScreens[player.Id] = Normalize(screen);
            activeHud.Add(player.Id);
            FocusScreen(player, title, status);
            // FocusScreen() has already reset visibility for every known screen container.
            return true;
        }

        /// <summary>
        /// Switch or refresh a screen inside an already open session. Reuses the client's Effect
        /// instance (visibility and text only), so it is explicit about every KJ_Screen_ container.
        /// Falls back to <see cref="Open"/> when the player has no session yet, which makes it safe
        /// to call from both commands and GUI navigation.
        /// </summary>
        public bool ShowScreen(UnturnedPlayer player, string screen, string title = null, string status = null)
        {
            if (!IsConfigured || player?.Player == null) return false;
            if (!activeScreens.ContainsKey(player.Id)) return Open(player, screen, title, status);
            activeScreens[player.Id] = Normalize(screen);
            FocusScreen(player, title, status);
            return true;
        }

        private static string Normalize(string screen)
        {
            return string.IsNullOrWhiteSpace(screen) ? "main" : screen.Trim().ToLowerInvariant();
        }

        private void FocusScreen(UnturnedPlayer player, string title, string status)
        {
            var screen = activeScreens[player.Id];
            if (Array.IndexOf(Screens, screen) < 0)
            {
                // A screen name without a matching KJ_Screen_ container leaves the previously visible
                // screen rendered on top of the new content, which players read as a broken layout.
                Logger.LogError("[Kanomjeen.Core] UI screen '" + screen + "' has no KJ_Screen_ container in the Workshop prefab. Known screens: " + string.Join(", ", Screens));
                screen = "main";
                activeScreens[player.Id] = screen;
            }

            SetText(player, "KJ_Title", title ?? string.Empty);
            SetText(player, "KJ_Status", status ?? string.Empty);
            SetText(player, "KJ_ContractVersion", contractVersion);
            for (var i = 0; i < Screens.Length; i++) SetVisible(player, "KJ_Screen_" + Screens[i], Screens[i] == screen);
        }

        public void Close(UnturnedPlayer player)
        {
            if (player?.Player == null) return;
            activeScreens.Remove(player.Id);
            if (activeHud.Contains(player.Id))
            {
                ShowHud(player);
                return;
            }
            if (effectId != 0) EffectManager.askEffectClearByID(effectId, player.CSteamID);
            player.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, false);
        }

        public void ShowHud(UnturnedPlayer player)
        {
            if (!IsConfigured || player?.Player == null) return;
            UiGuard.Run("hud.clear+send", () =>
            {
                EffectManager.askEffectClearByID(effectId, player.CSteamID);
                EffectManager.sendUIEffect(effectId, key, player.CSteamID, true);
            });
            activeHud.Add(player.Id);
            activeScreens.Remove(player.Id);
            player.Player.setPluginWidgetFlag(EPluginWidgetFlags.Modal, false);
            SendVisibility(player, "KJ_Dim", false);
            SendVisibility(player, "KJ_Root", false);
        }

        /// <summary>True when this player currently holds an open Kanomjeen UI session.</summary>
        public bool IsOpen(UnturnedPlayer player) => player != null && IsOpen(player.Id);

        /// <summary>True when this player id currently holds an open Kanomjeen UI session.</summary>
        public bool IsOpen(string playerId) => !string.IsNullOrEmpty(playerId) && activeScreens.ContainsKey(playerId);

        /// <summary>
        /// Hide every reusable row of a list before the screen is repopulated. Feature UI classes
        /// must never let a row keep another player's (or an earlier refresh's) content.
        /// </summary>
        public void HideRows(UnturnedPlayer player, string rowPrefix, int count)
        {
            for (var i = 0; i < count; i++) SetVisible(player, rowPrefix + i, false);
        }

        public void SetText(UnturnedPlayer player, string childName, string text)
        {
            if (!IsConfigured || player?.Player == null || string.IsNullOrEmpty(childName)) return;
            if (!IsOpen(player.Id) && !activeHud.Contains(player.Id)) return;
            UiGuard.Run("text:" + childName, () => EffectManager.sendUIEffectText(key, player.CSteamID, true, childName, text ?? string.Empty));
        }

        public void SetVisible(UnturnedPlayer player, string childName, bool visible)
        {
            if (!IsConfigured || player?.Player == null || string.IsNullOrEmpty(childName)) return;
            if (!IsOpen(player.Id) && !activeHud.Contains(player.Id)) return;
            SendVisibility(player, childName, visible);
        }

        private void SendVisibility(UnturnedPlayer player, string childName, bool visible)
        {
            if (!IsConfigured || player?.Player == null || string.IsNullOrEmpty(childName)) return;
            UiGuard.Run("visibility:" + childName, () => EffectManager.sendUIEffectVisibility(key, player.CSteamID, true, childName, visible));
        }

        public void PushHud(UnturnedPlayer player, HudSnapshot snapshot)
        {
            if (!IsConfigured || player?.Player == null || snapshot == null || !activeHud.Contains(player.Id)) return;
            SetText(player, "KJ_Hud_Health", snapshot.Health.ToString());
            SetText(player, "KJ_Hud_Food", snapshot.Food.ToString());
            SetText(player, "KJ_Hud_Water", snapshot.Water.ToString());
            SetText(player, "KJ_Hud_Virus", snapshot.Virus.ToString());
            SetText(player, "KJ_Hud_Stamina", snapshot.Stamina.ToString());
            SetText(player, "KJ_Hud_Oxygen", snapshot.Oxygen.ToString());
            SetText(player, "KJ_Map_Bearing", snapshot.Bearing.ToString("000") + "°");
            SetText(player, "KJ_Map_Direction", snapshot.Direction);
            SetText(player, "KJ_Map_Coords", "X " + snapshot.X.ToString("0") + "  Y " + snapshot.Y.ToString("0") + "  Z " + snapshot.Z.ToString("0"));
        }

        public string GetActiveScreen(string playerId)
        {
            return playerId != null && activeScreens.TryGetValue(playerId, out var screen) ? screen : null;
        }

        public void ClearPlayer(string playerId)
        {
            activeScreens.Remove(playerId);
            activeHud.Remove(playerId);
        }

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
