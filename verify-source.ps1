$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

$Required = @(
  'Directory.Build.props','BUILD.md','INSTALL.md','PERMISSIONS.md','QA_REPORT.md',
  'workshop-ui/UnityProject/ProjectSettings/ProjectVersion.txt'
)

foreach ($relative in $Required) {
  $path = Join-Path $Root $relative
  if (-not (Test-Path $path)) { throw "Required file missing: $relative" }
}

$Projects = Get-ChildItem (Join-Path $Root 'src') -Recurse -Filter '*.csproj'
if ($Projects.Count -ne 11) { throw "Expected 11 plugin projects, found $($Projects.Count)." }

$SourceFiles = Get-ChildItem (Join-Path $Root 'src') -Recurse -Filter '*.cs'
$Forbidden = $SourceFiles | Select-String -Pattern 'california\.|namespace\s+Tpa\b' -CaseSensitive:$false
if ($Forbidden) {
  $Forbidden | ForEach-Object { Write-Error "Forbidden legacy namespace/text: $($_.Path):$($_.LineNumber) $($_.Line.Trim())" }
  throw 'Legacy namespace check failed.'
}

$CoreConfig = Get-Content (Join-Path $Root 'src/Kanomjeen.Core/Configuration/KanomjeenCoreConfiguration.cs') -Raw
if ($CoreConfig -notmatch 'UiEffectId\s*=\s*51000') { throw 'Core default UiEffectId is not 51000.' }

Write-Host 'Workshop GUI assets are present; HUD data bridge is server-driven and requires live client QA.' -ForegroundColor Yellow

Write-Host 'Kanomjeen source verification passed.' -ForegroundColor Green
Write-Host 'This is structural verification only; run build.ps1 and runtime QA before production.' -ForegroundColor Yellow
