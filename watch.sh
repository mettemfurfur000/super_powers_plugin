#!/bin/bash

WATCH_DIR="./src"       # Directory to watch
COMPILE_COMMAND="make"  # Command to execute on change (e.g., make, npm run build)
SLEEP_INTERVAL=2        # Seconds to wait between checks

# Initialize last modification state
LAST_STATE=$(find "$WATCH_DIR" -type f -print0 | xargs -0 md5sum)

echo "Watching directory: $WATCH_DIR"
echo "Compile command: $COMPILE_COMMAND"

while true; do
    sleep "$SLEEP_INTERVAL"
    CURRENT_STATE=$(find "$WATCH_DIR" -type f -print0 | xargs -0 md5sum)

    if [ "$CURRENT_STATE" != "$LAST_STATE" ]; then
        echo "File changes detected! Recompiling..."
        "$COMPILE_COMMAND"
        LAST_STATE="$CURRENT_STATE"
    fi
done