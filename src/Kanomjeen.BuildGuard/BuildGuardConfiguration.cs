using System.Collections.Generic;
using Rocket.API;

namespace Kanomjeen.BuildGuard
{
    public sealed class BuildGuardConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public int MaxBarricadesPerPlayer = 250;
        public int MaxStructuresPerPlayer = 250;
        public int MaxSentriesPerPlayer = 4;
        public int MaxBedsPerPlayer = 5;
        public int MaxClaimsPerPlayer = 2;
        public float CountCacheSeconds = 10f;
        public List<ushort> BlockedItemIds = new List<ushort>();
        public string MessageColor = "cyan";

        public void LoadDefaults()
        {
            Enabled = true;
            MaxBarricadesPerPlayer = 250;
            MaxStructuresPerPlayer = 250;
            MaxSentriesPerPlayer = 4;
            MaxBedsPerPlayer = 5;
            MaxClaimsPerPlayer = 2;
            CountCacheSeconds = 10f;
            BlockedItemIds = new List<ushort>();
            MessageColor = "cyan";
        }
    }
}
