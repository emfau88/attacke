# Bulldog Tamagotchi — Masterplan

## Vision
Create a cozy but readable virtual-pet vertical slice where the player always understands: (1) what the dog needs, (2) what actions matter, and (3) how short sessions still feel rewarding. The experience should be playful, responsive, and clear even with placeholder art. The loop should avoid cheap-feeling randomness and instead provide predictable systems with light surprise.

## Phase 1 — Playable Core Loop (T201–T260)
- [ ] Centralized balance config (drift/action deltas/caps)
- [ ] Stable pet state model (Hunger/Mood/Energy + derived health/state)
- [ ] Explicit state machine (Happy/Neutral/Tired/Hungry/Sick)
- [ ] Time drift + offline progression via config
- [ ] Action gating by energy/conditions
- [ ] Action feedback (toasts/state text/change indicators)
- [ ] Debug overlay (timers/state/last action/offline calc)
- [ ] Save/load robustness + versioning/migration stub
- [ ] Deterministic test hooks for balancing
- [ ] CI compile gate passes after each block

## Phase 2 — Visual/UI Quality (T261–T320)
- [ ] UI hierarchy cleanup (panels/spacing/readability)
- [ ] Stat bars + labels polish pass
- [ ] Button interaction polish (cooldown visuals/timers)
- [ ] Reusable tween utility for simple motion
- [ ] Pet idle animation hooks (placeholder)
- [ ] Visual differentiation by pet state
- [ ] Background layering (light depth)
- [ ] Status banner + contextual hints
- [ ] Lightweight style consistency pass
- [ ] CI compile gate passes after each block

## Phase 3 — Depth (T321–T380)
- [ ] One mini-game or activity outcome loop
- [ ] Reward output integrated into core stats
- [ ] Inventory model (3–5 items)
- [ ] Feed consumes item stack
- [ ] Daily event/login reward
- [ ] Small random event system (controlled)
- [ ] Reward messaging + inventory feedback
- [ ] Simple progression pacing controls
- [ ] Regression checks vs core loop clarity
- [ ] CI compile gate passes after each block

## Definition of Done
- Phase 1 DoD: Core systems playable, readable, state machine visible, offline progression validated, debug overlay available.
- Phase 2 DoD: UI/feedback feels coherent and responsive, visible state differentiation, basic polish complete.
- Phase 3 DoD: One meaningful extra activity + rewards/inventory + daily event integrated without destabilizing core.

## Do not do yet
- Online backend/cloud sync
- Monetization systems
- Large narrative/cutscenes
- Multiplayer/social features
- Asset-store style mixing mid-phase

## Execution rules
- Run deterministic tickets only (ACTIONS/PATCH)
- Run in blocks of 10 tickets
- Before commit/push always run `bash tools/ci/ci_run.sh`
- Keep reports generated and retained
- Record block outcomes and blockers in `docs/game/ISSUES.md`
