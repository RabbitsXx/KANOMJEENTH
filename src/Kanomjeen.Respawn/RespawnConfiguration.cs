using Rocket.API;

namespace Kanomjeen.Respawn
{
    public sealed class RespawnConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public float ProtectionSeconds = 10f;
        public bool CancelOnAttack = true;
        public bool CancelOnMovement = true;
        public float MovementToleranceMeters = 1.5f;
        public float CheckIntervalSeconds = 0.5f;
        public string MessageColor = "cyan";

        public void LoadDefaults()
        {
            Enabled = true;
            ProtectionSeconds = 10f;
            CancelOnAttack = true;
            CancelOnMovement = true;
            MovementToleranceMeters = 1.5f;
            CheckIntervalSeconds = 0.5f;
            MessageColor = "cyan";
        }
    }
}
