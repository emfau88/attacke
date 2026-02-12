# VISUAL_TARGET_BULLDOG.md

## Purpose
Lock the final visual direction to match user-provided mockup style for Bulldog Tamagotchi.

## Source of Truth
- User reference image (Telegram message id 578).
- This doc is the design contract for implementation.

## Style Pillars (must-have)
1. **Glossy cartoon UI** (mobile game style), not flat/minimal.
2. **High-contrast rounded bars** with icon-left + percentage-right.
3. **Quest card** centered below bars with progress and timer.
4. **Hero pet centered** (Bulldog), thick outline + soft shading.
5. **Large circular action buttons** at bottom (Feed / Play / Sleep).
6. **Colorful outdoor background** (sky + grass depth layers).
7. **Juicy feedback** (button press scale, save badge, floating labels).

## Composition Contract (1080x1920)
- Top section (y 60..470): 3 status bars.
- Mid-top (y 490..690): quest card panel.
- Mid (y 720..1380): pet hero area.
- Bottom (y 1450..1880): three large circular action buttons.
- Safe margin: 48 px min on all sides.

## UI Specs
### Status Bars
- Height: ~110
- Corner radius: ~50
- Border: bright stroke 4-6 px
- Fill gradients:
  - Hunger: amber/orange
  - Mood: cyan/blue
  - Energy: green
- Typography: bold, high readability

### Quest Card
- Dark glossy panel with light border
- Primary line: `Keep Mood > X for Ys`
- Secondary line: remaining seconds + progress fraction

### Buttons
- Circle button diameter: ~280
- Gloss/highlight ring + icon center
- Label pill below each button
- Action mapping:
  - Feed => hunger recovery
  - Play => mood increase
  - Sleep => energy recovery

## Pet Rendering Target
- Bulldog silhouette readable at small sizes
- State variants:
  - Happy, Idle, Hungry, Tired, Sick
- Visual response:
  - expression + color tint + micro FX

## Motion/Feedback
- Button press: scale 1.0 -> 1.08 -> 1.0 (120-180ms)
- Bar updates: smooth lerp (non-jitter)
- Save badge appears 0.8-1.2s after stat changes
- Optional floating text (+Mood, +Energy, etc.)

## Technical Constraints
- Deterministic implementation, CI-gated.
- No weakening of existing correctness gates.
- Keep runtime artifacts out of commits.

## Definition of Done (visual)
- A screenshot from Main scene shows same visual hierarchy as reference.
- Top bars, quest card, centered bulldog, bottom circular buttons all present.
- Interactions visibly update bars + pet state.
- User acceptance: “close to reference direction.”
