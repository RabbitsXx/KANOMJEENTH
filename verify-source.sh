#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

required=(
  "Directory.Build.props" "BUILD.md" "INSTALL.md" "PERMISSIONS.md" "QA_REPORT.md"
  "workshop-ui/MasterBundle.dat" "workshop-ui/Effects/KanomjeenUI/Asset.dat" "workshop-ui/UI_CONTRACT.md"
  "workshop-ui/UnityProject/Assets/KanomjeenUI/Editor/KanomjeenUiBuilder.cs"
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
grep -Eq '^ID[[:space:]]+51000[[:space:]]*$' "$ROOT/workshop-ui/Effects/KanomjeenUI/Asset.dat"
grep -Eq '1\.0' "$ROOT/workshop-ui/UI_CONTRACT.md"

# The prefab's screen containers and the server's screen list must agree. A screen the server
# does not know about is never explicitly hidden, so it stays visible on top of the requested
# screen (this shipped once and appeared in-game as two stacked screens).
builder_screens="$(grep -oE 'Screen\(parent, "[a-z]+"\)' "$ROOT/workshop-ui/UnityProject/Assets/KanomjeenUI/Editor/KanomjeenUiBuilder.cs" | sed -E 's/.*"([a-z]+)".*/\1/' | sort -u | tr '\n' ' ')"
server_screens="$(sed -n '/static readonly string\[\] Screens/,/};/p' "$ROOT/src/Kanomjeen.Core/Services/UiService.cs" | grep -oE '"[a-z]+"' | tr -d '"' | sort -u | tr '\n' ' ')"
if [[ "$builder_screens" != "$server_screens" ]]; then
  echo 'UI screen mismatch between the Unity prefab builder and UiService.Screens.' >&2
  echo "  builder: $builder_screens" >&2
  echo "  server : $server_screens" >&2
  exit 1
fi

echo 'Kanomjeen source verification passed.'
echo 'Structural verification only: compile and runtime QA are still required.'
