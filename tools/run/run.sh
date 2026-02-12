#!/usr/bin/env bash
set -euo pipefail
PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
STATE_FILE="${PROJECT_ROOT}/tools/run/state.json"
REPORT_DIR="${PROJECT_ROOT}/reports"
APPLY_SCRIPT="${PROJECT_ROOT}/tools/run/apply_ticket.sh"
GATES_SCRIPT="${PROJECT_ROOT}/tools/run/gates.sh"
ROLLBACK_SCRIPT="${PROJECT_ROOT}/tools/run/rollback.sh"

cd "$PROJECT_ROOT"

if ! command -v git >/dev/null 2>&1; then
  echo "preflight: git not found" >&2
  exit 2
fi

if [[ -n "$(git status --porcelain)" ]]; then
  echo "preflight: working tree not clean" >&2
  git status --short >&2
  exit 3
fi

git pull --rebase origin main

RUN_ID="$(date -u +%Y%m%dT%H%M%SZ)"
LOG_TSV="${REPORT_DIR}/run_${RUN_ID}.tsv"
: > "$LOG_TSV"

NEXT_TICKET=$(grep -o '"next_ticket"[[:space:]]*:[[:space:]]*"[^"]*"' "$STATE_FILE" | sed 's/.*"\([^"]*\)"$/\1/')
if [[ -z "$NEXT_TICKET" ]]; then NEXT_TICKET="T001"; fi

mapfile -t ALL_TICKETS < <(find tickets -maxdepth 1 -type f -name 'T*.md' -printf '%f\n' | sed 's/\.md$//' | sort)

start_index=0
for i in "${!ALL_TICKETS[@]}"; do
  if [[ "${ALL_TICKETS[$i]}" == "$NEXT_TICKET" ]]; then
    start_index="$i"
    break
  fi
done

for ((i=start_index; i<${#ALL_TICKETS[@]}; i++)); do
  ticket="${ALL_TICKETS[$i]}"
  ticket_file="tickets/${ticket}.md"
  checkpoint="$(git rev-parse --short HEAD)"

  tmp_state="${STATE_FILE}.tmp"
  cat > "$tmp_state" <<STATE
{
  "next_ticket": "${ticket}",
  "last_checkpoint": "${checkpoint}",
  "last_run_report": "",
  "updated_at": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
STATE
  mv "$tmp_state" "$STATE_FILE"

  set +e
  "$APPLY_SCRIPT" "$ticket_file" >"${REPORT_DIR}/${ticket}_apply.log" 2>&1
  apply_rc=$?
  set -e

  if [[ $apply_rc -ne 0 ]]; then
    "$ROLLBACK_SCRIPT" "$checkpoint"
    err_line="apply_failed_rc_${apply_rc}"
    echo -e "${ticket}\tfailed\t${checkpoint}\t-\t-\t${err_line}" >> "$LOG_TSV"
    continue
  fi

  set +e
  "$GATES_SCRIPT" >"${REPORT_DIR}/${ticket}_gates.log" 2>&1
  gate_rc=$?
  set -e
  if [[ $gate_rc -ne 0 ]]; then
    "$ROLLBACK_SCRIPT" "$checkpoint"
    err_line="gate_failed_rc_${gate_rc}"
    echo -e "${ticket}\tfailed\t${checkpoint}\t-\t${gate_rc}\t${err_line}" >> "$LOG_TSV"
    continue
  fi

  if [[ -n "$(git status --porcelain)" ]]; then
    git add -A
    git commit -m "run(${ticket}): apply ticket"
    git push origin main
    commit_hash="$(git rev-parse --short HEAD)"
  else
    commit_hash="$(git rev-parse --short HEAD)"
  fi

  next="DONE"
  if (( i + 1 < ${#ALL_TICKETS[@]} )); then
    next="${ALL_TICKETS[$((i+1))]}"
  fi
  cat > "$tmp_state" <<STATE
{
  "next_ticket": "${next}",
  "last_checkpoint": "${commit_hash}",
  "last_run_report": "",
  "updated_at": "$(date -u +%Y-%m-%dT%H:%M:%SZ)"
}
STATE
  mv "$tmp_state" "$STATE_FILE"

  echo -e "${ticket}\tsuccess\t${checkpoint}\t${commit_hash}\t0\t-" >> "$LOG_TSV"
done

readarray -t report_paths < <("${PROJECT_ROOT}/tools/run/report.sh" "$RUN_ID" "$LOG_TSV" "$REPORT_DIR")
json_path="${report_paths[0]}"
md_path="${report_paths[1]}"

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

echo "run_complete"
echo "json_report=${json_path}"
echo "md_report=${md_path}"
