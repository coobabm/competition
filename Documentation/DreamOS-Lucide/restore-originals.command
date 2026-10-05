#!/bin/bash
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
if [[ "${1:-}" != "" && "${1:-}" != "--check" ]]; then
  echo "Usage: $0 [--check]" >&2; exit 2
fi
# Validate every file first. Refuse to overwrite any newer user edits.
(cd "$HERE/Originals" && shasum -a 256 --quiet -c "$HERE/originals.sha256")
(cd "$ROOT" && shasum -a 256 --quiet -c "$HERE/lucide.sha256" && shasum -a 256 --quiet -c "$HERE/metadata.sha256")
if [[ "${1:-}" == "--check" ]]; then
  echo "PASS: all backups, Lucide PNGs and original metadata verified; nothing changed."
  exit 0
fi
while IFS= read -r relative; do
  cp "$HERE/Originals/$relative" "$ROOT/$relative"
done < "$HERE/files.txt"
(cd "$ROOT" && shasum -a 256 --quiet -c "$HERE/originals.sha256")
echo "Original DreamOS icons restored. Return to Unity and refresh assets."
