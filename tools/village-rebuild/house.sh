#!/bin/sh
# Rebuilds one village house end to end and takes its review shots: sh house.sh Barracks
#   1. blender/house_<name>.py builds the house and its Synty anchors (blender/run.sh)
#   2. models_import.cs maps its materials
#   3. houses_synty.cs places the Synty props and bakes it
#   4. house_shot.cs renders Temp/village-shots/unity_<Name>_0..2.png
#   (houses_prefabs.cs then updates every house prefab, so the shots show what the game shows)
# Needs the Unity editor open on the project, with the Unity CLI.
cd "$(dirname "$0")"
NAME="$1"
LOWER=$(echo "$NAME" | tr '[:upper:]' '[:lower:]')
U="${UNITY:-/c/Users/kaaru/AppData/Local/Unity/bin/unity}"
HERE="$(pwd -W 2>/dev/null || pwd)"
SHOTS="../../Temp/village-shots"
mkdir -p "$SHOTS"

wait_for() { n=0; until [ -f "$1" ] || [ $n -ge 60 ]; do sleep 1; n=$((n+1)); done; }
run() { "$U" command eval_file --file "$HERE/$1" --timeout 60 >/dev/null 2>&1; }

sh blender/run.sh "house_$LOWER" | grep -E "EXPORTED|NOTE|Error|Traceback"
run models_import.cs
echo "$NAME" > "$SHOTS/houses_synty_house.txt"
rm -f "$SHOTS/houses_synty_$NAME.txt"
run houses_synty.cs
wait_for "$SHOTS/houses_synty_$NAME.txt"
cat "$SHOTS/houses_synty_$NAME.txt"; echo
run houses_prefabs.cs
for step in 1 2 3; do
  rm -f "$SHOTS/house_shot_done.txt"
  echo "$step $NAME" > "$SHOTS/house_shot.txt"
  run house_shot.cs
  wait_for "$SHOTS/house_shot_done.txt"
done
ls "$SHOTS"/unity_"$NAME"_*.png
