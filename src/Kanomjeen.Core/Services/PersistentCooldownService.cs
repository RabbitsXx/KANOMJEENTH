using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Rocket.Core.Logging;

namespace Kanomjeen.Core.Services
{
    [Serializable]
    public sealed class CooldownRecord
    {
        public string Key;
        public DateTime UntilUtc;
    }

    [Serializable]
    public sealed class CooldownFile
    {
        public List<CooldownRecord> Records = new List<CooldownRecord>();
    }

    public sealed class PersistentCooldownService
    {
        private readonly Dictionary<string, DateTime> values = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        private readonly string filePath;
        private bool dirty;

        public PersistentCooldownService(string filePath)
        {
            this.filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            Load();
        }

        public TimeSpan GetRemaining(string playerId, string feature, DateTime nowUtc)
        {
            var key = MakeKey(playerId, feature);
            if (!values.TryGetValue(key, out var until) || until <= nowUtc)
            {
                if (values.Remove(key)) dirty = true;
                return TimeSpan.Zero;
            }
            return until - nowUtc;
        }

        public void Set(string playerId, string feature, DateTime nowUtc, TimeSpan duration)
        {
            values[MakeKey(playerId, feature)] = nowUtc.Add(duration < TimeSpan.Zero ? TimeSpan.Zero : duration);
            dirty = true;
        }

        public void Clear(string playerId, string feature)
        {
            if (values.Remove(MakeKey(playerId, feature))) dirty = true;
        }

        public void SaveIfDirty()
        {
            if (!dirty) return;
            Save();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                var file = new CooldownFile();
                var now = DateTime.UtcNow;
                foreach (var pair in values)
                    if (pair.Value > now) file.Records.Add(new CooldownRecord { Key = pair.Key, UntilUtc = pair.Value });

                var serializer = new XmlSerializer(typeof(CooldownFile));
                var temp = filePath + ".tmp";
                using (var stream = File.Create(temp)) serializer.Serialize(stream, file);
                if (File.Exists(filePath)) File.Delete(filePath);
                File.Move(temp, filePath);
                dirty = false;
            }
            catch (Exception ex)
            {
                Logger.LogError("[Kanomjeen.Core] Failed to save cooldowns: " + ex.Message);
            }
        }

        private void Load()
        {
            if (!File.Exists(filePath)) return;
            try
            {
                var serializer = new XmlSerializer(typeof(CooldownFile));
                using (var stream = File.OpenRead(filePath))
                {
                    var file = serializer.Deserialize(stream) as CooldownFile;
                    if (file == null) return;
                    var now = DateTime.UtcNow;
                    foreach (var record in file.Records)
                        if (record != null && !string.IsNullOrEmpty(record.Key) && record.UntilUtc > now)
                            values[record.Key] = record.UntilUtc;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("[Kanomjeen.Core] Cooldown file could not be loaded; starting clean: " + ex.Message);
            }
        }

        private static string MakeKey(string playerId, string feature) => (playerId ?? string.Empty) + "|" + (feature ?? string.Empty);
    }
}
