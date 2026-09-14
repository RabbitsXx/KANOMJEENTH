using System;
using System.Collections.Generic;
using Kanomjeen.Core.Configuration;
using Kanomjeen.Core.Services;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;

namespace Kanomjeen.Core.Waypoints
{
    public sealed class WaypointService
    {
        private readonly WaypointStore store;
        private readonly Func<KanomjeenCoreConfiguration> config;

        public WaypointService(string path, Func<KanomjeenCoreConfiguration> config)
        {
            store = new WaypointStore(path);
            this.config = config;
        }

        public string CurrentMapId => string.IsNullOrWhiteSpace(Provider.map) ? "unknown" : Provider.map.Trim();

        public List<WaypointRecord> GetVisible(UnturnedPlayer player)
        {
            if (player == null) return new List<WaypointRecord>();
            return store.GetVisible(player.Id, CurrentMapId, IsStaff(player), DateTime.UtcNow);
        }

        public WaypointRecord CreatePlayer(UnturnedPlayer player, string name, WaypointIcon icon = WaypointIcon.Flag, WaypointColor color = WaypointColor.Gold)
        {
            if (player?.Player == null || !ValidateName(name, out var cleanName) || !ValidatePosition(player.Position)) return null;
            if (store.CountSaved(player.Id) >= EffectiveLimit(player)) return null;
            var waypoint = NewRecord(WaypointOwnerType.Player, player.Id, cleanName, player.Position, icon, color, WaypointVisibility.Owner, WaypointSource.Manual, null, DateTime.MinValue);
            store.Upsert(waypoint);
            return waypoint;
        }

        public WaypointRecord UpsertFeature(string ownerId, string name, Vector3 position, WaypointIcon icon, WaypointColor color,
            WaypointVisibility visibility, WaypointSource source, string sourceKey, DateTime expiresUtc, WaypointOwnerType ownerType = WaypointOwnerType.Player)
        {
            if (!ValidateName(name, out var cleanName) || !ValidatePosition(position) || string.IsNullOrWhiteSpace(sourceKey)) return null;
            var waypoint = store.FindBySource(ownerId, source, sourceKey) ?? NewRecord(ownerType, ownerId, cleanName, position, icon, color, visibility, source, sourceKey, expiresUtc);
            waypoint.Name = cleanName;
            waypoint.MapId = CurrentMapId;
            waypoint.X = position.x;
            waypoint.Y = position.y;
            waypoint.Z = position.z;
            waypoint.Icon = icon;
            waypoint.Color = color;
            waypoint.Visibility = visibility;
            waypoint.ExpiresUtc = expiresUtc;
            store.Upsert(waypoint);
            return waypoint;
        }

        public bool Track(UnturnedPlayer player, string waypointId)
        {
            if (player?.Player == null) return false;
            var waypoint = store.Find(waypointId);
            if (!CanSee(player, waypoint) || waypoint.IsExpired(DateTime.UtcNow) || !string.Equals(waypoint.MapId, CurrentMapId, StringComparison.OrdinalIgnoreCase)) return false;
            store.SetTrackedId(player.Id, waypoint.Id);
            ApplyNativeMarker(player, waypoint);
            return true;
        }

        public void StopTracking(UnturnedPlayer player)
        {
            if (player == null) return;
            store.SetTrackedId(player.Id, null);
            ClearNativeMarker(player);
        }

        public void Sync(UnturnedPlayer player)
        {
            if (player?.Player == null) return;
            var waypoint = store.Find(store.GetTrackedId(player.Id));
            if (!CanSee(player, waypoint) || waypoint.IsExpired(DateTime.UtcNow) || !string.Equals(waypoint.MapId, CurrentMapId, StringComparison.OrdinalIgnoreCase))
            {
                StopTracking(player);
                return;
            }
            ApplyNativeMarker(player, waypoint);
        }

        public bool IsTracked(string playerId, string waypointId)
        {
            return !string.IsNullOrEmpty(playerId) && !string.IsNullOrEmpty(waypointId) && store.GetTrackedId(playerId) == waypointId;
        }

        public WaypointRecord GetTracked(UnturnedPlayer player)
        {
            return player == null ? null : store.Find(store.GetTrackedId(player.Id));
        }

        public bool Rename(UnturnedPlayer player, string waypointId, string name)
        {
            var waypoint = store.Find(waypointId);
            if (!CanEdit(player, waypoint) || !ValidateName(name, out var cleanName)) return false;
            waypoint.Name = cleanName;
            store.Upsert(waypoint);
            if (IsTracked(player.Id, waypoint.Id)) ApplyNativeMarker(player, waypoint);
            return true;
        }

        public bool SetStyle(UnturnedPlayer player, string waypointId, WaypointIcon icon, WaypointColor color)
        {
            var waypoint = store.Find(waypointId);
            if (!CanEdit(player, waypoint)) return false;
            waypoint.Icon = icon;
            waypoint.Color = color;
            store.Upsert(waypoint);
            return true;
        }

        public bool Delete(UnturnedPlayer player, string waypointId)
        {
            var waypoint = store.Find(waypointId);
            if (!CanEdit(player, waypoint)) return false;
            var tracked = IsTracked(player.Id, waypoint.Id);
            if (!store.Remove(waypoint.Id)) return false;
            if (tracked) ClearNativeMarker(player);
            return true;
        }

        public void RemoveFeature(string ownerId, WaypointSource source, string sourceKey = null)
        {
            store.RemoveBySource(ownerId, source, sourceKey);
        }

        public void RemoveFeatureAndSync(UnturnedPlayer player, WaypointSource source, string sourceKey = null)
        {
            if (player == null) return;
            store.RemoveBySource(player.Id, source, sourceKey);
            Sync(player);
        }

        public List<string> SweepExpired() => store.SweepExpired(DateTime.UtcNow);
        public void SaveIfDirty() => store.SaveIfDirty();
        public void Save() => store.Save();

        public int EffectiveLimit(UnturnedPlayer player)
        {
            var baseline = Math.Max(1, config().WaypointSavedLimit);
            var maximum = Math.Max(baseline, config().WaypointMaximumPermissionLimit);
            for (var limit = maximum; limit > baseline; limit--)
                if (GameplayGuard.Has(player, "kanomjeen.waypoint.limit." + limit)) return limit;
            return baseline;
        }

        public static bool ValidateName(string name, out string cleanName)
        {
            cleanName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
            if (string.IsNullOrEmpty(cleanName) || cleanName.Length > 32) return false;
            for (var i = 0; i < cleanName.Length; i++)
                if (char.IsControl(cleanName[i]) || cleanName[i] == '<' || cleanName[i] == '>') return false;
            return true;
        }

        private bool ValidatePosition(Vector3 position)
        {
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z)) return false;
            if (float.IsInfinity(position.x) || float.IsInfinity(position.y) || float.IsInfinity(position.z)) return false;
            var bound = Math.Max(1024f, config().WaypointMaximumCoordinateMagnitude);
            return Math.Abs(position.x) <= bound && Math.Abs(position.y) <= bound && Math.Abs(position.z) <= bound;
        }

        private WaypointRecord NewRecord(WaypointOwnerType ownerType, string ownerId, string name, Vector3 position, WaypointIcon icon,
            WaypointColor color, WaypointVisibility visibility, WaypointSource source, string sourceKey, DateTime expiresUtc)
        {
            return new WaypointRecord
            {
                Id = Guid.NewGuid().ToString("N"), OwnerType = ownerType, OwnerId = ownerId, MapId = CurrentMapId,
                X = position.x, Y = position.y, Z = position.z, Name = name, Icon = icon, Color = color,
                Visibility = visibility, CreatedUtc = DateTime.UtcNow, ExpiresUtc = expiresUtc, Source = source, SourceKey = sourceKey
            };
        }

        private bool CanSee(UnturnedPlayer player, WaypointRecord waypoint)
        {
            if (player == null || waypoint == null) return false;
            if (waypoint.OwnerType == WaypointOwnerType.Player) return waypoint.OwnerId == player.Id;
            return waypoint.Visibility == WaypointVisibility.Everyone || (waypoint.Visibility == WaypointVisibility.Staff && IsStaff(player));
        }

        private bool CanEdit(UnturnedPlayer player, WaypointRecord waypoint)
        {
            if (player == null || waypoint == null || waypoint.IsTemporary) return false;
            if (waypoint.OwnerType == WaypointOwnerType.Player) return waypoint.OwnerId == player.Id;
            return IsStaff(player);
        }

        private static bool IsStaff(UnturnedPlayer player) => GameplayGuard.Has(player, "kanomjeen.waypoint.staff");

        private static void ApplyNativeMarker(UnturnedPlayer player, WaypointRecord waypoint)
        {
            if (player?.Player?.quests == null || waypoint == null) return;
            player.Player.quests.replicateSetMarker(true, new Vector3(waypoint.X, waypoint.Y, waypoint.Z), waypoint.Name);
        }

        private static void ClearNativeMarker(UnturnedPlayer player)
        {
            if (player?.Player?.quests == null) return;
            player.Player.quests.replicateSetMarker(false, Vector3.zero, string.Empty);
        }
    }
}
