#!/usr/bin/env bash
set -euo pipefail
RUN_ID="${1:?run id required}"
LOG_TSV="${2:?log tsv required}"
OUT_DIR="${3:?out dir required}"

JSON_PATH="${OUT_DIR}/run_${RUN_ID}.json"
MD_PATH="${OUT_DIR}/run_${RUN_ID}.md"

{
  echo "{"
  echo "  \"run_id\": \"${RUN_ID}\"," 
  echo "  \"generated_at\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"," 
  echo "  \"tickets\": ["

  first=1
  while IFS=$'\t' read -r ticket status checkpoint commit gate_code error; do
    [[ -z "${ticket:-}" ]] && continue
    esc_error=$(printf '%s' "$error" | sed 's/\\/\\\\/g; s/"/\\"/g')
    if [[ $first -eq 0 ]]; then echo "    ,"; fi
    first=0
    echo "    {"
    echo "      \"ticket\": \"$ticket\"," 
    echo "      \"status\": \"$status\"," 
    echo "      \"checkpoint\": \"$checkpoint\"," 
    echo "      \"commit\": \"$commit\"," 
    echo "      \"gate_code\": \"$gate_code\"," 
    echo "      \"error\": \"$esc_error\""
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
  echo "| Ticket | Status | Checkpoint | Commit | Gate | Error |"
  echo "|---|---|---|---|---|---|"
  while IFS=$'\t' read -r ticket status checkpoint commit gate_code error; do
    [[ -z "${ticket:-}" ]] && continue
    echo "| ${ticket} | ${status} | ${checkpoint} | ${commit} | ${gate_code} | ${error} |"
  done < "$LOG_TSV"
} > "$MD_PATH"

echo "$JSON_PATH"
echo "$MD_PATH"
