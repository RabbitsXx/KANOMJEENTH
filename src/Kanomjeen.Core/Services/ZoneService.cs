using System;
using System.Collections.Generic;
using Kanomjeen.Core.Configuration;
using UnityEngine;

namespace Kanomjeen.Core.Services
{
    public enum ZoneFeature
    {
        TPA,
        Home,
        Build,
        Kit
    }

    public sealed class ZoneService
    {
        private readonly Func<IList<ZoneRule>> getZones;
        private readonly Dictionary<string, ZoneRule> dynamicZones = new Dictionary<string, ZoneRule>(StringComparer.Ordinal);

        public ZoneService(Func<IList<ZoneRule>> getZones)
        {
            this.getZones = getZones ?? throw new ArgumentNullException(nameof(getZones));
        }

        public void AddOrUpdateDynamic(string key, ZoneRule zone)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Dynamic zone key is required.", nameof(key));
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            dynamicZones[key] = zone;
        }

        public bool RemoveDynamic(string key) => !string.IsNullOrEmpty(key) && dynamicZones.Remove(key);
        public void ClearDynamic() => dynamicZones.Clear();

        public ZoneRule FindBlocking(Vector3 position, ZoneFeature feature)
        {
            var zones = getZones();
            if (zones != null)
            {
                for (var i = 0; i < zones.Count; i++)
                {
                    var found = Match(zones[i], position, feature);
                    if (found != null) return found;
                }
            }

            foreach (var pair in dynamicZones)
            {
                var found = Match(pair.Value, position, feature);
                if (found != null) return found;
            }
            return null;
        }

        private static ZoneRule Match(ZoneRule zone, Vector3 position, ZoneFeature feature)
        {
            if (zone == null || zone.Radius <= 0f || !Blocks(zone, feature)) return null;
            var dx = position.x - zone.X;
            var dy = position.y - zone.Y;
            var dz = position.z - zone.Z;
            return (dx * dx) + (dy * dy) + (dz * dz) <= zone.Radius * zone.Radius ? zone : null;
        }

        public bool IsBlocked(Vector3 position, ZoneFeature feature) => FindBlocking(position, feature) != null;

        private static bool Blocks(ZoneRule zone, ZoneFeature feature)
        {
            switch (feature)
            {
                case ZoneFeature.TPA: return zone.BlockTPA || zone.Deadzone || zone.HighTier || zone.AirdropObjective;
                case ZoneFeature.Home: return zone.BlockHome || zone.Deadzone || zone.HighTier || zone.AirdropObjective;
                case ZoneFeature.Build: return zone.BlockBuild || zone.Safezone || zone.AirdropObjective;
                case ZoneFeature.Kit: return zone.BlockKit || zone.Deadzone || zone.HighTier || zone.AirdropObjective;
                default: return false;
            }
        }
    }
}
