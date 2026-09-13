using Rocket.API;

namespace Kanomjeen.Homes
{
    public sealed class HomeConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public int MaxHomes = 2;
        public float TeleportCooldownSeconds = 900f;
        public float TeleportDelaySeconds = 10f;
        public float MovementToleranceMeters = 0.5f;
        public float WarmupCheckIntervalSeconds = 0.25f;
        public bool BlockInVehicle = true;
        public bool CancelOnDamage = true;
        public bool CancelOnMovement = true;
        public float SaveIntervalSeconds = 30f;
        public string MessageColor = "cyan";

        public void LoadDefaults()
        {
            Enabled = true;
            MaxHomes = 2;
            TeleportCooldownSeconds = 900f;
            TeleportDelaySeconds = 10f;
            MovementToleranceMeters = 0.5f;
            WarmupCheckIntervalSeconds = 0.25f;
            BlockInVehicle = true;
            CancelOnDamage = true;
            CancelOnMovement = true;
            SaveIntervalSeconds = 30f;
            MessageColor = "cyan";
        }
    }
}
