using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Rocket.Core.Logging;

namespace Kanomjeen.AdminAudit
{
    [Serializable]
    public sealed class ModerationRecord
    {
        public string PlayerId;
        public string LastName;
        public int Warnings;
        public DateTime MutedUntilUtc = DateTime.MinValue;
        public string LastReason;
    }

    [Serializable]
    public sealed class ModerationDatabase
    {
        public List<ModerationRecord> Players = new List<ModerationRecord>();
    }

    public sealed class ModerationStore
    {
        private readonly Dictionary<string, ModerationRecord> records = new Dictionary<string, ModerationRecord>(StringComparer.Ordinal);
        private readonly string path;
        private bool dirty;

        public ModerationStore(string path) { this.path = path; Load(); }

        public ModerationRecord Get(string id, string name = null)
        {
            if (!records.TryGetValue(id, out var record))
            {
                record = new ModerationRecord { PlayerId = id, LastName = name ?? id };
                records[id] = record;
                dirty = true;
            }
            if (!string.IsNullOrWhiteSpace(name) && record.LastName != name) { record.LastName = name; dirty = true; }
            return record;
        }

        public bool IsMuted(string id, DateTime now, out TimeSpan remaining)
        {
            var record = Get(id);
            if (record.MutedUntilUtc > now) { remaining = record.MutedUntilUtc - now; return true; }
            if (record.MutedUntilUtc != DateTime.MinValue) { record.MutedUntilUtc = DateTime.MinValue; dirty = true; }
            remaining = TimeSpan.Zero; return false;
        }

        public void MarkDirty() => dirty = true;
        public void SaveIfDirty() { if (dirty) Save(); }

        public void Save()
        {
            try
            {
                var db = new ModerationDatabase { Players = new List<ModerationRecord>(records.Values) };
                var dir = Path.GetDirectoryName(path); if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var temp = path + ".tmp";
                var serializer = new XmlSerializer(typeof(ModerationDatabase));
                using (var stream = File.Create(temp)) serializer.Serialize(stream, db);
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path); dirty = false;
            }
            catch (Exception ex) { Logger.LogError("[Kanomjeen.AdminAudit] Moderation save failed: " + ex.Message); }
        }

        private void Load()
        {
            if (!File.Exists(path)) return;
            try
            {
                var serializer = new XmlSerializer(typeof(ModerationDatabase));
                using (var stream = File.OpenRead(path))
                {
                    var db = serializer.Deserialize(stream) as ModerationDatabase;
                    if (db?.Players == null) return;
                    foreach (var item in db.Players) if (item != null && !string.IsNullOrWhiteSpace(item.PlayerId)) records[item.PlayerId] = item;
                }
            }
            catch (Exception ex) { Logger.LogWarning("[Kanomjeen.AdminAudit] Moderation database could not be loaded: " + ex.Message); }
        }
    }
}
