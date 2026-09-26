#!/usr/bin/env bash
# Run a Unity test filter in batch mode and print the <test-run> summary.
# Usage: tools/run-unity-tests.sh <EditMode|PlayMode> <filter> <name>
# The Unity Editor must be closed: batch mode cannot open a project that is already open.
# Unity may exit 0 even when tests fail, so use the test XML as the result.
set -u

PLATFORM="${1:?usage: run-unity-tests.sh <EditMode|PlayMode> <filter> <name>}"
FILTER="${2:?filter required}"
NAME="${3:?result name required}"
UNITY="${UNITY:-}"
OUT="Builds/TestResults"

PROJECT_PATH="$(pwd)"
case "$(uname -s)" in
  MINGW*|MSYS*|CYGWIN*) PROJECT_PATH="$(pwd -W)" ;;
esac

if [[ -z "$UNITY" ]]; then
  UNITY_VERSION="$(sed -n 's/^m_EditorVersion: *//p' ProjectSettings/ProjectVersion.txt | tr -d '\r')"
  for candidate in \
    "${HOME:-}/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity" \
    "/opt/unity/editors/$UNITY_VERSION/Editor/Unity" \
    "/opt/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity" \
    "/c/Program Files/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity.exe"
  do
    if [[ -x "$candidate" ]]; then UNITY="$candidate"; break; fi
  done
fi

if [[ -z "$UNITY" || ! -x "$UNITY" ]]; then
  echo "run-unity-tests: Unity executable not found. Pass UNITY=/path/to/Unity." >&2
  exit 1
fi

mkdir -p "$OUT"
RESULT_FILE="$OUT/$NAME.xml"
LOG_FILE="$OUT/$NAME.log"
rm -f "$RESULT_FILE"
FILTER_ARGS=()
if [[ -n "$FILTER" ]]; then
  FILTER_ARGS=(-testFilter "$FILTER")
fi

set +e
"$UNITY" -batchmode -projectPath "$PROJECT_PATH" -runTests -testPlatform "$PLATFORM" \
  "${FILTER_ARGS[@]}" -testResults "$RESULT_FILE" -logFile "$LOG_FILE"
UNITY_STATUS=$?
set -e

if [[ ! -f "$RESULT_FILE" ]]; then
  echo "No test results; Unity exit=$UNITY_STATUS. Diagnostics from $LOG_FILE:"
  rg -n "error CS|readonly database|Failed to start|No tests|Exception" "$LOG_FILE" | head -30 || true
  exit 1
fi

python3 - "$RESULT_FILE" <<'PY'
import sys
import xml.etree.ElementTree as ET

path = sys.argv[1]
try:
    root = ET.parse(path).getroot()
except (ET.ParseError, OSError) as error:
    print(f"Invalid Unity test XML at {path}: {error}", file=sys.stderr)
    raise SystemExit(1)

for name in ("result", "total", "passed", "failed", "skipped"):
    print(f"{name}={root.attrib.get(name, 'missing')}")

failed = [case for case in root.iter("test-case") if case.attrib.get("result") == "Failed"]
for case in failed[:20]:
    print(f"FAILED {case.attrib.get('fullname', case.attrib.get('name', '<unnamed>'))}")
    message = case.find("./failure/message")
    if message is not None and message.text:
        print(message.text.strip())

try:
    total = int(root.attrib.get("total", "0"))
    failures = int(root.attrib.get("failed", "0"))
except ValueError:
    raise SystemExit("Unity test XML has invalid total/failed counts")

if total == 0 or failures != 0 or root.attrib.get("result") != "Passed":
    raise SystemExit(1)
PY
