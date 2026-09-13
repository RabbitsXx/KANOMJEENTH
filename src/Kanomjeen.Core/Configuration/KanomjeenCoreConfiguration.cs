using System.Collections.Generic;
using Rocket.API;

namespace Kanomjeen.Core.Configuration
{
    public sealed class ZoneRule
    {
        public string Name = "Restricted Zone";
        public float X;
        public float Y;
        public float Z;
        public float Radius = 100f;
        public bool BlockTPA = true;
        public bool BlockHome = true;
        public bool BlockBuild;
        public bool BlockKit;
        public bool HighTier;
        public bool Deadzone;
        public bool Safezone;
        public bool AirdropObjective;
    }

    public sealed class KanomjeenCoreConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public float CombatCooldownSeconds = 30f;
        public float RaidCooldownSeconds = 180f;
        public float CommandRateLimitSeconds = 1.5f;
        public float TeleportMovementToleranceMeters = 0.5f;
        public float PersistenceFlushSeconds = 30f;
        public bool EnableUi = true;
        public ushort UiEffectId = 51000;
        public short UiKey = 23001;
        public string UiContractVersion = "1.0";
        public string MessageColor = "cyan";
        public List<ZoneRule> Zones = new List<ZoneRule>();

        public void LoadDefaults()
        {
            Enabled = true;
            CombatCooldownSeconds = 30f;
            RaidCooldownSeconds = 180f;
            CommandRateLimitSeconds = 1.5f;
            TeleportMovementToleranceMeters = 0.5f;
            PersistenceFlushSeconds = 30f;
            EnableUi = true;
            UiEffectId = 51000;
            UiKey = 23001;
            UiContractVersion = "1.0";
            MessageColor = "cyan";
            Zones = new List<ZoneRule>();
        }
    }
}
