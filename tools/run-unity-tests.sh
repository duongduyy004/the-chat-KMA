#!/usr/bin/env bash
# Run a Unity test filter in batch mode and print the <test-run> summary.
# Usage: tools/run-unity-tests.sh <EditMode|PlayMode> <filter> <name>
# The Unity Editor must be closed: batch mode cannot open a project that is already open.
# Unity exits 0 even when tests fail, so read the printed summary, not the exit code.
set -uo pipefail

PLATFORM="${1:?usage: run-unity-tests.sh <EditMode|PlayMode> <filter> <name>}"
FILTER="${2:?filter required}"
NAME="${3:?result name required}"
UNITY="${UNITY:-/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe}"
OUT="Builds/TestResults"

mkdir -p "$OUT"
rm -f "$OUT/$NAME.xml"
"$UNITY" -batchmode -projectPath "$(pwd -W)" -runTests -testPlatform "$PLATFORM" \
  -testFilter "$FILTER" -testResults "$OUT/$NAME.xml" -logFile "$OUT/$NAME.log"

if [ -f "$OUT/$NAME.xml" ]; then
  grep -o '<test-run[^>]*>' "$OUT/$NAME.xml"
  grep -o '<test-case [^>]*result="Failed"[^>]*>' "$OUT/$NAME.xml" | head -20
else
  echo "No test results; compiler errors from $OUT/$NAME.log:"
  grep -n "error CS" "$OUT/$NAME.log" | head -20
  exit 1
fi
