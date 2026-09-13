# Kanomjeen staging certification report — 2026-09-13

1. **OS:** Host Ubuntu 26.04.1 LTS, kernel 7.0.0-31-generic; server container reports Debian GNU/Linux 13.
2. **.NET SDK:** 8.0.425, installed locally at `/home/rabbitsx/.local/share/kanomjeen-dotnet` for this build.
3. **Unturned Dedicated Server:** game version 3.26.3.11; Steam build ID 25107012.
4. **RocketModFix/LDM:** Rocket.Unturned 4.9.3.18; Rocket.API/Core assembly version 4.9.3.16.
5. **Unity:** server runtime 2022.3.62f3. Unity Editor is not installed/found. `Extras/Sources/Project.unitypackage` is absent from this dedicated-server installation.
6. **Source verification:** PASS. See `verify-source-final.log`.
7. **Build:** PASS — clean Release build, 0 compiler errors, exactly 11 DLLs in `dist/plugins`. Existing obsolete API warnings remain for the legacy-compatible DamageTool and EffectManager calls. Packaged artifact: `dist/Kanomjeen-plugins-0.1.0-staging-20260913.tar.gz`, SHA-256 `a1f5c82e7a040836d9f47496d6900a1073c1bc1c0f734d4e77629afcfcc202da`.
8. **Problems found:** ambiguous Rocket/Unity `Logger`; missing Rocket permission extension import; ambiguous `Random`; `Directory` member shadowing `System.IO.Directory`; obsolete/nonexistent `SteamBlacklist.add/remove`; command value `0` did not match the installed API's explicit permanent duration constant. All compile/API issues were corrected. Runtime produced no Kanomjeen exceptions. California 2 content produced missing-asset, collider, prefab, and animation warnings/errors unrelated to Kanomjeen plugins.
9. **Files/members changed:** all plugin entry files now alias `Rocket.Core.Logging.Logger`; `GameplayGuard.cs` imports `Rocket.API`; `KanomjeenAirdropsPlugin.random` is `System.Random`; `KanomjeenAdminAuditPlugin.CommandBan`, `CommandUnban`, and `Flush` use the exact installed APIs/types; Stats/Airdrops/AdminAudit `TryBindCore` methods log successful binding. Staging `Commands.config.xml` had stale legacy `Tpa.TpaPlugin` entries removed.
10. **Final DLL SHA-256:**
    - Kanomjeen.AdminAudit.dll — `4ae39517f1b49ad91d1a7dd1dd015eb0e7baa1481e13bb27fa081c98fb4a7d54`
    - Kanomjeen.Airdrops.dll — `14588494699079ad929fcf123cd1e3633d75129582d138375573ffd2036fd311`
    - Kanomjeen.BuildGuard.dll — `daa88d029ff023bc2d1adcb7d7e1e465e4a02261fb63f2d2a6e737e5b93604fe`
    - Kanomjeen.Core.dll — `dbd448fb4b5d99e6d09cd9a8ce681a45f0e1f433ded663992c29c7c80d0d1075`
    - Kanomjeen.Homes.dll — `2bf0d3ad488e394dd65874f25d12a0f768d8b03d21ae73087e2a849c066d00ee`
    - Kanomjeen.Kits.dll — `5c06c0c8ca222c445f78f5d3ce218768955b7df44c7ae9d2115c71f6f5bc4e63`
    - Kanomjeen.Respawn.dll — `c98f5e68f9ffae5e5beab43a408929c18070eb576038e5b696c8776e3af53c29`
    - Kanomjeen.ServerManager.dll — `75d9037ebe76db2fd61cf9b21502f8ef73c7de2a58f42314182e012f06e39de1`
    - Kanomjeen.Stats.dll — `7d3666913c14130d0b84fd1e7ed64866359f5e395be00cb361b7fa108f526d70`
    - Kanomjeen.TPA.dll — `c8033140c9980b2775cde3ce2205f6a45785e3baac445c7a527649a5e4a91159`
    - Kanomjeen.VehicleGuard.dll — `b068c0ca73545860fcea9ceb01486430cadf5ab684df54406c0ac4a84e8b676a`
11. **Staging install path:** `/home/rabbitsx/unturned/Servers/Mywork/Rocket/Plugins/`.
12. **Rollback/backup:** full archive `/home/rabbitsx/unturned/Backups/Kanomjeen-20260913-predeploy/Servers-Mywork.tar.gz`; disabled old plugin `/home/rabbitsx/unturned/Backups/Kanomjeen-20260913-predeploy/disabled-plugins/Tpa`; prior DLL backup `/home/rabbitsx/unturned/Backups/KanomjeenPlugins-20260913-104730`.
13. **Server startup:** PASS for plugin load. Two full container/server restarts completed; UDP 27015/27016 listening; all 11 modules loaded and all Core consumers reported binding.
14. **Startup warnings/errors:** no Kanomjeen TypeLoadException, MissingMethodException, delegate exception, NullReferenceException, or Load exception. California 2 reports missing object GUID `7bcb4fe1-b38b-40fc-ab23-bf6c55c45e71`, negative-scale collider warnings, a missing `Clip` fallback, and missing `Indicators` animation. These require separate map-content review.
15. **California 2 zones:** none configured; live coordinate selection was not performed and no coordinates were invented.
16. **Airdrop spawns:** none configured. Installed content contains valid cargo spawn table `CA_Carepackage`, legacy spawn ID `4409`, but no live coordinate was selected or tested.
17. **Unity prefab build:** NOT RUN — Unity Editor and Project.unitypackage unavailable.
18. **Masterbundle files:** none generated.
19. **Masterbundle/hash SHA-256:** unavailable because no bundle/hash was generated.
20. **Workshop File ID:** unavailable; no upload performed.
21. **Resolutions tested:** none.
22. **Languages tested:** none.
23. **QA critical result:** compile and plugin-load gates PASS; Workshop bundle/hash, local GUI, California 2 configuration, persistence gameplay, exploit, performance, and multiplayer matrices remain untested/FAIL for release gating.
24. **Known issues:** Unity/Workshop workflow is blocked on a compatible desktop Unity installation, Unturned client source package, legal Thai font, and Steam publishing session. Multiplayer QA requires two non-admin clients plus staff. California 2 map-content warnings/errors predate and are independent of this plugin set but should be reviewed. Persistent gameplay XML has not yet been exercised by live commands across restart.
25. **Final verdict:** **STAGING FAILED**
