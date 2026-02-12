# Phase1 Self-Fix Plan (T241-T260)

1. Patch runner asset-change detector to count untracked `Assets/` files.
2. Keep CI gate required before every commit/push.
3. Rework T241 to enforce Main scene build target + asset checkpoint.
4. Rework T242-T243 to adjust drift/threshold config in `Phase1Config`.
5. Rework T246 to add audio feedback call path hook in `PetController`.
6. Rework T247-T259 as gameplay tuning micro-steps in `Phase1Config`.
7. Rework T260 to visible UI quest label tweak in `PetController`.
8. Run block T241-T250.
9. Review report + failures.
10. Run block T251-T260.
