#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 2 ]]; then
  echo "Usage: ./install.sh <server-root> <server-id>" >&2
  exit 2
fi

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DIST="$ROOT/dist/plugins"
SERVER_ROOT="$1"
SERVER_ID="$2"
PLUGINS="$SERVER_ROOT/Servers/$SERVER_ID/Rocket/Plugins"
BACKUP="$SERVER_ROOT/Backups/KanomjeenPlugins-$(date +%Y%m%d-%H%M%S)"

[[ -d "$DIST" ]] || { echo "Build output not found: $DIST. Run ./build.sh first." >&2; exit 1; }
mkdir -p "$PLUGINS" "$BACKUP"

find "$PLUGINS" -maxdepth 1 -type f -name 'Kanomjeen.*.dll' -exec cp -f {} "$BACKUP/" \;

if [[ -f "$PLUGINS/Tpa.dll" ]]; then
  echo "WARNING: old Tpa.dll detected. Disable/remove it before production because commands conflict." >&2
fi

cp -f "$DIST"/Kanomjeen.*.dll "$PLUGINS/"

echo "Installed Kanomjeen plugins to: $PLUGINS"
echo "Backup: $BACKUP"
echo "Restart the server; do not rely on Rocket reload for replacing loaded DLL code."
