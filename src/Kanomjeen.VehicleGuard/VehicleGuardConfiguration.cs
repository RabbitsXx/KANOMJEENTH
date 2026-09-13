using Rocket.API;

namespace Kanomjeen.VehicleGuard
{
    public sealed class VehicleGuardConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public bool LogVehicleEntry = true;
        public bool LogVehicleDamage = true;
        public float InspectDistance = 12f;
        public string MessageColor = "cyan";

        public void LoadDefaults()
        {
            Enabled = true;
            LogVehicleEntry = true;
            LogVehicleDamage = true;
            InspectDistance = 12f;
            MessageColor = "cyan";
        }
    }
}
