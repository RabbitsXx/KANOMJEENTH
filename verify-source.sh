#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

required=(
  "Directory.Build.props" "BUILD.md" "INSTALL.md" "PERMISSIONS.md" "QA_REPORT.md"
  "workshop-ui/UnityProject/ProjectSettings/ProjectVersion.txt"
)

for f in "${required[@]}"; do
  [[ -f "$ROOT/$f" ]] || { echo "Required file missing: $f" >&2; exit 1; }
done

server_count="$(find "$ROOT/src" -type f -name '*.csproj' ! -path '*/Kanomjeen.Minimap.Client/*' | wc -l | tr -d ' ')"
client_count="$(find "$ROOT/src/Kanomjeen.Minimap.Client" -maxdepth 1 -type f -name '*.csproj' | wc -l | tr -d ' ')"
[[ "$server_count" == "11" ]] || { echo "Expected 11 server plugin projects, found $server_count" >&2; exit 1; }
[[ "$client_count" == "1" ]] || { echo "Expected 1 client module project, found $client_count" >&2; exit 1; }

if grep -RniE 'california\.|namespace[[:space:]]+Tpa\b' "$ROOT/src" --include='*.cs'; then
  echo 'Legacy namespace check failed.' >&2
  exit 1
fi

grep -Eq 'UiEffectId[[:space:]]*=[[:space:]]*51000' "$ROOT/src/Kanomjeen.Core/Configuration/KanomjeenCoreConfiguration.cs"
echo 'Workshop GUI assets are present; HUD data bridge is server-driven and requires live client QA.'

echo 'Kanomjeen source verification passed.'
echo 'Structural verification only: compile and runtime QA are still required.'
