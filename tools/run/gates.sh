#!/usr/bin/env bash
set -euo pipefail
PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$PROJECT_ROOT"

# Gate 1: forbidden folders must not be tracked
if git ls-files | grep -E '^(Library|Temp|Obj|Logs)/' >/dev/null; then
  echo "GATE_FAIL: forbidden folders tracked" >&2
  git ls-files | grep -E '^(Library|Temp|Obj|Logs)/' >&2 || true
  exit 11
fi

# Gate 2: Unity compile gate (optional)
UNITY_BIN=""
if [[ -x "/opt/unity/2022.3.30f1/Unity" ]]; then
  UNITY_BIN="/opt/unity/2022.3.30f1/Unity"
elif command -v unity >/dev/null 2>&1; then
  UNITY_BIN="$(command -v unity)"
fi

if [[ -n "$UNITY_BIN" ]]; then
  LOG_PATH="${PROJECT_ROOT}/reports/unity_gate_latest.log"
  set +e
  "$UNITY_BIN" -batchmode -nographics -quit -projectPath "$PROJECT_ROOT" -logFile "$LOG_PATH"
  RC=$?
  set -e

  if grep -q "error CS" "$LOG_PATH"; then
    echo "GATE_FAIL: Unity compile errors detected" >&2
    grep -n "error CS" "$LOG_PATH" >&2 || true
    exit 12
  fi

  if [[ $RC -ne 0 ]]; then
    echo "GATE_WARN: Unity exited with code $RC but no C# compile errors found" >&2
  else
    echo "GATE_OK: Unity compile gate passed"
  fi
else
  echo "GATE_WARN: Unity CLI not found, compile gate skipped"
fi

exit 0
