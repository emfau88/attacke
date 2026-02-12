# Deterministic Ticket Run Engine

This repo includes a deterministic Bash run engine for autonomous ticket packs.

## Layout
- `tools/run/run.sh` — orchestrates preflight, sequential ticket loop, gates, rollback, commit/push, reporting.
- `tools/run/gates.sh` — gate checks (forbidden tracked folders + optional Unity compile check).
- `tools/run/rollback.sh` — resets repo to a per-ticket checkpoint.
- `tools/run/report.sh` — builds machine-readable JSON + markdown run reports.
- `tools/run/apply_ticket.sh` — deterministic ticket executor (ACTIONS + optional PATCH).
- `tools/run/state.json` — resume state (`next_ticket`, `last_checkpoint`, `last_run_report`).

## Ticket Format (binding)
Each `tickets/Txxx.md` may contain one or both sections:

### `## ACTIONS`
Allowed actions only:
- `MKDIR <relative_path>`
- `WRITE <relative_path> <<<EOF` ... `EOF`
- `REPLACE <relative_path> PATTERN`
  - `<<<EOF` ... `EOF`
  - `REPLACEMENT <<<EOF` ... `EOF`

Rules:
- Paths must be relative and inside repo.
- `WRITE` overwrites complete file.
- `REPLACE` is literal, first occurrence only.

### `## PATCH`
Plain unidiff content applied via:
- `git apply --whitespace=fix`

## Exit codes (`apply_ticket.sh`)
- `0` success
- `11` invalid action / malformed ticket / invalid path
- `12` `REPLACE` pattern not found
- `13` patch apply failed

## Resume behavior
`run.sh` reads `state.json.next_ticket` and starts from that ticket.

## Failure handling
If apply/gates fail, runner:
1. rolls back to checkpoint (`git reset --hard` + `git clean -fd -e reports/`),
2. logs failure row to report TSV,
3. continues with next ticket.

## Reports
Outputs:
- `reports/run_<timestamp>.json`
- `reports/run_<timestamp>.md`
