#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Release}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DIST="$ROOT/dist/plugins"

projects=(
  "Kanomjeen.Core"
  "Kanomjeen.TPA"
  "Kanomjeen.Homes"
  "Kanomjeen.Kits"
  "Kanomjeen.Respawn"
  "Kanomjeen.BuildGuard"
  "Kanomjeen.Airdrops"
  "Kanomjeen.Stats"
  "Kanomjeen.ServerManager"
  "Kanomjeen.AdminAudit"
  "Kanomjeen.VehicleGuard"
)

rm -rf "$DIST"
mkdir -p "$DIST"

for name in "${projects[@]}"; do
  project="$ROOT/src/$name/$name.csproj"
  echo "==> Building $name"
  dotnet build "$project" -c "$CONFIGURATION" --nologo
  dll="$ROOT/src/$name/bin/$CONFIGURATION/$name.dll"
  [[ -f "$dll" ]] || { echo "Expected DLL missing: $dll" >&2; exit 1; }
  cp "$dll" "$DIST/$name.dll"
done

count="$(find "$DIST" -maxdepth 1 -type f -name 'Kanomjeen.*.dll' | wc -l | tr -d ' ')"
[[ "$count" == "11" ]] || { echo "Expected 11 DLLs, found $count" >&2; exit 1; }

echo "Build complete: $DIST"
