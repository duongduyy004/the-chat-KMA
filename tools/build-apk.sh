#!/usr/bin/env bash
# Build the KMA Android player for ARM64 and/or x86_64 in one headless Unity session.
# Linux counterpart of tools/build-apk.ps1.
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ABI="arm64"
OUTPUT_DIR="Builds/Android"
BASE_NAME="kma"
UNITY="${KMA_UNITY_EDITOR:-}"
LOG_FILE=""

usage() {
  cat <<'USAGE'
Usage: tools/build-apk.sh [options]

  --abi <list>        arm64 | x86_64 | all | comma-separated (default: arm64)
  --arm64             shorthand for --abi arm64
  --x86_64            shorthand for --abi x86_64  (Android emulators)
  --output-dir <dir>  APK directory, relative to the project root (default: Builds/Android)
  --name <base>       APK base name; files land at <base>-<abi>.apk (default: kma)
  --unity <path>      Unity executable (default: $KMA_UNITY_EDITOR, else Unity Hub lookup)
  --log <path>        Unity log file (default: <output-dir>/build-apk.log)
  -h, --help          this message

The Unity Editor must be closed: batchmode cannot open a locked project.
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --abi)        ABI="${2:?--abi needs a value}"; shift 2 ;;
    --arm64)      ABI="arm64"; shift ;;
    --x86_64)     ABI="x86_64"; shift ;;
    --output-dir) OUTPUT_DIR="${2:?--output-dir needs a value}"; shift 2 ;;
    --name)       BASE_NAME="${2:?--name needs a value}"; shift 2 ;;
    --unity)      UNITY="${2:?--unity needs a value}"; shift 2 ;;
    --log)        LOG_FILE="${2:?--log needs a value}"; shift 2 ;;
    -h|--help)    usage; exit 0 ;;
    *) echo "build-apk: unknown option '$1'" >&2; usage >&2; exit 2 ;;
  esac
done

cd "$PROJECT_ROOT"

UNITY_VERSION="$(sed -n 's/^m_EditorVersion: *//p' ProjectSettings/ProjectVersion.txt | tr -d '\r')"
if [[ -z "$UNITY_VERSION" ]]; then
  echo "build-apk: cannot read the editor version from ProjectSettings/ProjectVersion.txt" >&2
  exit 1
fi

if [[ -z "$UNITY" ]]; then
  for candidate in \
    "$HOME/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity" \
    "$HOME/Applications/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity" \
    "/opt/unity/editors/$UNITY_VERSION/Editor/Unity" \
    "/opt/Unity/Hub/Editor/$UNITY_VERSION/Editor/Unity" \
    "/usr/share/unity3d/Editor/Unity"
  do
    if [[ -x "$candidate" ]]; then UNITY="$candidate"; break; fi
  done
fi

if [[ -z "$UNITY" || ! -x "$UNITY" ]]; then
  echo "build-apk: Unity $UNITY_VERSION not found." >&2
  echo "  Pass --unity /path/to/Editor/Unity or export KMA_UNITY_EDITOR." >&2
  exit 1
fi

if [[ -f Temp/UnityLockfile ]]; then
  echo "build-apk: Temp/UnityLockfile exists — close the Unity Editor before building." >&2
  exit 1
fi

mkdir -p "$OUTPUT_DIR"
LOG_FILE="${LOG_FILE:-$OUTPUT_DIR/build-apk.log}"
mkdir -p "$(dirname "$LOG_FILE")"

echo "Unity   : $UNITY ($UNITY_VERSION)"
echo "ABI     : $ABI"
echo "Output  : $OUTPUT_DIR/$BASE_NAME-<abi>.apk"
echo "Log     : $LOG_FILE"
echo

status=0
set +e
"$UNITY" \
  -batchmode -nographics -quit \
  -projectPath "$PROJECT_ROOT" \
  -executeMethod KMA.EditorTools.AndroidBuildMatrix.Build \
  -androidAbi "$ABI" \
  -buildOutputDir "$OUTPUT_DIR" \
  -buildName "$BASE_NAME" \
  -logFile - 2>&1 | tee "$LOG_FILE"
pipeline_status=("${PIPESTATUS[@]}")
status=${pipeline_status[0]}
tee_status=${pipeline_status[1]:-0}
set -e

if [[ $status -eq 0 && $tee_status -ne 0 ]]; then
  status=$tee_status
fi

if [[ $status -ne 0 ]]; then
  echo "build-apk: Unity exited with $status. Last 40 log lines:" >&2
  tail -n 40 "$LOG_FILE" >&2 || true
  exit "$status"
fi

echo
for apk in "$OUTPUT_DIR/$BASE_NAME"-*.apk; do
  [[ -f "$apk" ]] || continue
  printf '%s  %s bytes  sha256=%s\n' \
    "$apk" "$(stat -c %s "$apk")" "$(sha256sum "$apk" | cut -d' ' -f1)"
done
