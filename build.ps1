param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Dist = Join-Path $Root 'dist/plugins'

$Projects = @(
    @{ Name='Kanomjeen.Core'; Path='src/Kanomjeen.Core/Kanomjeen.Core.csproj' },
    @{ Name='Kanomjeen.TPA'; Path='src/Kanomjeen.TPA/Kanomjeen.TPA.csproj' },
    @{ Name='Kanomjeen.Homes'; Path='src/Kanomjeen.Homes/Kanomjeen.Homes.csproj' },
    @{ Name='Kanomjeen.Kits'; Path='src/Kanomjeen.Kits/Kanomjeen.Kits.csproj' },
    @{ Name='Kanomjeen.Respawn'; Path='src/Kanomjeen.Respawn/Kanomjeen.Respawn.csproj' },
    @{ Name='Kanomjeen.BuildGuard'; Path='src/Kanomjeen.BuildGuard/Kanomjeen.BuildGuard.csproj' },
    @{ Name='Kanomjeen.Airdrops'; Path='src/Kanomjeen.Airdrops/Kanomjeen.Airdrops.csproj' },
    @{ Name='Kanomjeen.Stats'; Path='src/Kanomjeen.Stats/Kanomjeen.Stats.csproj' },
    @{ Name='Kanomjeen.ServerManager'; Path='src/Kanomjeen.ServerManager/Kanomjeen.ServerManager.csproj' },
    @{ Name='Kanomjeen.AdminAudit'; Path='src/Kanomjeen.AdminAudit/Kanomjeen.AdminAudit.csproj' },
    @{ Name='Kanomjeen.VehicleGuard'; Path='src/Kanomjeen.VehicleGuard/Kanomjeen.VehicleGuard.csproj' }
)

if (Test-Path $Dist) { Remove-Item $Dist -Recurse -Force }
New-Item -ItemType Directory -Path $Dist -Force | Out-Null

Push-Location $Root
try {
    foreach ($Project in $Projects) {
        Write-Host "==> Building $($Project.Name)" -ForegroundColor Cyan
        dotnet build $Project.Path -c $Configuration --nologo
        if ($LASTEXITCODE -ne 0) { throw "Build failed: $($Project.Name)" }

        $ProjectDir = Split-Path -Parent (Join-Path $Root $Project.Path)
        $Dll = Join-Path $ProjectDir "bin/$Configuration/$($Project.Name).dll"
        if (-not (Test-Path $Dll)) { throw "Expected DLL missing: $Dll" }
        Copy-Item $Dll (Join-Path $Dist "$($Project.Name).dll") -Force
    }

    $Expected = $Projects.Name | Sort-Object
    $Actual = Get-ChildItem $Dist -Filter '*.dll' | ForEach-Object BaseName | Sort-Object
    if (Compare-Object $Expected $Actual) {
        throw 'dist/plugins does not contain exactly the expected Kanomjeen DLL set.'
    }

    Write-Host "Build complete: $Dist" -ForegroundColor Green
}
finally {
    Pop-Location
}
