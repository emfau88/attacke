#!/usr/bin/env bash
set -euo pipefail
PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$PROJECT_ROOT"

DETAIL=""
STATUS="ok"

emit() {
  echo "GATE_STATUS=${STATUS}"
  echo "GATE_DETAIL=${DETAIL}"
}

# Gate 1: working tree clean (can be overridden during apply-phase)
EXPECT_CLEAN="${GATE_EXPECT_CLEAN:-1}"
if [[ "$EXPECT_CLEAN" == "1" ]]; then
  if [[ -n "$(git status --porcelain)" ]]; then
    STATUS="failed"
    DETAIL="git_status_not_clean"
    emit
    exit 30
  fi
fi

resolve_unity() {
  if [[ -n "${UNITY_PATH:-}" && -x "${UNITY_PATH}" ]]; then
    echo "$UNITY_PATH"; return 0
  fi

  local candidates=(
    "/opt/unity/2022.3.30f1/Unity"
    "/opt/Unity/Editor/Unity"
    "/usr/bin/unity-editor"
    "/usr/local/bin/unity-editor"
    "/usr/bin/unity"
    "/usr/local/bin/unity"
  )

  for c in "${candidates[@]}"; do
    if [[ -x "$c" ]]; then echo "$c"; return 0; fi
  done

  if command -v unity-editor >/dev/null 2>&1; then
    command -v unity-editor; return 0
  fi
  if command -v unity >/dev/null 2>&1; then
    command -v unity; return 0
  fi

  return 1
}

UNITY_BIN=""
if UNITY_BIN="$(resolve_unity)"; then
  :
else
  STATUS="skipped"
  DETAIL="unity_not_found"
  emit
  exit 0
fi

LOG_PATH="${PROJECT_ROOT}/reports/unity_gate_latest.log"
TIMEOUT_SEC=900

run_compile_once() {
  local out_rc=0
  timeout "$TIMEOUT_SEC" "$UNITY_BIN" \
    -batchmode -nographics -quit \
    -projectPath "$PROJECT_ROOT" \
    -executeMethod CI.CompileGate.Run \
    -logFile "$LOG_PATH"
  out_rc=$?
  return $out_rc
}

set +e
run_compile_once
rc=$?
set -e

if grep -qi "another Unity instance is running" "$LOG_PATH"; then
  echo "GATE_INFO=unity_lock_detected_retrying"
  sleep 60
  set +e
  run_compile_once
  rc=$?
  set -e
fi

if [[ $rc -eq 124 ]]; then
  STATUS="failed"
  DETAIL="unity_timeout_${TIMEOUT_SEC}s"
  emit
  exit 21
fi

if grep -q "error CS" "$LOG_PATH" || grep -qi "Compilation failed" "$LOG_PATH"; then
  STATUS="failed"
  DETAIL="compile_errors_in_log"
  emit
  exit 20
fi

if [[ $rc -eq 20 ]]; then
  STATUS="failed"
  DETAIL="compile_gate_exit_20"
  emit
  exit 20
fi

if [[ $rc -eq 21 ]]; then
  STATUS="failed"
  DETAIL="compile_gate_exit_21"
  emit
  exit 21
fi

if [[ $rc -ne 0 ]]; then
  STATUS="failed"
  DETAIL="unity_batch_exit_${rc}"
  emit
  exit 21
fi

STATUS="ok"
DETAIL="unity_compile_ok"
emit
exit 0
