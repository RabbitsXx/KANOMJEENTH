using System;
using System.Collections.Generic;
using System.Globalization;
using Rocket.Unturned.Player;
using Logger = Rocket.Core.Logging.Logger;
using SDG.Unturned;
using UnityEngine;

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
        private readonly Dictionary<string, int> hudZoom = new Dictionary<string, int>(StringComparer.Ordinal);

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
            hudZoom[player.Id] = 50;
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
            ShowNativeSurvivalWidgets(player);
            SetVisible(player, "KJ_Hud", false);
            SendVisibility(player, "KJ_Dim", false);
            SendVisibility(player, "KJ_Root", false);
        }

        private static void ShowNativeSurvivalWidgets(UnturnedPlayer player)
        {
            if (player?.Player == null) return;
            player.Player.setPluginWidgetFlag(EPluginWidgetFlags.ShowHealth, true);
            player.Player.setPluginWidgetFlag(EPluginWidgetFlags.ShowFood, true);
            player.Player.setPluginWidgetFlag(EPluginWidgetFlags.ShowWater, true);
            player.Player.setPluginWidgetFlag(EPluginWidgetFlags.ShowVirus, true);
            player.Player.setPluginWidgetFlag(EPluginWidgetFlags.ShowStamina, true);
            player.Player.setPluginWidgetFlag(EPluginWidgetFlags.ShowOxygen, true);
        }

        /// <summary>True when this player currently holds an open Kanomjeen UI session.</summary>
        public bool IsOpen(UnturnedPlayer player) => player != null && IsOpen(player.Id);

        /// <summary>True when this player id currently holds an open Kanomjeen UI session.</summary>
        public bool IsOpen(string playerId) => !string.IsNullOrEmpty(playerId) && activeScreens.ContainsKey(playerId);

        public bool IsHudActive(UnturnedPlayer player) => player != null && activeHud.Contains(player.Id);

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

        private void SetHudBar(UnturnedPlayer player, string suffix, byte value)
        {
            var filled = Math.Min(10, (value + 9) / 10);
            for (var i = 0; i < 10; i++) SetVisible(player, "KJ_Hud_" + suffix + "_Bar_" + i, i < filled);
        }

        public void AdjustHudZoom(UnturnedPlayer player, int delta)
        {
            if (player?.Player == null || !activeHud.Contains(player.Id)) return;
            if (!hudZoom.TryGetValue(player.Id, out var zoom)) zoom = 50;
            hudZoom[player.Id] = Math.Max(40, Math.Min(60, zoom + (delta * 10)));
            SetHudZoomVisibility(player, hudZoom[player.Id]);
        }

        private void SetHudZoomVisibility(UnturnedPlayer player, int zoom)
        {
            SetVisible(player, "KJ_Minimap_Image_Zoom_40", zoom == 40);
            SetVisible(player, "KJ_Minimap_Image_Zoom_50", zoom == 50);
            SetVisible(player, "KJ_Minimap_Image_Zoom_60", zoom == 60);
        }

        public void PushHud(UnturnedPlayer player, HudSnapshot snapshot)
        {
            if (!IsConfigured || player?.Player == null || snapshot == null || !activeHud.Contains(player.Id)) return;
            SetHudZoomVisibility(player, hudZoom.TryGetValue(player.Id, out var zoom) ? zoom : 50);
            SetText(player, "KJ_Map_Bearing", snapshot.Bearing.ToString("000") + "°");
            SetText(player, "KJ_Map_Direction", snapshot.Direction);
            SetText(player, "KJ_Map_Coords", "X " + snapshot.X.ToString("0") + "  Y " + snapshot.Y.ToString("0") + "  Z " + snapshot.Z.ToString("0"));
            SetVisible(player, "KJ_Map_PlayerMarker", false);
            var directions = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            for (var i = 0; i < directions.Length; i++) SetVisible(player, "KJ_Map_PlayerMarker_" + directions[i], false);
        }

        public void SetWaypointTarget(UnturnedPlayer player, Vector3 target)
        {
            if (player?.Player == null) return;
            SetText(player, "KJ_Map_Waypoint_Target", string.Format(CultureInfo.InvariantCulture, "{0:0.0}|{1:0.0}|{2:0.0}", target.x, target.y, target.z));
        }

        public void ClearWaypointTarget(UnturnedPlayer player)
        {
            if (player?.Player == null) return;
            SetText(player, "KJ_Map_Waypoint_Target", string.Empty);
        }

        public string GetActiveScreen(string playerId)
        {
            return playerId != null && activeScreens.TryGetValue(playerId, out var screen) ? screen : null;
        }

        public void ClearPlayer(string playerId)
        {
            activeScreens.Remove(playerId);
            activeHud.Remove(playerId);
            hudZoom.Remove(playerId);
        }

        private void OnEffectButtonClicked(Player nativePlayer, string buttonName)
        {
            if (nativePlayer == null || string.IsNullOrEmpty(buttonName)) return;
            var player = UnturnedPlayer.FromPlayer(nativePlayer);
            if (player == null || (!activeScreens.TryGetValue(player.Id, out var screen) && !activeHud.Contains(player.Id))) return;
            if (string.IsNullOrEmpty(screen)) screen = "hud";
            if (buttonName == "KJ_Close")
            {
                Close(player);
                return;
            }
            ButtonClicked?.Invoke(this, new UiButtonEventArgs(player, screen, buttonName));
        }
    }
}
