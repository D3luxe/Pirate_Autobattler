# Debug Encounter Command Analysis (`startencounter`)

## Initial Goal
The user requested a debug command `startencounter <encounterId>` to launch any encounter by its ID, even if it's not present on the current game map.

## Key Findings from Trace & Verify Protocol

1.  **`BattleManager` Dependency:**
    - The `BattleManager` (controller for the "Battle" scene) initializes itself using `GameSession.CurrentRunState.currentEncounterId`.
    - **Crucially**, it passes this ID to the `MapManager` to get `MapNodeData`. It does **not** fetch a generic `EncounterSO` from the `GameDataRegistry`.
    - This means the `BattleManager` is tightly coupled to the `MapManager` and expects the encounter to be part of the currently generated map. A simple command cannot work without modifying this.

2.  **Encounter Routing in `RunManager`:**
    - The `RunManager` contains a `switch` statement that routes different `EncounterType` enums to different scenes.
    - `Battle`, `Elite`, `Boss` -> "Battle" scene.
    - `Shop` -> "Shop" scene.
    - `Port`, `Event`, `Treasure` are currently unhandled in the `RunManager`'s navigation logic.

3.  **"Event" Encounter Incompleteness:**
    - The trace for "Event" encounters revealed the feature is not implemented at a runtime level. No code path loads the `Event.unity` scene during gameplay.

## Conclusion & Revised Problem Statement

A simple `startencounter` command that just loads the "Battle" scene is insufficient and incorrect because:
- Not all encounters are battles.
- The `BattleManager` requires data from the `MapManager`, which a direct-to-battle command would bypass.

To fulfill the user's request, a more significant modification is needed:
1.  A mechanism must be created to pass a specific `EncounterSO` into a target scene, bypassing the `MapManager`. A temporary static variable in `GameSession` (e.g., `DebugEncounterToLoad`) is a likely candidate.
2.  The controller for **each** relevant scene (`BattleManager`, `ShopController`, etc.) must be modified to check for and use this debug data, providing an alternative initialization path.
3.  The `startencounter` command in `DebugConsoleController` must contain the routing logic (a `switch` on `EncounterType`) to load the correct scene.

This analysis needs to be extended by investigating the controllers for non-battle scenes (`Shop`, `Port`, etc.) to understand their specific initialization dependencies before a final implementation plan can be made.
