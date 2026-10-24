#!/bin/bash
# Ravenfall's updater on Linux, started by the game (setsid ./update.sh) after it downloaded update/update.7z.
#
# What it does: waits for the game to close, unpacks the update into update/unpacked (in the game folder, so moving
# the files into place is a rename and /tmp, often a small RAM disk, is never filled), moves them over the old ones,
# cleans up and starts the game again. Every step is written to update/update.log, and the game is started again
# whatever happens, so a failed update never leaves the streamer without a game.
#
# It runs from a copy outside the game folder: bash reads a script while it runs, and the update replaces this file.
# A new version of it comes with an update in updater/ and the game moves it into place when it next starts.

if [ -z "$RAVENFALL_UPDATER_COPY" ]; then
    GAME_DIR="$(cd "$(dirname "$0")" && pwd)"
    COPY="$(mktemp "${TMPDIR:-/tmp}/ravenfall-update.XXXXXX")" || COPY="$GAME_DIR/update/update-running.sh"
    mkdir -p "$GAME_DIR/update"
    cp "$0" "$COPY" && chmod +x "$COPY" || COPY="$0"
    RAVENFALL_UPDATER_COPY=1 exec "$COPY" "$GAME_DIR"
fi

GAME_DIR="$1"
cd "$GAME_DIR" || exit 1

UPDATE_DIR="$GAME_DIR/update"
UNPACKED="$UPDATE_DIR/unpacked"
LOG="$UPDATE_DIR/update.log"
GAME_EXEC="$GAME_DIR/Ravenfall.x86_64"
mkdir -p "$UPDATE_DIR"
: > "$LOG"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*" | tee -a "$LOG"
}

start_game() {
    chmod +x "$GAME_EXEC" 2>/dev/null
    log "Starting $GAME_EXEC"
    cd "$GAME_DIR" && setsid "$GAME_EXEC" >/dev/null 2>&1 &
    [ "$0" != "$GAME_DIR/update.sh" ] && rm -f "$0"
    exit "${1:-0}"
}

log "Ravenfall update in $GAME_DIR"

PACKAGE=""
for name in update.7z update-linux.7z update.zip; do
    if [ -f "$UPDATE_DIR/$name" ]; then PACKAGE="$UPDATE_DIR/$name"; break; fi
done
if [ -z "$PACKAGE" ]; then
    log "No update archive in $UPDATE_DIR"
    start_game 1
fi
log "Archive $PACKAGE ($(du -m "$PACKAGE" | cut -f1) MB)"

# the game: asked to close, then ended if it will not
for pid in $(pgrep -f "Ravenfall.x86_64"); do
    [ "$pid" = "$$" ] && continue
    log "Waiting for Ravenfall ($pid) to close"
    kill -TERM "$pid" 2>/dev/null
    for _ in $(seq 1 30); do kill -0 "$pid" 2>/dev/null || break; sleep 1; done
    if kill -0 "$pid" 2>/dev/null; then
        log "Ravenfall did not close; ending it"
        kill -KILL "$pid" 2>/dev/null
        sleep 1
    fi
done

# something to unpack 7z with: 7z (p7zip), 7za, 7zz (7-Zip), or bsdtar (libarchive)
EXTRACT=""
if command -v 7z >/dev/null 2>&1; then EXTRACT="7z"
elif command -v 7za >/dev/null 2>&1; then EXTRACT="7za"
elif command -v 7zz >/dev/null 2>&1; then EXTRACT="7zz"
elif command -v bsdtar >/dev/null 2>&1; then EXTRACT="bsdtar"
fi
if [ -z "$EXTRACT" ]; then
    log "Nothing to unpack the update with. Install p7zip (apt install p7zip-full, dnf install p7zip) and start Ravenfall again."
    start_game 1
fi

rm -rf "$UNPACKED"
mkdir -p "$UNPACKED"
log "Unpacking with $EXTRACT"
if [ "$EXTRACT" = "bsdtar" ]; then
    bsdtar -xf "$PACKAGE" -C "$UNPACKED" >>"$LOG" 2>&1
else
    "$EXTRACT" x -y -bd "$PACKAGE" -o"$UNPACKED" >>"$LOG" 2>&1
fi
if [ $? -ne 0 ]; then
    log "Unpacking failed (is the disk full?). The game is left as it was."
    rm -rf "$UNPACKED"
    start_game 1
fi

# into place: every file moved over the old one (a rename on the same drive), counted
log "Installing"
FAILED=0
INSTALLED=0
while IFS= read -r -d '' file; do
    relative="${file#$UNPACKED/}"
    case "$relative" in update.json|*.7z|*.zip) continue ;; esac
    mkdir -p "$GAME_DIR/$(dirname "$relative")"
    if mv -f "$file" "$GAME_DIR/$relative" 2>>"$LOG"; then
        INSTALLED=$((INSTALLED + 1))
    else
        FAILED=$((FAILED + 1))
        log "Could not replace $relative"
    fi
done < <(find "$UNPACKED" -type f -print0)
log "Installed $INSTALLED files, $FAILED could not be replaced"

rm -rf "$UNPACKED"
if [ "$FAILED" -eq 0 ]; then
    rm -f "$PACKAGE"
    log "Done"
fi
start_game "$FAILED"
