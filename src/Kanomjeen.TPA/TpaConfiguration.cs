using Rocket.API;

namespace Kanomjeen.TPA
{
    public sealed class TpaConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public float RequestTimeoutSeconds = 30f;
        public float RequestCommandCooldownSeconds = 3f;
        public float TeleportCooldownSeconds = 600f;
        public float TeleportDelaySeconds = 10f;
        public float MovementToleranceMeters = 0.5f;
        public float WarmupCheckIntervalSeconds = 0.25f;
        public int MaxOutgoingRequests = 1;
        public int MaxIncomingRequests = 3;
        public bool AllowTpaHere = true;
        public bool BlockInVehicle = true;
        public bool CancelOnDamage = true;
        public bool CancelOnMovement = true;
        public string MessageColor = "cyan";

        public void LoadDefaults()
        {
            Enabled = true;
            RequestTimeoutSeconds = 30f;
            RequestCommandCooldownSeconds = 3f;
            TeleportCooldownSeconds = 600f;
            TeleportDelaySeconds = 10f;
            MovementToleranceMeters = 0.5f;
            WarmupCheckIntervalSeconds = 0.25f;
            MaxOutgoingRequests = 1;
            MaxIncomingRequests = 3;
            AllowTpaHere = true;
            BlockInVehicle = true;
            CancelOnDamage = true;
            CancelOnMovement = true;
            MessageColor = "cyan";
        }
    }
}
