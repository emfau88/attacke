# Phase 1 — Playable Core Loop

## Scope
Build a complete, understandable pet-care loop with robust state handling and feedback.

## Systems
- Stats and clamps
- Drift + offline progression from config
- State machine + transitions
- Action effects + gating
- Save/load + migration stub
- Debug overlay for tuning

## Acceptance
- Player can run a full care cycle in Play Mode
- Actions visibly update stats and state
- Offline progression is capped and explainable
- No compile errors; CI compile gate passes

## Test notes
- Verify each action delta and gating path
- Verify state transitions at threshold boundaries
- Verify save/reload and migration fallback
- Verify debug overlay numbers match internal state

## Implemented status
- [ ] Core stat model
- [ ] Config-driven drift/actions
- [ ] State machine visualization
- [ ] Debug overlay
- [ ] Save/version checks
