using System.Collections.Generic;
using Rocket.API;

namespace Kanomjeen.ServerManager
{
    public sealed class ServerManagerConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public float AutoSaveIntervalSeconds = 900f;
        public float RestartEveryHours = 6f;
        public int AutomaticRestartCountdownSeconds = 300;
        public float AnnouncementIntervalSeconds = 1200f;
        public List<string> Announcements = new List<string>();
        public string MessageColor = "cyan";

        public void LoadDefaults()
        {
            Enabled = true;
            AutoSaveIntervalSeconds = 900f;
            RestartEveryHours = 6f;
            AutomaticRestartCountdownSeconds = 300;
            AnnouncementIntervalSeconds = 1200f;
            Announcements = new List<string>
            {
                "Kanomjeen is Semi-Vanilla: exploration, vehicles, raids and California 2 progression matter.",
                "TPA and Home are blocked during combat, raid tags and restricted objectives.",
                "Use /stats to view your persistent survival statistics."
            };
            MessageColor = "cyan";
        }
    }
}
