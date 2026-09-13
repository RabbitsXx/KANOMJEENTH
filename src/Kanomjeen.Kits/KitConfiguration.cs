using System.Collections.Generic;
using Rocket.API;

namespace Kanomjeen.Kits
{
    public sealed class KitItem
    {
        public ushort Id;
        public byte Amount = 1;
    }

    public sealed class KitDefinition
    {
        public string Name = "starter";
        public string DisplayName = "Starter Survival Kit";
        public string Permission = "kanomjeen.kit.starter";
        public float CooldownSeconds = 21600f;
        public List<KitItem> Items = new List<KitItem>();
    }

    public sealed class KitConfiguration : IRocketPluginConfiguration
    {
        public bool Enabled = true;
        public bool BlockInVehicle = true;
        public string MessageColor = "cyan";
        public List<KitDefinition> Kits = new List<KitDefinition>();

        public void LoadDefaults()
        {
            Enabled = true;
            BlockInVehicle = true;
            MessageColor = "cyan";
            Kits = new List<KitDefinition>
            {
                new KitDefinition
                {
                    Name = "starter",
                    DisplayName = "Starter Survival Kit",
                    Permission = "kanomjeen.kit.starter",
                    CooldownSeconds = 21600f,
                    Items = new List<KitItem>
                    {
                        new KitItem { Id = 13, Amount = 1 },
                        new KitItem { Id = 14, Amount = 1 },
                        new KitItem { Id = 95, Amount = 1 }
                    }
                }
            };
        }
    }
}
