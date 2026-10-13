#!/bin/sh
# Builds one village model headless: ./run.sh clock_tower
# Blender 5.2 is expected in its default install folder; set BLENDER to override.
cd "$(dirname "$0")"
BLENDER="${BLENDER:-/c/Program Files/Blender Foundation/Blender 5.2/blender.exe}"
VILLAGE_CLOSEUP="$CLOSEUP" "$BLENDER" --background --factory-startup --python-expr "exec(open('common.py').read()); exec(open('$1.py').read())" 2>&1 \
  | grep -E "^(SWATCH|EXPORTED|PREVIEW|MEASURE|NOTE)|Error|Traceback|line [0-9]+"
