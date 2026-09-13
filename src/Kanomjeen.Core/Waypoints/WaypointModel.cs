using System;
using System.Collections.Generic;

namespace Kanomjeen.Core.Waypoints
{
    public enum WaypointOwnerType { Player, Global, Event }
    public enum WaypointIcon { Home, Flag, Crate, Safezone, Custom }
    public enum WaypointColor { Gold, Cyan, Red, Green, Purple }
    public enum WaypointVisibility { Owner, Staff, Everyone }
    public enum WaypointSource { Manual, Home, Tpa, Airdrop, Staff }

    [Serializable]
    public sealed class WaypointRecord
    {
        public string Id;
        public WaypointOwnerType OwnerType;
        public string OwnerId;
        public string MapId;
        public float X;
        public float Y;
        public float Z;
        public string Name;
        public WaypointIcon Icon;
        public WaypointColor Color;
        public WaypointVisibility Visibility;
        public DateTime CreatedUtc;
        public DateTime ExpiresUtc;
        public WaypointSource Source;
        public string SourceKey;

        public bool IsTemporary => ExpiresUtc > DateTime.MinValue;
        public bool IsExpired(DateTime nowUtc) => IsTemporary && ExpiresUtc <= nowUtc;
    }

    [Serializable]
    public sealed class PlayerWaypointState
    {
        public string PlayerId;
        public string TrackedWaypointId;
    }

    [Serializable]
    public sealed class WaypointDatabase
    {
        public int SchemaVersion = 1;
        public List<WaypointRecord> Waypoints = new List<WaypointRecord>();
        public List<PlayerWaypointState> Players = new List<PlayerWaypointState>();
    }
}
