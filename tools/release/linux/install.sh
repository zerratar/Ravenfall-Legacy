#!/bin/bash
set -e

# Ensure we are in the directory where the script is located (the game root)
cd "$(dirname "$0")"

UPDATE_DIR="./update"
UPDATE_FILE="$UPDATE_DIR/update.7z"
GAME_EXEC="./Ravenfall.x86_64"
UPDATE_URL="https://www.ravenfall.stream/download/linux"

command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Ensure 7z is installed
if ! command_exists 7z; then
    echo "7z not found. Attempting to install p7zip-full..."

    if command_exists apt; then
        sudo apt update && sudo apt install -y p7zip-full
    elif command_exists dnf; then
        sudo dnf install -y p7zip
    elif command_exists yum; then
        sudo yum install -y p7zip
    else
        echo "No compatible package manager found. Please install 'p7zip-full' or 'p7zip' manually."
        exit 1
    fi

    # Check again
    if ! command_exists 7z; then
        echo "7z installation failed. Please install it manually."
        exit 1
    fi
fi

# Ensure update directory exists
mkdir -p "$UPDATE_DIR"

# Download the update file
echo "Downloading update from $UPDATE_URL..."
if ! curl -fSL "$UPDATE_URL" -o "$UPDATE_FILE"; then
    echo "Failed to download update file."
    exec "$GAME_EXEC"
    exit 1
fi

# Kill any running instance of Ravenfall.x86_64 (except this script)
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