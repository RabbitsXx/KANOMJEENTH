using Rocket.API;

namespace Kanomjeen.Stats
{
    public sealed class StatsConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public float SaveIntervalSeconds = 30f;
        public string MessageColor = "cyan";

        public void LoadDefaults()
        {
            Enabled = true;
            SaveIntervalSeconds = 30f;
            MessageColor = "cyan";
        }
    }
}
