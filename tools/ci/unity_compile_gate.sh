#!/usr/bin/env bash
set -euo pipefail

UNITY_BIN="/opt/unity/2022.3.30f1/Unity"
PROJECT_PATH="/home/ubuntu/workspace/attacke"
LOG_PATH="/home/ubuntu/workspace/attacke/tools/ci/unity_compile_gate.log"
TIMEOUT_SEC=900

run_once() {
  timeout "$TIMEOUT_SEC" "$UNITY_BIN" \
    -batchmode -nographics -quit \
    -projectPath "$PROJECT_PATH" \
    -executeMethod CI.CompileGate.Run \
    -logFile "$LOG_PATH"
}

set +e
run_once
rc=$?
set -e

if grep -qi "another Unity instance is running" "$LOG_PATH" 2>/dev/null; then
  sleep 60
  set +e
  run_once
  rc=$?
  set -e
fi

if [[ $rc -ne 0 ]]; then
  echo "UNITY COMPILE GATE: FAIL"
  tail -n 200 "$LOG_PATH" || true
  exit 1
fi

if grep -Eqi "error CS|Compilation failed|Scripts have compiler errors|Error while compiling" "$LOG_PATH"; then
  echo "UNITY COMPILE GATE: FAIL"
  tail -n 200 "$LOG_PATH" || true
  exit 1
fi

echo "UNITY COMPILE GATE: PASS"
grep -Eim1 "Unity.*[0-9]+\.[0-9]+" "$LOG_PATH" || true
exit 0
