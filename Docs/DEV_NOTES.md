# DEV_NOTES

- Scene: `Assets/Scenes/Main.unity`
- Main runtime driver: `Assets/Scripts/PetController.cs`
- Save key: `bulldog_save_v1` (JSON envelope in PlayerPrefs)
- Editor reset shortcut: `R` (editor-only)

## Gameplay values
- Feed: Hunger -15, Mood +2
- Play: Mood +10, Energy -5, Hunger +5
- Sleep: Energy +15, Mood +2, Hunger +5
- Drift tick: every 60s => Hunger +2, Mood -1, Energy -1
