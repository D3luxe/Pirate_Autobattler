---
title: "Implement Unhandled Encounters Task"
date: 2025-09-06
system: ["core", "ui", "data"]
types: ["task", "plan", "feature"]
tags: ["RunManager", "EncounterSO", "PortController", "EventController", "RewardService", "GameSession", "UI Toolkit"]
stage: "Planned"
categories: ["task"]
related_system: "core"
---

### Objective

To implement the `Treasure`, `Port`, and `Event` encounters, making them functional within the main game loop. The implementation must be compatible with a future debug command to launch any encounter directly by its ID, bypassing map navigation.

### Assumptions

- The scenes `Port.unity` and `Event.unity` are included in the Build Settings and can be loaded by name.
- A new `RewardService` will be created to manage reward logic. The legacy `RewardUIController` will be replaced by a new UI Toolkit-based controller, as detailed in the `refactor-reward-system.md` task.
- The `GameSession` and `RunManager` singletons are available and initialized before this logic runs.
- `EncounterSO` assets in the `GameDataRegistry` contain the necessary data (e.g., `portHealPercent`, `eventChoices`).

### Analysis

- A trace of the codebase confirms that controllers for `Port` and `Event` encounters do not exist and must be created from scratch.
- The `Treasure` encounter type has no corresponding `.unity` scene file. The existence of a `RewardUIController` suggests this should be a non-scene-changing event that displays rewards directly on the main "Run" screen.
- The `Event` encounter type requires dynamic UI generation to handle a variable number of choices. A UI templating system, where the `EncounterSO` specifies a UI prefab to use, is the most flexible approach.
- To support future debug-launching, a mechanism is needed to pass an `EncounterSO` into a target scene, bypassing the `MapManager`. A static property on `GameSession` is the most direct solution.

### Planned steps

1.  **Create Debug-Loading Mechanism in `GameSession`**
    *   **File:** `Assets/Scripts/Core/GameSession.cs`
    *   **Action:** Add a new `public static EncounterSO DebugEncounterToLoad { get; set; }` property. This will act as a temporary channel for passing a specific encounter into a scene, bypassing the `MapManager`.

2.  **Implement "Treasure" Encounter in `RunManager`**
    *   **File:** `Assets/Scripts/Core/RunManager.cs`
    *   **Action:** In the `HandlePlayerNodeChanged()` method's `switch` statement, add a `case` for `EncounterType.Treasure`.
    *   **Logic:** This case will not load a new scene. It will call a new `RewardService` to generate and present the treasure reward. The `RewardService` will be responsible for interfacing with the UI, abstracting that logic away from the `RunManager`.

3.  **Implement "Port" Encounter**
    *   **Action:** Create a new script `Assets/Scripts/UI/PortController.cs`.
    *   **Logic:** The `PortController` will have an initialization method that first checks if `GameSession.DebugEncounterToLoad` is set. If so, it uses that encounter data; otherwise, it falls back to the standard method of getting data from the `MapManager`. It will then apply the `portHealPercent` to the player's ship and provide a UI button to return to the "Run" scene.
    *   **Action:** In `RunManager.cs`, add a `case` to the `switch` to `SceneManager.LoadScene("Port");`.

4.  **Implement "Event" Encounter with UI Templating**
    *   **Action:** Modify `Assets/Scripts/Data/EncounterSO.cs` to add a `public GameObject eventUIPrefab;` field.
    *   **Action:** Create a new script `Assets/Scripts/UI/EventController.cs`.
    *   **Logic:** The `EventController` will use the same initialization pattern as `PortController`. It will then instantiate the `eventUIPrefab` from the loaded `EncounterSO` and dynamically populate it with the event's title, description, and choice buttons.
    *   **Action:** In `RunManager.cs`, add a `case` to the `switch` to `SceneManager.LoadScene("Event");`.

5.  **Add Final Cleanup Logic**
    *   **File:** `Assets/Scripts/Core/RunManager.cs`
    *   **Method:** `OnRunSceneLoaded()`
    *   **Action:** Add a check to ensure `GameSession.DebugEncounterToLoad` is always set back to `null` when the "Run" scene loads. This prevents the debug state from persisting incorrectly.