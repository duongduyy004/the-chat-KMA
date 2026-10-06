#!/usr/bin/env bash
# Send a screenshot request to an already-running Unity Editor (see
# Assets/Editor/PlayModeScreenshot.cs) and wait for it to finish.
#
# Usage: tools/qa-screenshot.sh <output.png> [scene.unity] [waitSeconds] [holdSprintTutorial] [forceSprintDistance] [forceSprintResult] [openPause] [openDialogue] [dialogueTaps] [openSettings] [qaState] [gameViewSize]
# forceSprintDistance/forceSprintResult drive SprintController/ResultPanel through their
# existing public test seams (AdvanceToDistance/Show) so a screenshot can show a late-race
# or result state without simulating real taps; leave blank to play out naturally.
# gameViewSize (e.g. 2400x1080 for 20:9) fixes the Game view resolution before Play Mode;
# blank keeps the current one, so pass 1920x1080 to go back to 16:9 after a wide capture.
set -euo pipefail

OUTPUT="${1:?usage: qa-screenshot.sh <output.png> [scene.unity] [waitSeconds]}"
SCENE="${2:-}"
WAIT="${3:-3}"
HOLD_SPRINT_TUTORIAL="${4:-false}"
FORCE_SPRINT_DISTANCE="${5:--1}"
FORCE_SPRINT_RESULT="${6:-}"
OPEN_PAUSE="${7:-false}"
OPEN_DIALOGUE="${8:-}"
DIALOGUE_TAPS="${9:-0}"
OPEN_SETTINGS="${10:-false}"
QA_STATE="${11:-}"
GAME_VIEW_SIZE="${12:-}"

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

case "$OPEN_PAUSE" in
  true|false) ;;
  *) echo "openPause must be true or false" >&2; exit 2 ;;
esac

case "$OPEN_SETTINGS" in
  true|false) ;;
  *) echo "openSettings must be true or false" >&2; exit 2 ;;
esac

if [[ -n "$GAME_VIEW_SIZE" && ! "$GAME_VIEW_SIZE" =~ ^[0-9]+x[0-9]+$ ]]; then
  echo "gameViewSize must look like 2400x1080" >&2; exit 2
fi

case "$FORCE_SPRINT_RESULT" in
  ""|pass|fail) ;;
  *) echo "forceSprintResult must be empty, pass, or fail" >&2; exit 2 ;;
esac

cat > "$REQ.tmp" <<EOF
{"id":"$ID","scene":"$SCENE","output":"$OUTPUT","waitSeconds":$WAIT,"holdSprintTutorial":$HOLD_SPRINT_TUTORIAL,"forceSprintDistance":$FORCE_SPRINT_DISTANCE,"forceSprintResult":"$FORCE_SPRINT_RESULT","openPause":$OPEN_PAUSE,"openDialogue":"$OPEN_DIALOGUE","dialogueTaps":$DIALOGUE_TAPS,"openSettings":$OPEN_SETTINGS,"qaState":"$QA_STATE","gameViewSize":"$GAME_VIEW_SIZE"}
EOF
# The Editor may be reading the previous request.json; on Windows that briefly locks it.
for _ in 1 2 3 4 5 6 7 8 9 10; do
  mv "$REQ.tmp" "$REQ" 2>/dev/null && break
  sleep 0.5
done
[ -f "$REQ.tmp" ] && { echo "Could not replace $REQ (locked?)" >&2; exit 1; }

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
