#!/usr/bin/env bash
set -euo pipefail
PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
STATE_FILE="${PROJECT_ROOT}/tools/run/state.json"
REPORT_DIR="${PROJECT_ROOT}/reports"
APPLY_SCRIPT="${PROJECT_ROOT}/tools/run/apply_ticket.sh"
GATES_SCRIPT="${PROJECT_ROOT}/tools/run/gates.sh"
ROLLBACK_SCRIPT="${PROJECT_ROOT}/tools/run/rollback.sh"

cd "$PROJECT_ROOT"
mkdir -p "$REPORT_DIR"

CURRENT_TICKET="-"
PHASE="precheck"
LAST_COMMIT="$(git rev-parse --short HEAD 2>/dev/null || echo '-')"
RUN_ID="$(date -u +%Y%m%dT%H%M%SZ)"
LOG_TSV="${REPORT_DIR}/run_${RUN_ID}.tsv"
: > "$LOG_TSV"

json_path=""
md_path=""
chat_summary=""
reverts=0
FINALIZED=0

log_heartbeat() {
  local clean="yes"
  if [[ -n "$(git status --porcelain)" ]]; then clean="no"; fi
  LAST_COMMIT="$(git rev-parse --short HEAD 2>/dev/null || echo '-')"
  echo "$(date -u +%Y-%m-%dT%H:%M:%SZ) ticket=${CURRENT_TICKET} phase=${PHASE} last_commit=${LAST_COMMIT} git_clean=${clean}" >> "${REPORT_DIR}/heartbeat.log"
}

set_phase() {
  local p="$1"
  PHASE="$p"
  log_heartbeat
}

heartbeat_loop() {
  while true; do
    log_heartbeat
    sleep 60
  done
}

finalize() {
  local exit_code=$?

  if [[ -n "${HEARTBEAT_PID:-}" ]]; then
    kill "$HEARTBEAT_PID" >/dev/null 2>&1 || true
  fi

  if [[ "$FINALIZED" -eq 0 ]]; then
    set_phase "report"
    if [[ -s "$LOG_TSV" ]] || [[ ! -f "$chat_summary" ]]; then
      readarray -t report_paths < <("${PROJECT_ROOT}/tools/run/report.sh" "$RUN_ID" "$LOG_TSV" "$REPORT_DIR" "$reverts")
      json_path="${report_paths[0]}"
      md_path="${report_paths[1]}"
      chat_summary="${report_paths[2]}"
    fi

    if [[ -f "$chat_summary" ]]; then
      echo "run_complete"
      echo "json_report=${json_path}"
      echo "md_report=${md_path}"
      echo "chat_summary=${chat_summary}"
      echo "----CHAT_SUMMARY_START----"
      cat "$chat_summary"
      echo "----CHAT_SUMMARY_END----"
    fi

    echo "----RUN_SELF_CHECK----"
    tail -n 5 "${REPORT_DIR}/heartbeat.log" 2>/dev/null || true
    ls -1t "${REPORT_DIR}"/run_*.tsv 2>/dev/null | head -n 1 || true
    echo "----RUN_SELF_CHECK_END----"

    FINALIZED=1
  fi

  set_phase "done"
  exit "$exit_code"
}

heartbeat_loop &
HEARTBEAT_PID=$!
trap finalize EXIT

if ! command -v git >/dev/null 2>&1; then
  echo "preflight: git not found" >&2
  exit 2
fi

set_phase "precheck"
git fetch origin main

# dirty recovery path
if [[ -n "$(git status --porcelain)" ]]; then
  SNAP_DIR="${REPORT_DIR}/dirty_snapshot_$(date -u +%Y%m%dT%H%M%SZ)"
  mkdir -p "$SNAP_DIR"
  git status --short > "${SNAP_DIR}/status.txt" || true
  git diff > "${SNAP_DIR}/diff.patch" || true
  git ls-files --others --exclude-standard > "${SNAP_DIR}/untracked.txt" || true

  while IFS= read -r f; do
    [[ -z "$f" ]] && continue
    if [[ -f "$f" ]]; then
      mime="$(file --mime-type -b "$f" || true)"
      size="$(wc -c < "$f" 2>/dev/null || echo 0)"
      if [[ "$mime" == text/* && "$size" -le 524288 ]]; then
        mkdir -p "${SNAP_DIR}/files/$(dirname "$f")"
        cp "$f" "${SNAP_DIR}/files/$f"
      fi
    fi
  done < "${SNAP_DIR}/untracked.txt"

  git reset --hard origin/main
  git clean -fd -e reports/ -e reports/*
fi

git checkout main
git pull --rebase origin main

mapfile -t ALL_TICKETS < <(find tickets -maxdepth 1 -type f -name 'T*.md' -printf '%f\n' | sed 's/\.md$//' | sort)

NEXT_TICKET=$(grep -o '"next_ticket"[[:space:]]*:[[:space:]]*"[^"]*"' "$STATE_FILE" | sed 's/.*"\([^"]*\)"$/\1/' || true)
[[ -z "$NEXT_TICKET" ]] && NEXT_TICKET="T001"

CLI_FROM="${1:-}"
CLI_UNTIL="${2:-}"
if [[ -n "$CLI_FROM" ]]; then
  NEXT_TICKET="$CLI_FROM"
elif [[ -n "${RUN_FROM:-}" ]]; then
  NEXT_TICKET="$RUN_FROM"
fi

RUN_UNTIL_TICKET=""
if [[ -n "$CLI_UNTIL" ]]; then
  RUN_UNTIL_TICKET="$CLI_UNTIL"
elif [[ -n "${RUN_UNTIL:-}" ]]; then
  RUN_UNTIL_TICKET="${RUN_UNTIL}"
fi

LATEST_TSV="$(ls -1t ${REPORT_DIR}/run_*.tsv 2>/dev/null | head -n1 || true)"
if [[ "$NEXT_TICKET" == "DONE" && -n "$LATEST_TSV" ]]; then
  for t in "${ALL_TICKETS[@]}"; do
    line="$(grep -E "^${t}[[:space:]]" "$LATEST_TSV" | tail -n1 || true)"
    if [[ -z "$line" ]]; then
      NEXT_TICKET="$t"; break
    fi
    st="$(echo "$line" | cut -f2)"
    if [[ "$st" != "success" ]]; then
      NEXT_TICKET="$t"; break
    fi
  done
fi

start_index=-1
for i in "${!ALL_TICKETS[@]}"; do
  if [[ "${ALL_TICKETS[$i]}" == "$NEXT_TICKET" ]]; then
    start_index="$i"
    break
  fi
done
[[ $start_index -lt 0 ]] && start_index=0

for ((i=start_index; i<${#ALL_TICKETS[@]}; i++)); do
  ticket="${ALL_TICKETS[$i]}"
  if [[ -n "$RUN_UNTIL_TICKET" && "$ticket" > "$RUN_UNTIL_TICKET" ]]; then
    break
  fi

  ticket_file="tickets/${ticket}.md"
  CURRENT_TICKET="$ticket"
  LAST_COMMIT="$(git rev-parse --short HEAD)"

  checkpoint="$(git rev-parse --short HEAD)"

  cat > "${STATE_FILE}.tmp" <<STATE
{
  "next_ticket": "${ticket}",
  "last_checkpoint": "${checkpoint}",
  "last_run_report": "",
  "updated_at": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
STATE
  mv "${STATE_FILE}.tmp" "$STATE_FILE"

  set_phase "apply"
  set +e
  "$APPLY_SCRIPT" "$ticket_file" >"${REPORT_DIR}/${ticket}_apply.log" 2>&1
  apply_rc=$?
  set -e

  if [[ $apply_rc -ne 0 ]]; then
    set_phase "report"
    "$ROLLBACK_SCRIPT" "$checkpoint"
    reverts=$((reverts+1))
    echo -e "${ticket}\tfailed\t${checkpoint}\t-\t-\tskipped\tapply_failed\tapply_failed_rc_${apply_rc}\tyes" >> "$LOG_TSV"
    continue
  fi

  if [[ "$ticket" > "T240" && "$ticket" < "T261" ]]; then
    assets_changed=0
    if git diff --name-only | grep -q '^Assets/'; then
      assets_changed=1
    fi
    if git ls-files --others --exclude-standard Assets | grep -q '.'; then
      assets_changed=1
    fi
    if [[ "$assets_changed" -eq 0 ]]; then
      set_phase "report"
      "$ROLLBACK_SCRIPT" "$checkpoint"
      reverts=$((reverts+1))
      echo -e "${ticket}\tfailed\t${checkpoint}\t-\t12\tfailed\tassets_change_required\tno_assets_change_detected\tyes" >> "$LOG_TSV"
      continue
    fi
  fi

  set_phase "gate"
  set +e
  GATE_EXPECT_CLEAN=0 "$GATES_SCRIPT" >"${REPORT_DIR}/${ticket}_gates.log" 2>&1
  gate_rc=$?
  set -e

  gate_status="$(grep '^GATE_STATUS=' "${REPORT_DIR}/${ticket}_gates.log" | tail -n1 | cut -d= -f2- || true)"
  gate_detail="$(grep '^GATE_DETAIL=' "${REPORT_DIR}/${ticket}_gates.log" | tail -n1 | cut -d= -f2- || true)"
  [[ -z "$gate_status" ]] && gate_status="failed"
  [[ -z "$gate_detail" ]] && gate_detail="gate_log_parse_failed"

  if [[ $gate_rc -ne 0 ]]; then
    set_phase "report"
    "$ROLLBACK_SCRIPT" "$checkpoint"
    reverts=$((reverts+1))
    echo -e "${ticket}\tfailed\t${checkpoint}\t-\t${gate_rc}\t${gate_status}\t${gate_detail}\tgate_failed_rc_${gate_rc}\tyes" >> "$LOG_TSV"
    continue
  fi

  commit_hash="$(git rev-parse --short HEAD)"
  if [[ -n "$(git status --porcelain)" ]]; then
    set_phase "commit"
    git add -u

    mapfile -t NEW_FILES < <(git ls-files --others --exclude-standard | grep -Ev '^(reports/|tools/run/state\.json$)' || true)
    if [[ ${#NEW_FILES[@]} -gt 0 ]]; then
      git add -- "${NEW_FILES[@]}"
    fi

    if [[ -n "$(git diff --cached --name-only)" ]]; then
      set_phase "ci"
      if ! bash "${PROJECT_ROOT}/tools/ci/ci_run.sh" >"${REPORT_DIR}/${ticket}_ci.log" 2>&1; then
        set_phase "report"
        "$ROLLBACK_SCRIPT" "$checkpoint"
        reverts=$((reverts+1))
        echo -e "${ticket}\tfailed\t${checkpoint}\t-\t1\tfailed\tci_gate_failed\tci_run_failed\tyes" >> "$LOG_TSV"
        continue
      fi

      set_phase "commit"
      git commit -m "run(${ticket}): apply ticket"
      set_phase "push"
      git push origin main
      commit_hash="$(git rev-parse --short HEAD)"
      LAST_COMMIT="$commit_hash"
    fi
  fi

  echo -e "${ticket}\tsuccess\t${checkpoint}\t${commit_hash}\t0\t${gate_status}\t${gate_detail}\t-\tno" >> "$LOG_TSV"

  next="DONE"
  if (( i + 1 < ${#ALL_TICKETS[@]} )); then
    next="${ALL_TICKETS[$((i+1))]}"
  fi
  cat > "${STATE_FILE}.tmp" <<STATE
{
  "next_ticket": "${next}",
  "last_checkpoint": "${commit_hash}",
  "last_run_report": "",
  "updated_at": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
STATE
  mv "${STATE_FILE}.tmp" "$STATE_FILE"
done

set_phase "report"
readarray -t report_paths < <("${PROJECT_ROOT}/tools/run/report.sh" "$RUN_ID" "$LOG_TSV" "$REPORT_DIR" "$reverts")
json_path="${report_paths[0]}"
md_path="${report_paths[1]}"
chat_summary="${report_paths[2]}"

current_head="$(git rev-parse --short HEAD)"
cat > "${STATE_FILE}.tmp" <<STATE
{
  "next_ticket": "DONE",
  "last_checkpoint": "${current_head}",
  "last_run_report": "${json_path}",
  "updated_at": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
STATE
mv "${STATE_FILE}.tmp" "$STATE_FILE"

set_phase "done"
