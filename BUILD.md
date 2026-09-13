# Build Kanomjeen Plugin Suite

## Requirements
- .NET SDK capable of building `netstandard2.1`
- internet/NuGet access for first package restore, or packages already cached
- target server compatible with the versions in `Directory.Build.props`

Current compile references:
- RocketModFix.LDM.Redist 4.9.3.18
- RocketModFix.Unturned.Redist.Server 3.26.3.11
- RocketModFix.UnityEngine.Redist 2022.3.62.3

## 0. Verify repository structure first
Before compiling, run the structural verifier. This catches missing handoff files, legacy namespace leftovers, project-count mistakes, and UI identity mismatches.

Windows PowerShell:

```powershell
./verify-source.ps1
```

Linux:

```bash
chmod +x verify-source.sh build.sh install.sh
./verify-source.sh
```

This is not a compiler/runtime test. A passing verifier only means the source package is structurally coherent.

## 1. Build all modules
From the `kanomjeen-suite` root:

```bash
./build.sh
```

Windows PowerShell:

```powershell
./build.ps1
```

The scripts build Core first and then feature modules. Output DLLs are copied into:

```text
dist/plugins/
```

Expected DLLs:
- `Kanomjeen.Core.dll`
- `Kanomjeen.TPA.dll`
- `Kanomjeen.Homes.dll`
- `Kanomjeen.Kits.dll`
- `Kanomjeen.Respawn.dll`
- `Kanomjeen.BuildGuard.dll`
- `Kanomjeen.Airdrops.dll`
- `Kanomjeen.Stats.dll`
- `Kanomjeen.ServerManager.dll`
- `Kanomjeen.AdminAudit.dll`
- `Kanomjeen.VehicleGuard.dll`

## 2. Manual build
Build Core first:

```bash
dotnet build src/Kanomjeen.Core/Kanomjeen.Core.csproj -c Release
```

Then each feature project, for example:

```bash
dotnet build src/Kanomjeen.TPA/Kanomjeen.TPA.csproj -c Release
dotnet build src/Kanomjeen.Homes/Kanomjeen.Homes.csproj -c Release
```

Project references automatically rebuild Core as needed.

## 3. API/version gate
If a compile error comes from an Unturned/Rocket API member:
1. do not substitute a guessed signature
2. compare the exact target server build
3. inspect the server assemblies or ApiDump output
4. update the adapter/event signature
5. rebuild and run staging tests

This is especially important after an Unturned stable update.

## 4. Workshop UI
The C# build does not create client assets. Follow:
`workshop-ui/WORKSHOP_RELEASE.md`

The UI requires Unity + Unturned's current `Project.unitypackage` and Master Bundle export. Final bundle/hash files are binary build outputs and are intentionally not faked in source control.

## 5. Clean rebuild
```bash
dotnet clean src/Kanomjeen.Core/Kanomjeen.Core.csproj -c Release
find src -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
./build.sh
```

On Windows remove `bin`/`obj` folders as appropriate and rerun `build.ps1`.

## 6. Release gate
A successful compiler exit is necessary but not sufficient. Complete the multiplayer/runtime checklist in `QA_REPORT.md` before production deployment.
