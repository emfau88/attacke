# Phase 3 — Depth

## Scope
Add one compact interaction layer that increases replay value without clutter.

## Systems
- One mini-game/activity
- Reward mapping to core stats/items
- Simple inventory with 3–5 items
- Daily event/login reward
- Controlled random event

## Acceptance
- Player can perform activity and receive meaningful rewards
- Inventory updates are visible and deterministic
- Daily event triggers predictably
- CI compile gate passes after each block

## Test notes
- Verify reward economy isn’t overpowering core loop
- Verify inventory consume/add edge cases
- Verify daily event timing and reset logic

## Implemented status
- [ ] Activity loop
- [ ] Inventory + items
- [ ] Daily event
- [ ] Event messaging
