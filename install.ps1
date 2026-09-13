param(
    [Parameter(Mandatory=$true)]
    [string]$ServerRoot,
    [Parameter(Mandatory=$true)]
    [string]$ServerId
)

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Dist = Join-Path $Root 'dist/plugins'
$Plugins = Join-Path $ServerRoot "Servers/$ServerId/Rocket/Plugins"
$Backup = Join-Path $ServerRoot ("Backups/KanomjeenPlugins-" + (Get-Date -Format 'yyyyMMdd-HHmmss'))

if (-not (Test-Path $Dist)) { throw "Build output not found: $Dist. Run ./build.ps1 first." }
New-Item -ItemType Directory -Path $Plugins -Force | Out-Null
New-Item -ItemType Directory -Path $Backup -Force | Out-Null

Get-ChildItem $Plugins -Filter 'Kanomjeen.*.dll' -ErrorAction SilentlyContinue | ForEach-Object {
    Copy-Item $_.FullName (Join-Path $Backup $_.Name) -Force
}

$OldTpa = Join-Path $Plugins 'Tpa.dll'
if (Test-Path $OldTpa) {
    Write-Warning "Old Tpa.dll detected. Disable/remove it before production because it conflicts with Kanomjeen.TPA commands."
}

Get-ChildItem $Dist -Filter 'Kanomjeen.*.dll' | ForEach-Object {
    Copy-Item $_.FullName (Join-Path $Plugins $_.Name) -Force
}

Write-Host "Installed Kanomjeen plugins to: $Plugins" -ForegroundColor Green
Write-Host "Previous Kanomjeen DLL backup: $Backup" -ForegroundColor Yellow
Write-Host "Restart the server. Do not rely on Rocket reload to replace already-loaded assembly code." -ForegroundColor Yellow
