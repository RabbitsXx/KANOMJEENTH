$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

$Required = @(
  'Directory.Build.props','BUILD.md','INSTALL.md','PERMISSIONS.md','QA_REPORT.md',
  'workshop-ui/MasterBundle.dat','workshop-ui/Effects/KanomjeenUI/Asset.dat','workshop-ui/UI_CONTRACT.md',
  'workshop-ui/UnityProject/Assets/KanomjeenUI/Editor/KanomjeenUiBuilder.cs'
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
$Asset = Get-Content (Join-Path $Root 'workshop-ui/Effects/KanomjeenUI/Asset.dat') -Raw
if ($CoreConfig -notmatch 'UiEffectId\s*=\s*51000') { throw 'Core default UiEffectId is not 51000.' }
if ($Asset -notmatch '(?m)^ID\s+51000\s*$') { throw 'Workshop Effect ID is not 51000.' }

$Contract = Get-Content (Join-Path $Root 'workshop-ui/UI_CONTRACT.md') -Raw
if ($Contract -notmatch '1\.0') { throw 'UI contract version 1.0 marker missing.' }

# The prefab's screen containers and the server's screen list must agree. A screen the server
# does not know about is never explicitly hidden, so it stays visible on top of the requested
# screen (this shipped once and appeared in-game as two stacked screens).
$BuilderPath = Join-Path $Root 'workshop-ui/UnityProject/Assets/KanomjeenUI/Editor/KanomjeenUiBuilder.cs'
$UiServicePath = Join-Path $Root 'src/Kanomjeen.Core/Services/UiService.cs'

$BuilderScreens = @(Select-String -Path $BuilderPath -Pattern 'Screen\(parent,\s*"([a-z]+)"\)' -AllMatches |
  ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)

$ScreenBlock = [regex]::Match((Get-Content $UiServicePath -Raw), 'static readonly string\[\] Screens\s*=\s*\{(?<body>[^}]*)\}').Groups['body'].Value
$ServerScreens = @([regex]::Matches($ScreenBlock, '"([a-z]+)"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)

if (($BuilderScreens -join ',') -ne ($ServerScreens -join ',')) {
  throw "UI screen mismatch. builder=[$($BuilderScreens -join ', ')] server=[$($ServerScreens -join ', ')]"
}

Write-Host 'Kanomjeen source verification passed.' -ForegroundColor Green
Write-Host 'This is structural verification only; run build.ps1 and runtime QA before production.' -ForegroundColor Yellow
