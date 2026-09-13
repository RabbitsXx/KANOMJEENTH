using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Rocket.Core.Logging;

namespace Kanomjeen.Homes
{
    [Serializable]
    public sealed class HomeRecord
    {
        public string Name;
        public float X;
        public float Y;
        public float Z;
        public float Rotation;
        public DateTime CreatedUtc;
    }

    [Serializable]
    public sealed class PlayerHomes
    {
        public string PlayerId;
        public List<HomeRecord> Homes = new List<HomeRecord>();
    }

    [Serializable]
    public sealed class HomeDatabase
    {
        public List<PlayerHomes> Players = new List<PlayerHomes>();
    }

    public sealed class HomeStore
    {
        private readonly Dictionary<string, PlayerHomes> players = new Dictionary<string, PlayerHomes>(StringComparer.Ordinal);
        private readonly string path;
        private bool dirty;

        public HomeStore(string path) { this.path = path; Load(); }

        public List<HomeRecord> Get(string playerId) => GetPlayer(playerId).Homes;

        public HomeRecord Find(string playerId, string name)
        {
            var homes = Get(playerId);
            if (string.IsNullOrWhiteSpace(name)) return homes.Count > 0 ? homes[0] : null;
            foreach (var home in homes) if (string.Equals(home.Name, name, StringComparison.OrdinalIgnoreCase)) return home;
            return null;
        }

        public bool Add(string playerId, HomeRecord home, int limit, out string error)
        {
            var list = Get(playerId);
            foreach (var existing in list)
                if (string.Equals(existing.Name, home.Name, StringComparison.OrdinalIgnoreCase)) { error = "duplicate"; return false; }
            if (list.Count >= limit) { error = "limit"; return false; }
            list.Add(home); dirty = true; error = null; return true;
        }

        public bool Delete(string playerId, string name)
        {
            var list = Get(playerId);
            for (var i = 0; i < list.Count; i++)
                if (string.Equals(list[i].Name, name, StringComparison.OrdinalIgnoreCase)) { list.RemoveAt(i); dirty = true; return true; }
            return false;
        }

        public void SaveIfDirty() { if (dirty) Save(); }

        public void Save()
        {
            try
            {
                var db = new HomeDatabase { Players = new List<PlayerHomes>(players.Values) };
                var directory = Path.GetDirectoryName(path); if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                var tmp = path + ".tmp";
                var serializer = new XmlSerializer(typeof(HomeDatabase));
                using (var stream = File.Create(tmp)) serializer.Serialize(stream, db);
                if (File.Exists(path)) File.Delete(path); File.Move(tmp, path); dirty = false;
            }
            catch (Exception ex) { Logger.LogError("[Kanomjeen.Homes] Save failed: " + ex.Message); }
        }

        private PlayerHomes GetPlayer(string playerId)
        {
            if (!players.TryGetValue(playerId, out var data)) { data = new PlayerHomes { PlayerId = playerId }; players[playerId] = data; }
            return data;
        }

        private void Load()
        {
            if (!File.Exists(path)) return;
            try
            {
                var serializer = new XmlSerializer(typeof(HomeDatabase));
                using (var stream = File.OpenRead(path))
                {
                    var db = serializer.Deserialize(stream) as HomeDatabase;
                    if (db?.Players == null) return;
                    foreach (var p in db.Players) if (p != null && !string.IsNullOrEmpty(p.PlayerId)) players[p.PlayerId] = p;
                }
            }
            catch (Exception ex) { Logger.LogWarning("[Kanomjeen.Homes] Existing database could not be loaded: " + ex.Message); }
        }
    }
}
