# Phase 2 — Visual/UI Quality

## Scope
Polish responsiveness and readability without external paid assets.

## Systems
- UI layout pass (portrait-safe)
- Tween utility for feedback
- Button disabled/cooldown visual states
- Pet visual hooks by state
- Toast panel and contextual status lines

## Acceptance
- UI hierarchy and spacing are stable
- Feedback animations are subtle and consistent
- Pet state differences are obvious at a glance
- CI compile gate passes after each block

## Test notes
- Validate button spam behavior and cooldown text
- Validate state-color/icon changes
- Validate readability at 1080x1920 and smaller

## Implemented status
- [ ] Layout pass
- [ ] Tween utility
- [ ] State visuals
- [ ] Toast polish
