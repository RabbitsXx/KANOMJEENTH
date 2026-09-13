using System.Collections.Generic;
using Rocket.API;

namespace Kanomjeen.Airdrops
{
    public sealed class AirdropSpawn
    {
        public string Name = "spawn";
        public ushort AirdropId;
        public float X;
        public float Y;
        public float Z;
    }

    public sealed class AirdropConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public bool AutoEnabled = true;
        public int MinPlayers = 5;
        public float MinIntervalSeconds = 2400f;
        public float MaxIntervalSeconds = 3600f;
        public float AirdropSpeed = 128f;
        public float ObjectiveRadius = 350f;
        public float ObjectiveLifetimeSeconds = 900f;
        public bool BlockBuildInObjective = true;
        public string MessageColor = "yellow";
        public List<AirdropSpawn> Spawns = new List<AirdropSpawn>();

        public void LoadDefaults()
        {
            Enabled = true;
            AutoEnabled = true;
            MinPlayers = 5;
            MinIntervalSeconds = 2400f;
            MaxIntervalSeconds = 3600f;
            AirdropSpeed = 128f;
            ObjectiveRadius = 350f;
            ObjectiveLifetimeSeconds = 900f;
            BlockBuildInObjective = true;
            MessageColor = "yellow";
            Spawns = new List<AirdropSpawn>();
        }
    }
}
