#!/usr/bin/env bash
# Send a screenshot request to an already-running Unity Editor (see
# Assets/Editor/PlayModeScreenshot.cs) and wait for it to finish.
#
# Usage: tools/qa-screenshot.sh <output.png> [scene.unity] [waitSeconds] [holdSprintTutorial]
set -euo pipefail

OUTPUT="${1:?usage: qa-screenshot.sh <output.png> [scene.unity] [waitSeconds]}"
SCENE="${2:-}"
WAIT="${3:-3}"
HOLD_SPRINT_TUTORIAL="${4:-false}"

REQ_DIR="Builds/Screenshots"
REQ="$REQ_DIR/request.json"
DONE="$REQ_DIR/done.json"
ID="$(date +%s%N)"

mkdir -p "$REQ_DIR"
rm -f "$DONE"

case "$HOLD_SPRINT_TUTORIAL" in
  true|false) ;;
  *) echo "holdSprintTutorial must be true or false" >&2; exit 2 ;;
esac

cat > "$REQ.tmp" <<EOF
{"id":"$ID","scene":"$SCENE","output":"$OUTPUT","waitSeconds":$WAIT,"holdSprintTutorial":$HOLD_SPRINT_TUTORIAL}
EOF
mv "$REQ.tmp" "$REQ"

echo "Requested $OUTPUT (scene=${SCENE:-<current>}, wait=${WAIT}s, id=$ID)"

for _ in $(seq 1 $((WAIT + 60))); do
  if [ -f "$DONE" ] && grep -q "\"$ID\"" "$DONE"; then
    cat "$DONE"
    echo
    if grep -q '"status":"ok"' "$DONE"; then
      exit 0
    else
      exit 1
    fi
  fi
  sleep 1
done

echo "Timed out waiting for done.json (is the Editor running? see PlayModeScreenshot.cs)" >&2
exit 1
