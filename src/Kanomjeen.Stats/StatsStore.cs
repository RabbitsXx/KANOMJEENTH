using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Rocket.Core.Logging;

namespace Kanomjeen.Stats
{
    [Serializable]
    public sealed class PlayerStatsRecord
    {
        public string PlayerId;
        public string LastName;
        public int Kills;
        public int Deaths;
        public int Headshots;
        public int ZombieKills;
        public double PlaytimeSeconds;
        public double LongestLifeSeconds;
        public int AirdropsCaptured;
    }

    [Serializable]
    public sealed class StatsDatabase
    {
        public List<PlayerStatsRecord> Players = new List<PlayerStatsRecord>();
    }

    public sealed class StatsStore
    {
        private readonly Dictionary<string, PlayerStatsRecord> records = new Dictionary<string, PlayerStatsRecord>(StringComparer.Ordinal);
        private readonly string path;
        private bool dirty;

        public StatsStore(string path) { this.path = path; Load(); }

        public PlayerStatsRecord Get(string id, string name = null)
        {
            if (!records.TryGetValue(id, out var record))
            {
                record = new PlayerStatsRecord { PlayerId = id, LastName = name ?? id };
                records[id] = record;
                dirty = true;
            }
            if (!string.IsNullOrWhiteSpace(name) && record.LastName != name) { record.LastName = name; dirty = true; }
            return record;
        }

        public void MarkDirty() => dirty = true;
        public void SaveIfDirty() { if (dirty) Save(); }

        public void Save()
        {
            try
            {
                var db = new StatsDatabase { Players = new List<PlayerStatsRecord>(records.Values) };
                var dir = Path.GetDirectoryName(path); if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var tmp = path + ".tmp";
                var serializer = new XmlSerializer(typeof(StatsDatabase));
                using (var stream = File.Create(tmp)) serializer.Serialize(stream, db);
                if (File.Exists(path)) File.Delete(path);
                File.Move(tmp, path);
                dirty = false;
            }
            catch (Exception ex) { Logger.LogError("[Kanomjeen.Stats] Save failed: " + ex.Message); }
        }

        private void Load()
        {
            if (!File.Exists(path)) return;
            try
            {
                var serializer = new XmlSerializer(typeof(StatsDatabase));
                using (var stream = File.OpenRead(path))
                {
                    var db = serializer.Deserialize(stream) as StatsDatabase;
                    if (db?.Players == null) return;
                    foreach (var record in db.Players)
                        if (record != null && !string.IsNullOrWhiteSpace(record.PlayerId)) records[record.PlayerId] = record;
                }
            }
            catch (Exception ex) { Logger.LogWarning("[Kanomjeen.Stats] Database could not be loaded: " + ex.Message); }
        }
    }
}
