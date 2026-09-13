using Rocket.API;

namespace Kanomjeen.AdminAudit
{
    public sealed class AdminAuditConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public float AuditFlushSeconds = 10f;
        public int MaxWarningsBeforeAction = 0;
        public uint WarningAutoBanSeconds = 3600;
        public string MessageColor = "cyan";

        public void LoadDefaults()
        {
            Enabled = true;
            AuditFlushSeconds = 10f;
            MaxWarningsBeforeAction = 0;
            WarningAutoBanSeconds = 3600;
            MessageColor = "cyan";
        }
    }
}
