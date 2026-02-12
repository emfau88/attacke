# BULLDOG_IMPLEMENTATION_PLAN.md

## Goal
Implement a playable Bulldog Tamagotchi loop in the exact visual direction of the approved mockup reference.

## Execution Model
- Small deterministic tickets.
- One visible outcome per ticket.
- CI gate must pass before each commit/push.
- Keep existing game logic and improve presentation/UX incrementally.

---

## Block A — Visual Contract Lock (no risky runtime changes)
1. Create visual contract doc (`VISUAL_TARGET_BULLDOG.md`).
2. Define spacing/radius/color/typography tokens.
3. Add mockup compliance checklist.

### Acceptance
- Docs committed.
- Checklist can be used to accept/reject screenshots.

---

## Block B — UI Layout Parity (top bars, quest, bottom actions)
1. Rebuild HUD hierarchy to match reference composition.
2. Introduce reusable `StatusBarWidget` setup function.
3. Place quest card with explicit timer/progress line.
4. Convert bottom actions to large circular CTA style.

### Acceptance
- Main scene screenshot shows matching hierarchy and proportions.
- Buttons functional with existing Feed/Play/Sleep logic.

---

## Block C — Bulldog Hero Visual Layer
1. Add dedicated pet hero container in center.
2. Add state-specific visual variants (color/expression placeholders first).
3. Add “Saved” badge + floating feedback alignment.

### Acceptance
- Pet visibly reacts to state changes.
- Save badge appears after action and auto-hides.

---

## Block D — Gameplay Feel / Juice
1. Add consistent button press animation.
2. Smooth bar transitions + low-jitter text updates.
3. Tune drift/cooldown values for playable loop.

### Acceptance
- Controls feel responsive.
- Loop remains readable and engaging over 2-3 minutes.

---

## Block E — Hardening + Delivery
1. Run targeted ticket blocks and full phase run.
2. Verify CI compile gate green.
3. Update phase docs and final summary with screenshots.

### Acceptance
- T241-T260 replacement/fix path stable.
- Green run for selected release block.

---

## Prompt/Ticket Authoring Rules (self-driven)
- Each ticket must modify concrete `Assets/` content.
- REPLACE patterns anchored to current canonical source text.
- No speculative/fuzzy matching in tickets.
- If source drift exists, update ticket pattern first, then run.

## Immediate Next Actions
1. Implement Block B in `Assets/Scripts/PetController.cs` with minimal file surface.
2. Run CI.
3. Produce screenshot/checklist delta report against visual contract.
