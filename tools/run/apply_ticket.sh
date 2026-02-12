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

count_literal_matches() {
  local file="$1"
  local pattern_file="$2"
  awk -v pat="$(cat "$pattern_file")" '
    BEGIN { count=0 }
    {
      buf = buf $0 ORS
    }
    END {
      if (length(pat) == 0) { print 0; exit }
      s = buf
      while ((pos = index(s, pat)) > 0) {
        count++
        s = substr(s, pos + length(pat))
      }
      print count
    }
  ' "$file"
}

normalize_text_file() {
  local in_file="$1"
  local out_file="$2"
  awk '{
    sub(/\r$/, "")
    sub(/[ \t]+$/, "")
    print
  }' "$in_file" > "$out_file"
}

log_context_window() {
  local src_file="$1"
  local pattern_file="$2"
  local mode="$3"

  local needle
  needle="$(head -n1 "$pattern_file")"
  [[ -z "$needle" ]] && return 0

  mapfile -t hit_lines < <(grep -nF "$needle" "$src_file" | cut -d: -f1 || true)
  if [[ ${#hit_lines[@]} -eq 0 ]]; then
    echo "[apply_ticket] REPLACE_DIAG_CONTEXT mode=${mode} context=none"
    return 0
  fi

  local first_line="${hit_lines[0]}"
  local last_line="${hit_lines[$((${#hit_lines[@]}-1))]}"

  local first_start=$(( first_line > 1 ? first_line - 1 : 1 ))
  local first_end=$(( first_line + 1 ))
  local last_start=$(( last_line > 1 ? last_line - 1 : 1 ))
  local last_end=$(( last_line + 1 ))

  local first_ctx
  local last_ctx
  first_ctx="$(sed -n "${first_start},${first_end}p" "$src_file" | tr '\n' '|' | cut -c1-220)"
  last_ctx="$(sed -n "${last_start},${last_end}p" "$src_file" | tr '\n' '|' | cut -c1-220)"

  echo "[apply_ticket] REPLACE_DIAG_CONTEXT mode=${mode} first_line=${first_line} first_ctx='${first_ctx}' last_line=${last_line} last_ctx='${last_ctx}'"
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

      pattern_sha="$(sha256sum "$pattern_file" | awk '{print $1}')"
      pattern_preview="$(head -n1 "$pattern_file" | sed 's/[[:cntrl:]]//g' | cut -c1-120)"

      exact_count="$(count_literal_matches "$rel" "$pattern_file")"
      echo "[apply_ticket] REPLACE_DIAG file=$rel mode=exact pattern_sha=${pattern_sha} match_count=${exact_count} pattern_preview='${pattern_preview}'"
      if [[ "$exact_count" -gt 0 ]]; then
        log_context_window "$rel" "$pattern_file" "exact"
      fi

      apply_literal_replace() {
        local src_file="$1"
        local pat_file="$2"
        local rep_file="$3"
        local out_file="$4"
        awk -v pat="$(cat "$pat_file")" -v rep="$(cat "$rep_file")" '
          {
            buf = buf $0 ORS
          }
          END {
            pos = index(buf, pat)
            if (pos == 0) exit 12
            printf "%s%s%s", substr(buf, 1, pos - 1), rep, substr(buf, pos + length(pat))
          }
        ' "$src_file" > "$out_file"
      }

      if [[ "$exact_count" == "1" ]]; then
        tmp_out="$tmpdir/replaced_${i}.txt"
        set +e
        apply_literal_replace "$rel" "$pattern_file" "$replacement_file" "$tmp_out"
        rc=$?
        set -e
        if [[ $rc -ne 0 ]]; then
          echo "[apply_ticket] REPLACE failed during exact apply in $rel" >&2
          exit 11
        fi
        cat "$tmp_out" > "$rel"
        echo "[apply_ticket] REPLACE $rel mode=exact applied=yes"
        continue
      fi

      if [[ "$exact_count" -gt 1 ]]; then
        echo "[apply_ticket] REPLACE ambiguous in $rel (exact match_count=${exact_count})" >&2
        exit 12
      fi

      norm_src="$tmpdir/norm_src_${i}.txt"
      norm_pat="$tmpdir/norm_pat_${i}.txt"
      norm_rep="$tmpdir/norm_rep_${i}.txt"
      normalize_text_file "$rel" "$norm_src"
      normalize_text_file "$pattern_file" "$norm_pat"
      normalize_text_file "$replacement_file" "$norm_rep"

      norm_count="$(count_literal_matches "$norm_src" "$norm_pat")"
      echo "[apply_ticket] REPLACE_DIAG file=$rel mode=normalized_fallback pattern_sha=${pattern_sha} match_count=${norm_count}"
      if [[ "$norm_count" -gt 0 ]]; then
        log_context_window "$norm_src" "$norm_pat" "normalized_fallback"
      fi

      if [[ "$norm_count" == "1" ]]; then
        tmp_out="$tmpdir/replaced_norm_${i}.txt"
        set +e
        apply_literal_replace "$norm_src" "$norm_pat" "$norm_rep" "$tmp_out"
        rc=$?
        set -e
        if [[ $rc -ne 0 ]]; then
          echo "[apply_ticket] REPLACE failed during normalized fallback in $rel" >&2
          exit 11
        fi
        cat "$tmp_out" > "$rel"
        echo "[apply_ticket] REPLACE $rel mode=normalized_fallback applied=yes"
        continue
      fi

      if [[ "$norm_count" -gt 1 ]]; then
        echo "[apply_ticket] REPLACE ambiguous in $rel (normalized match_count=${norm_count})" >&2
        exit 12
      fi

      echo "[apply_ticket] REPLACE pattern not found in $rel (exact=0 normalized=0 pattern_sha=${pattern_sha})" >&2
      exit 12
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
