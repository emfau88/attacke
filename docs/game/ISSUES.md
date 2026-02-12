# Issues & Blocker Log

## Template
- Date/Time (UTC):
- Phase / Ticket block:
- Problem:
- Impact:
- Root cause:
- Fix attempted:
- Result:
- Next action:

## Block Reports

### Block Report — bootstrap
- Status: initialized issues tracker for phase execution logs.

### Block Report — T201-T210
- Success: 10
- Failed: 0
- Report: reports/run_20260212T202507Z.md
- Readiness: ready for next block


### Block Report — Self-Fix Pass T241-T260
- Why tickets failed:
  - T241,T242,T243,T246-T260 failed with `assets_change_required / no_assets_change_detected`.
  - Root cause: runner assets-check used `git diff --name-only` only, which misses untracked `Assets/` files created by ACTIONS.
  - No compile/gate failure was indicated in failed rows.
- Fix strategy:
  - Patch runner assets-check to include untracked files under `Assets/`.
  - Rewrite failed tickets into measurable gameplay micro-steps affecting `Assets/Scripts/Phase1Config.cs` and `Assets/Scripts/PetController.cs`.
  - Re-run in two blocks (T241-T250, T251-T260).
