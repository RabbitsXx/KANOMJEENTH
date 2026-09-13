using System;
using System.Collections.Generic;
using Kanomjeen.Core;
using Kanomjeen.Core.Services;
using Rocket.API.Collections;
using Logger = Rocket.Core.Logging.Logger;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace Kanomjeen.BuildGuard
{
    public sealed class KanomjeenBuildGuardPlugin : RocketPlugin<BuildGuardConfiguration>
    {
        private sealed class Counts
        {
            public int Barricades;
            public int Structures;
            public int Sentries;
            public int Beds;
            public int Claims;
            public DateTime ExpiresUtc;
        }

        private readonly Dictionary<ulong, Counts> cache = new Dictionary<ulong, Counts>();
        private KanomjeenCorePlugin Core => KanomjeenCorePlugin.Instance;
        private Color MessageColor => UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor ?? "cyan", Color.cyan);

        protected override void Load()
        {
            Validate();
            BarricadeManager.onDeployBarricadeRequested += OnDeployBarricade;
            StructureManager.onDeployStructureRequested += OnDeployStructure;
            Logger.Log("[Kanomjeen.BuildGuard] Loaded.");
        }

        protected override void Unload()
        {
            BarricadeManager.onDeployBarricadeRequested -= OnDeployBarricade;
            StructureManager.onDeployStructureRequested -= OnDeployStructure;
            cache.Clear();
            Logger.Log("[Kanomjeen.BuildGuard] Unloaded.");
        }

        private void OnDeployBarricade(Barricade barricade, ItemBarricadeAsset asset, Transform hit, ref Vector3 point,
            ref float angleX, ref float angleY, ref float angleZ, ref ulong owner, ref ulong group, ref bool shouldAllow)
        {
            if (!Configuration.Instance.Enabled || !shouldAllow || owner == 0 || asset == null) return;
            var player = UnturnedPlayer.FromCSteamID(new CSteamID(owner));
            if (player == null || GameplayGuard.Has(player, "kanomjeen.build.bypass")) return;
            if (Configuration.Instance.BlockedItemIds.Contains(asset.id)) { shouldAllow = false; Say(player, "BlockedItem"); return; }
            if (Core != null && Core.Zones.IsBlocked(point, ZoneFeature.Build)) { shouldAllow = false; Say(player, "BlockedZone"); return; }

            var counts = GetCounts(owner);
            if (Configuration.Instance.MaxBarricadesPerPlayer > 0 && counts.Barricades >= Configuration.Instance.MaxBarricadesPerPlayer) { shouldAllow = false; Say(player, "Limit", "barricades", Configuration.Instance.MaxBarricadesPerPlayer); return; }

            if ((asset.build == EBuild.SENTRY || asset.build == EBuild.SENTRY_FREEFORM) && Configuration.Instance.MaxSentriesPerPlayer > 0 && counts.Sentries >= Configuration.Instance.MaxSentriesPerPlayer)
            { shouldAllow = false; Say(player, "Limit", "sentries", Configuration.Instance.MaxSentriesPerPlayer); return; }
            if (asset.build == EBuild.BED && Configuration.Instance.MaxBedsPerPlayer > 0 && counts.Beds >= Configuration.Instance.MaxBedsPerPlayer)
            { shouldAllow = false; Say(player, "Limit", "beds", Configuration.Instance.MaxBedsPerPlayer); return; }
            if (asset.build == EBuild.CLAIM && Configuration.Instance.MaxClaimsPerPlayer > 0 && counts.Claims >= Configuration.Instance.MaxClaimsPerPlayer)
            { shouldAllow = false; Say(player, "Limit", "claim flags", Configuration.Instance.MaxClaimsPerPlayer); return; }

            cache.Remove(owner);
        }

        private void OnDeployStructure(Structure structure, ItemStructureAsset asset, ref Vector3 point,
            ref float angleX, ref float angleY, ref float angleZ, ref ulong owner, ref ulong group, ref bool shouldAllow)
        {
            if (!Configuration.Instance.Enabled || !shouldAllow || owner == 0 || asset == null) return;
            var player = UnturnedPlayer.FromCSteamID(new CSteamID(owner));
            if (player == null || GameplayGuard.Has(player, "kanomjeen.build.bypass")) return;
            if (Configuration.Instance.BlockedItemIds.Contains(asset.id)) { shouldAllow = false; Say(player, "BlockedItem"); return; }
            if (Core != null && Core.Zones.IsBlocked(point, ZoneFeature.Build)) { shouldAllow = false; Say(player, "BlockedZone"); return; }

            var counts = GetCounts(owner);
            if (Configuration.Instance.MaxStructuresPerPlayer > 0 && counts.Structures >= Configuration.Instance.MaxStructuresPerPlayer)
            { shouldAllow = false; Say(player, "Limit", "structures", Configuration.Instance.MaxStructuresPerPlayer); return; }
            cache.Remove(owner);
        }

        private Counts GetCounts(ulong owner)
        {
            if (cache.TryGetValue(owner, out var cached) && cached.ExpiresUtc > DateTime.UtcNow) return cached;
            var result = new Counts { ExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(2f, Configuration.Instance.CountCacheSeconds)) };

            try
            {
                foreach (BarricadeRegion region in BarricadeManager.regions)
                {
                    if (region?.drops == null) continue;
                    foreach (var drop in region.drops)
                    {
                        if (drop == null || drop.GetServersideData().owner != owner) continue;
                        result.Barricades++;
                        var build = drop.asset.build;
                        if (build == EBuild.SENTRY || build == EBuild.SENTRY_FREEFORM) result.Sentries++;
                        else if (build == EBuild.BED) result.Beds++;
                        else if (build == EBuild.CLAIM) result.Claims++;
                    }
                }
            }
            catch (Exception ex) { Logger.LogWarning("[Kanomjeen.BuildGuard] Barricade count failed: " + ex.Message); }

            try
            {
                foreach (StructureRegion region in StructureManager.regions)
                {
                    if (region?.drops == null) continue;
                    foreach (var drop in region.drops)
                        if (drop != null && drop.GetServersideData().owner == owner) result.Structures++;
                }
            }
            catch (Exception ex) { Logger.LogWarning("[Kanomjeen.BuildGuard] Structure count failed: " + ex.Message); }

            cache[owner] = result;
            return result;
        }

        private void Say(UnturnedPlayer player, string key, params object[] args) { if (player != null) UnturnedChat.Say(player, Translate(key, args), MessageColor, false); }
        private void Validate() { var c = Configuration.Instance; if (c.CountCacheSeconds < 2f) c.CountCacheSeconds = 10f; if (c.BlockedItemIds == null) c.BlockedItemIds = new List<ushort>(); Configuration.Save(); }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "BlockedZone", "Building is blocked in this area." }, { "BlockedItem", "That buildable is disabled on Kanomjeen." },
            { "Limit", "You reached the {0} limit ({1}). Remove existing buildables before placing more." }
        };
    }
}
