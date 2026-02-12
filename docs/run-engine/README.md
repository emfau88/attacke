# Deterministic Ticket Run Engine

This repo includes a deterministic Bash run engine for autonomous ticket packs.

## Layout
- `tools/run/run.sh` — orchestrates preflight, sequential ticket loop, gates, rollback, commit/push, reporting.
- `tools/run/gates.sh` — gate checks (forbidden tracked folders + optional Unity compile check).
- `tools/run/rollback.sh` — resets repo to a per-ticket checkpoint.
- `tools/run/report.sh` — builds machine-readable JSON + markdown run reports.
- `tools/run/apply_ticket.sh` — ticket apply hook (currently stub: exits 2).
- `tools/run/state.json` — resume state (`next_ticket`, `last_checkpoint`, `last_run_report`).

## Ticket Format
Each `tickets/Txxx.md` should include sections:
- `GOAL`
- `CHANGES`
- `ACCEPTANCE`
- `NOTES`

## Resume behavior
`run.sh` reads `state.json.next_ticket` and starts from that ticket.

## Failure handling
If apply/gates fail, runner:
1. rolls back to checkpoint (`git reset --hard` + `git clean -fd`),
2. logs failure row to report TSV,
3. continues with next ticket.

## Reports
Outputs:
- `reports/run_<timestamp>.json`
- `reports/run_<timestamp>.md`

## Current apply hook
`apply_ticket.sh` intentionally returns `exit 2` (`not implemented`) to validate deterministic failure + rollback flow.
