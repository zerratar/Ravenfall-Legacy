#!/bin/bash
set -e  # Exit immediately if any command fails

#command -v 7z >/dev/null 2>&1 || { echo >&2 "7z is required but not installed. Aborting."; exit 1; }

UPDATE_DIR="./update"
UPDATE_FILE="$UPDATE_DIR/update.7z"
GAME_EXEC="./Ravenfall.x86_64"

pkill -f "$GAME_EXEC" && echo "Stopped running Ravenfall.x86_64." || echo "No running Ravenfall.x86_64 found."

if [ ! -f "$UPDATE_FILE" ]; then
  echo "No update file found at $UPDATE_FILE."
  exec "$GAME_EXEC"
  exit 0
fi

echo "Extracting update..."
TMPDIR=$(mktemp -d)
7z x "$UPDATE_FILE" -o"$TMPDIR"

echo "Replacing existing files..."
cp -rT "$TMPDIR" .

echo "Cleaning up..."
rm -rf "$TMPDIR"
rm -f "$UPDATE_FILE"

echo "Update complete. Starting the game..."
chmod +x "$GAME_EXEC"
exec "$GAME_EXEC"