using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Rocket.Core.Logging;

namespace Kanomjeen.Core.Waypoints
{
    public sealed class WaypointStore
    {
        private const int CurrentSchemaVersion = 1;
        private readonly string path;
        private readonly Dictionary<string, WaypointRecord> waypoints = new Dictionary<string, WaypointRecord>(StringComparer.Ordinal);
        private readonly Dictionary<string, PlayerWaypointState> players = new Dictionary<string, PlayerWaypointState>(StringComparer.Ordinal);
        private bool dirty;

        public WaypointStore(string path)
        {
            this.path = path;
            Load();
        }

        public List<WaypointRecord> GetVisible(string playerId, string mapId, bool isStaff, DateTime nowUtc)
        {
            var result = new List<WaypointRecord>();
            foreach (var waypoint in waypoints.Values)
            {
                if (waypoint == null || waypoint.IsExpired(nowUtc) || !string.Equals(waypoint.MapId, mapId, StringComparison.OrdinalIgnoreCase)) continue;
                if (waypoint.OwnerType == WaypointOwnerType.Player)
                {
                    if (waypoint.OwnerId == playerId) result.Add(waypoint);
                    continue;
                }
                if (waypoint.Visibility == WaypointVisibility.Everyone || (isStaff && waypoint.Visibility == WaypointVisibility.Staff)) result.Add(waypoint);
            }
            result.Sort((a, b) => b.CreatedUtc.CompareTo(a.CreatedUtc));
            return result;
        }

        public int CountSaved(string playerId)
        {
            var count = 0;
            foreach (var waypoint in waypoints.Values)
                if (waypoint != null && waypoint.OwnerType == WaypointOwnerType.Player && waypoint.OwnerId == playerId && !waypoint.IsTemporary) count++;
            return count;
        }

        public WaypointRecord Find(string id)
        {
            return !string.IsNullOrEmpty(id) && waypoints.TryGetValue(id, out var value) ? value : null;
        }

        public WaypointRecord FindBySource(string ownerId, WaypointSource source, string sourceKey)
        {
            foreach (var waypoint in waypoints.Values)
                if (waypoint != null && waypoint.OwnerId == ownerId && waypoint.Source == source && string.Equals(waypoint.SourceKey, sourceKey, StringComparison.Ordinal)) return waypoint;
            return null;
        }

        public void Upsert(WaypointRecord waypoint)
        {
            if (waypoint == null || string.IsNullOrEmpty(waypoint.Id)) return;
            waypoints[waypoint.Id] = waypoint;
            dirty = true;
        }

        public bool Remove(string id)
        {
            if (string.IsNullOrEmpty(id) || !waypoints.Remove(id)) return false;
            foreach (var state in players.Values)
                if (state.TrackedWaypointId == id) state.TrackedWaypointId = null;
            dirty = true;
            return true;
        }

        public int RemoveBySource(string ownerId, WaypointSource source, string sourceKey = null)
        {
            var ids = new List<string>();
            foreach (var waypoint in waypoints.Values)
            {
                if (waypoint == null || waypoint.OwnerId != ownerId || waypoint.Source != source) continue;
                if (sourceKey != null && !string.Equals(waypoint.SourceKey, sourceKey, StringComparison.Ordinal)) continue;
                ids.Add(waypoint.Id);
            }
            foreach (var id in ids) Remove(id);
            return ids.Count;
        }

        public string GetTrackedId(string playerId)
        {
            return playerId != null && players.TryGetValue(playerId, out var state) ? state.TrackedWaypointId : null;
        }

        public void SetTrackedId(string playerId, string waypointId)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            if (!players.TryGetValue(playerId, out var state))
            {
                state = new PlayerWaypointState { PlayerId = playerId };
                players[playerId] = state;
            }
            state.TrackedWaypointId = waypointId;
            dirty = true;
        }

        public List<string> SweepExpired(DateTime nowUtc)
        {
            var expired = new List<string>();
            foreach (var waypoint in waypoints.Values)
                if (waypoint != null && waypoint.IsExpired(nowUtc)) expired.Add(waypoint.Id);
            foreach (var id in expired) Remove(id);
            return expired;
        }

        public void SaveIfDirty()
        {
            if (dirty) Save();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                var database = new WaypointDatabase
                {
                    SchemaVersion = CurrentSchemaVersion,
                    Waypoints = new List<WaypointRecord>(waypoints.Values),
                    Players = new List<PlayerWaypointState>(players.Values)
                };
                var temporaryPath = path + ".tmp";
                var serializer = new XmlSerializer(typeof(WaypointDatabase));
                using (var stream = File.Create(temporaryPath)) serializer.Serialize(stream, database);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temporaryPath, path);
                dirty = false;
            }
            catch (Exception ex)
            {
                Logger.LogError("[Kanomjeen.Waypoints] Save failed: " + ex.Message);
            }
        }

        private void Load()
        {
            if (!File.Exists(path)) return;
            try
            {
                var serializer = new XmlSerializer(typeof(WaypointDatabase));
                WaypointDatabase database;
                using (var stream = File.OpenRead(path)) database = serializer.Deserialize(stream) as WaypointDatabase;
                if (database == null) return;
                if (database.SchemaVersion > CurrentSchemaVersion)
                    throw new InvalidDataException("Database schema " + database.SchemaVersion + " is newer than supported schema " + CurrentSchemaVersion + ".");
                if (database.Waypoints != null)
                    foreach (var waypoint in database.Waypoints)
                        if (waypoint != null && !string.IsNullOrEmpty(waypoint.Id)) waypoints[waypoint.Id] = waypoint;
                if (database.Players != null)
                    foreach (var player in database.Players)
                        if (player != null && !string.IsNullOrEmpty(player.PlayerId)) players[player.PlayerId] = player;
                if (database.SchemaVersion < CurrentSchemaVersion) dirty = true;
            }
            catch (Exception ex)
            {
                try
                {
                    var backup = path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                    File.Move(path, backup);
                    Logger.LogWarning("[Kanomjeen.Waypoints] Database was unreadable and moved to '" + backup + "': " + ex.Message);
                }
                catch (Exception backupEx)
                {
                    Logger.LogError("[Kanomjeen.Waypoints] Database load failed and could not be backed up: " + backupEx.Message);
                }
            }
        }
    }
}
