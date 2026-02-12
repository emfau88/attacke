#!/usr/bin/env bash
set -euo pipefail
CHECKPOINT="${1:-}"
if [[ -z "$CHECKPOINT" ]]; then
  echo "rollback: missing checkpoint hash" >&2
  exit 2
fi

git reset --hard "$CHECKPOINT"
git clean -fd
