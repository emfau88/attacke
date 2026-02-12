#!/usr/bin/env bash
set -euo pipefail
RUN_ID="${1:?run id required}"
LOG_TSV="${2:?log tsv required}"
OUT_DIR="${3:?out dir required}"
REVERTS="${4:-0}"

JSON_PATH="${OUT_DIR}/run_${RUN_ID}.json"
MD_PATH="${OUT_DIR}/run_${RUN_ID}.md"
SUMMARY_PATH="${OUT_DIR}/chat_summary_${RUN_ID}.txt"

completed=()
failed=()
first_failure=""
success_count=0
fail_count=0
skip_count=0

{
  echo "{"
  echo "  \"run_id\": \"${RUN_ID}\"," 
  echo "  \"generated_at\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"," 
  echo "  \"tickets\": ["

  first=1
  while IFS=$'\t' read -r ticket status checkpoint commit gate_code gate_status gate_detail error revert; do
    [[ -z "${ticket:-}" ]] && continue
    esc_error=$(printf '%s' "$error" | sed 's/\\/\\\\/g; s/"/\\"/g')
    esc_gate_detail=$(printf '%s' "$gate_detail" | sed 's/\\/\\\\/g; s/"/\\"/g')

    if [[ "$status" == "success" ]]; then
      completed+=("$ticket:$commit")
      success_count=$((success_count+1))
    elif [[ "$status" == "failed" ]]; then
      failed+=("$ticket:$error:$gate_code")
      fail_count=$((fail_count+1))
      if [[ -z "$first_failure" ]]; then
        first_failure="$ticket|$gate_status|$gate_detail|$error|checkpoint=$checkpoint"
      fi
    else
      skip_count=$((skip_count+1))
    fi

    if [[ $first -eq 0 ]]; then echo "    ,"; fi
    first=0
    echo "    {"
    echo "      \"ticket\": \"$ticket\"," 
    echo "      \"status\": \"$status\"," 
    echo "      \"checkpoint\": \"$checkpoint\"," 
    echo "      \"commit\": \"$commit\"," 
    echo "      \"gate_code\": \"$gate_code\"," 
    echo "      \"gate_status\": \"$gate_status\"," 
    echo "      \"gate_detail\": \"$esc_gate_detail\"," 
    echo "      \"error\": \"$esc_error\"," 
    echo "      \"revert\": \"$revert\""
    echo -n "    }"
  done < "$LOG_TSV"

  echo
  echo "  ]"
  echo "}"
} > "$JSON_PATH"

{
  echo "# Run Report ${RUN_ID}"
  echo
  echo "Generated: $(date -u '+%Y-%m-%d %H:%M:%S UTC')"
  echo
  echo "| Ticket | Status | Checkpoint | Commit | GateCode | GateStatus | GateDetail | Error | Revert |"
  echo "|---|---|---|---|---|---|---|---|---|"
  while IFS=$'\t' read -r ticket status checkpoint commit gate_code gate_status gate_detail error revert; do
    [[ -z "${ticket:-}" ]] && continue
    echo "| ${ticket} | ${status} | ${checkpoint} | ${commit} | ${gate_code} | ${gate_status} | ${gate_detail} | ${error} | ${revert} |"
  done < "$LOG_TSV"
} > "$MD_PATH"

{
  echo "Run ${RUN_ID}"
  echo "Counts: success=${success_count} fail=${fail_count} skip=${skip_count}"
  echo

  echo "Completed tickets range:"
  if [[ ${#completed[@]} -eq 0 ]]; then
    echo "- none"
  else
    printf -- '- %s\n' "${completed[@]}"
  fi

  echo
  echo "Failed tickets (reason/exitcode):"
  if [[ ${#failed[@]} -eq 0 ]]; then
    echo "- none"
  else
    printf -- '- %s\n' "${failed[@]}"
  fi

  echo
  echo "First failure:"
  if [[ -n "$first_failure" ]]; then
    echo "- ${first_failure}"
    echo "Next steps: inspect per-ticket apply/gate/ci logs and fix the smallest root cause before retry."
  else
    echo "- none"
  fi

  echo
  echo "Reverts performed: ${REVERTS}"
  echo "Final git status:"
  git status --short || true
  echo
  echo "Reports:"
  echo "- ${MD_PATH}"
  echo "- ${JSON_PATH}"
} | sed -n '1,200p' > "$SUMMARY_PATH"

echo "$JSON_PATH"
echo "$MD_PATH"
echo "$SUMMARY_PATH"
