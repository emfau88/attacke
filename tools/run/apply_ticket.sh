#!/usr/bin/env bash
set -euo pipefail

TICKET_FILE="${1:-}"
if [[ -z "$TICKET_FILE" || ! -f "$TICKET_FILE" ]]; then
  echo "[apply_ticket] ticket file missing: $TICKET_FILE" >&2
  exit 11
fi

ROOT="$(git rev-parse --show-toplevel)"
cd "$ROOT"

tmpdir="$(mktemp -d)"
trap 'rm -rf "$tmpdir"' EXIT

extract_section() {
  local section="$1"
  local out="$2"
  awk -v sec="## ${section}" '
    $0 == sec { insec=1; next }
    /^## / && insec { exit }
    insec { print }
  ' "$TICKET_FILE" > "$out"
}

validate_relpath() {
  local rel="$1"
  if [[ "$rel" = /* || "$rel" == *".."* ]]; then
    return 1
  fi
  return 0
}

extract_heredoc() {
  local start_line="$1"
  local src="$2"
  local out="$3"
  awk -v s="$start_line" 'NR>=s' "$src" | awk '
    NR==1 {
      if ($0 !~ /^<<<EOF$/) exit 21
      in=1
      next
    }
    in {
      if ($0 == "EOF") exit 0
      print
    }
    END { if (in) exit 22 }
  ' > "$out"
}

extract_section "ACTIONS" "$tmpdir/actions.txt"
extract_section "PATCH" "$tmpdir/patch.txt"

has_actions=0
has_patch=0
[[ -s "$tmpdir/actions.txt" ]] && has_actions=1
[[ -s "$tmpdir/patch.txt" ]] && has_patch=1

if [[ $has_actions -eq 0 && $has_patch -eq 0 ]]; then
  echo "[apply_ticket] no ACTIONS or PATCH section found" >&2
  exit 11
fi

if [[ $has_actions -eq 1 ]]; then
  echo "[apply_ticket] ACTIONS start"
  mapfile -t lines < "$tmpdir/actions.txt"
  i=0
  while [[ $i -lt ${#lines[@]} ]]; do
    line="${lines[$i]}"
    i=$((i+1))

    [[ -z "${line// }" ]] && continue
    [[ "$line" =~ ^[[:space:]]*# ]] && continue

    if [[ "$line" =~ ^MKDIR[[:space:]]+(.+)$ ]]; then
      rel="${BASH_REMATCH[1]}"
      validate_relpath "$rel" || { echo "[apply_ticket] invalid path in MKDIR: $rel" >&2; exit 11; }
      mkdir -p "$rel"
      echo "[apply_ticket] MKDIR $rel"
      continue
    fi

    if [[ "$line" == WRITE*' <<<EOF' ]]; then
      rel="${line#WRITE }"
      rel="${rel% <<<EOF}"
      validate_relpath "$rel" || { echo "[apply_ticket] invalid path in WRITE: $rel" >&2; exit 11; }
      content_file="$tmpdir/write_${i}.txt"
      {
        while [[ $i -lt ${#lines[@]} ]]; do
          l="${lines[$i]}"
          i=$((i+1))
          if [[ "$l" == "EOF" ]]; then
            break
          fi
          printf '%s\n' "$l"
        done
      } > "$content_file"
      mkdir -p "$(dirname "$rel")"
      cat "$content_file" > "$rel"
      echo "[apply_ticket] WRITE $rel"
      continue
    fi

    if [[ "$line" =~ ^REPLACE[[:space:]]+(.+)[[:space:]]+PATTERN$ ]]; then
      rel="${BASH_REMATCH[1]}"
      validate_relpath "$rel" || { echo "[apply_ticket] invalid path in REPLACE: $rel" >&2; exit 11; }
      [[ -f "$rel" ]] || { echo "[apply_ticket] REPLACE target not found: $rel" >&2; exit 12; }

      [[ $i -lt ${#lines[@]} ]] || { echo "[apply_ticket] malformed REPLACE pattern block" >&2; exit 11; }
      [[ "${lines[$i]}" == "<<<EOF" ]] || { echo "[apply_ticket] expected <<<EOF after PATTERN" >&2; exit 11; }
      i=$((i+1))
      pattern_file="$tmpdir/pattern_${i}.txt"
      {
        while [[ $i -lt ${#lines[@]} ]]; do
          l="${lines[$i]}"
          i=$((i+1))
          if [[ "$l" == "EOF" ]]; then
            break
          fi
          printf '%s\n' "$l"
        done
      } > "$pattern_file"

      [[ $i -lt ${#lines[@]} ]] || { echo "[apply_ticket] malformed REPLACE replacement header" >&2; exit 11; }
      [[ "${lines[$i]}" == "REPLACEMENT <<<EOF" ]] || { echo "[apply_ticket] expected REPLACEMENT <<<EOF" >&2; exit 11; }
      i=$((i+1))
      replacement_file="$tmpdir/replacement_${i}.txt"
      {
        while [[ $i -lt ${#lines[@]} ]]; do
          l="${lines[$i]}"
          i=$((i+1))
          if [[ "$l" == "EOF" ]]; then
            break
          fi
          printf '%s\n' "$l"
        done
      } > "$replacement_file"

      tmp_out="$tmpdir/replaced_${i}.txt"
      set +e
      awk -v pat="$(cat "$pattern_file")" -v rep="$(cat "$replacement_file")" '
        BEGIN { found = 0; buf = "" }
        { buf = buf $0 ORS }
        END {
          pos = index(buf, pat)
          if (pos == 0) exit 12
          printf "%s%s%s", substr(buf, 1, pos - 1), rep, substr(buf, pos + length(pat))
        }
      ' "$rel" > "$tmp_out"
      rc=$?
      set -e
      if [[ $rc -eq 12 ]]; then
        echo "[apply_ticket] REPLACE pattern not found in $rel" >&2
        exit 12
      elif [[ $rc -ne 0 ]]; then
        echo "[apply_ticket] REPLACE failed in $rel" >&2
        exit 11
      fi
      cat "$tmp_out" > "$rel"

      echo "[apply_ticket] REPLACE $rel"
      continue
    fi

    echo "[apply_ticket] invalid ACTION: $line" >&2
    exit 11
  done
  echo "[apply_ticket] ACTIONS done"
fi

if [[ $has_patch -eq 1 ]]; then
  echo "[apply_ticket] PATCH start"
  sed '/^[[:space:]]*$/d' "$tmpdir/patch.txt" > "$tmpdir/patch.clean"
  if [[ -s "$tmpdir/patch.clean" ]]; then
    if ! git apply --whitespace=fix "$tmpdir/patch.clean"; then
      echo "[apply_ticket] PATCH apply failed" >&2
      exit 13
    fi
    echo "[apply_ticket] PATCH applied"
  else
    echo "[apply_ticket] PATCH section empty, skipped"
  fi
fi

echo "[apply_ticket] success: $(basename "$TICKET_FILE")"
exit 0
