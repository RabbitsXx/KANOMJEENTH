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

count="$(find "$ROOT/src" -type f -name '*.csproj' | wc -l | tr -d ' ')"
[[ "$count" == "11" ]] || { echo "Expected 11 plugin projects, found $count" >&2; exit 1; }

if grep -RniE 'california\.|namespace[[:space:]]+Tpa\b' "$ROOT/src" --include='*.cs'; then
  echo 'Legacy namespace check failed.' >&2
  exit 1
fi

grep -Eq 'UiEffectId[[:space:]]*=[[:space:]]*51000' "$ROOT/src/Kanomjeen.Core/Configuration/KanomjeenCoreConfiguration.cs"
echo 'Legacy combined Workshop GUI assets are intentionally absent; per-plugin GUI work is pending.'

echo 'Kanomjeen source verification passed.'
echo 'Structural verification only: compile and runtime QA are still required.'
