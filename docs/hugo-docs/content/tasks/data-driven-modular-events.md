---
title: "Modular Event System for Unity (UI Toolkit + ScriptableObjects)"
weight: 10
system: ["crosscutting"]
types: ["task", "plan", "integration"]
tags: ["EnemyPanelController", "TooltipController", "RuntimeItem", "UI Toolkit"]
stage: ["Planned"] # e.g., "Planned", "InProgress", "Completed", "Blocked"
---

### **Overview**

This document outlines the plan and analysis for transitioning the game's encounter-based event system from its current static data structure to a more flexible, data-driven, and modular system utilizing ScriptableObjects for event actions. The goal is to enable designers to create complex event outcomes without requiring code changes, improving extensibility and content iteration speed.

---

### **Analysis**

The current event system relies on `EncounterSO` to define event encounters, which contain a list of `EventChoice` objects. Each `EventChoice` currently holds direct, predefined fields for specific outcomes such as `goldCost`, `lifeCost`, `itemRewardId`, `shipRewardId`, and `nextEncounterId`.

Our trace revealed the following critical points:

1.  **`EncounterSO` Definition:** Located at `Assets/Scripts/Data/EncounterSO.cs`, it correctly references a `List<EventChoice> eventChoices`.
2.  **`EventChoice` Definition:** Located at `Assets/Scripts/Data/DataTypes.cs`, it explicitly defines the `choiceText`, `goldCost`, `lifeCost`, `itemRewardId`, `shipRewardId`, `nextEncounterId`, and `outcomeText` fields.
3.  **Runtime Handling (`RunManager.cs`):** The `RunManager`'s `HandlePlayerNodeChanged` method, when encountering an `EncounterType.Event`, solely loads the "Event" scene (`SceneManager.LoadScene("Event")`). It does not process any `EventChoice` data or execute outcomes at this stage.
4.  **Event Scene Controller (`EventController.cs`):** This script, located at `Assets/Scripts/UI/EventController.cs`, is responsible for displaying the event UI.
    *   It correctly retrieves the `EncounterSO` and populates the UI with `eventTitle` and `eventDescription`.
    *   It creates UI buttons for each `EventChoice` using `choice.choiceText`.
    *   **Crucially, the `OnChoiceSelected(EventChoice choice)` method contains a `TODO` comment:** `// TODO: Implement the consequences of the choice based on the properties of EventChoice (e.g., goldCost, itemRewardId)`.
    *   Currently, this method only logs the selected choice and its `outcomeText`, then immediately returns to the map.

**Conclusion:** The existing `EventChoice` fields (`goldCost`, `itemRewardId`, etc.) are defined in data and exposed in the editor, but their **runtime execution logic is currently missing**. This means the transition to the modular event system will involve implementing this execution logic from scratch, adhering to the new `EventChoiceAction` pattern, rather than refactoring existing runtime execution code. This simplifies the migration in terms of existing logic, but requires careful implementation of the new action system.

---

### **Actionable Steps**

The following steps outline the process to implement the modular event system:

1.  **Define `PlayerContext` (New):**
    *   Create a new class or interface, e.g., `Assets/Scripts/Core/PlayerContext.cs`, to encapsulate necessary game state and services that `EventChoiceAction`s will operate on (e.g., `GameSession.Instance`, `EconomyService.Instance`, `Inventory.Instance`, `MapManager.Instance`). This will be passed to the `Execute` method of actions.

2.  **Create Abstract `EventChoiceAction` Base Class (New):**
    *   Create `Assets/Scripts/Data/EventChoiceAction.cs`.
    *   Define it as an `abstract ScriptableObject` with an abstract method: `public abstract void Execute(PlayerContext context);`.

3.  **Implement Concrete `EventChoiceAction` Subclasses (New):**
    *   For each existing `EventChoice` outcome field, create a corresponding `ScriptableObject` subclass inheriting from `EventChoiceAction`. These will encapsulate the specific logic.
    *   **`Assets/Scripts/Data/Actions/GainResourceAction.cs`**: For `goldCost` and `lifeCost` (if lives are treated as a resource). Will interact with `EconomyService`.
    *   **`Assets/Scripts/Data/Actions/ModifyStatAction.cs`**: For `lifeCost` (if lives are treated as a stat).
    *   **`Assets/Scripts/Data/Actions/GiveItemAction.cs`**: For `itemRewardId`. Will interact with `Inventory`.
    *   **`Assets/Scripts/Data/Actions/GiveShipAction.cs`**: For `shipRewardId`. Will interact with `GameSession` or `PlayerShip` creation.
    *   **`Assets/Scripts/Data/Actions/LoadEncounterAction.cs`**: For `nextEncounterId`. Will interact with `RunManager` to load a new scene/encounter.
    *   (Optional) **`Assets/Scripts/Data/Actions/DisplayOutcomeTextAction.cs`**: If `outcomeText` is to be an action rather than a direct UI field.

4.  **Refactor `EventChoice` Data Structure (Modify):**
    *   Open `Assets/Scripts/Data/DataTypes.cs`.
    *   **Remove** the existing fields: `goldCost`, `lifeCost`, `itemRewardId`, `shipRewardId`, `nextEncounterId`.
    *   **Add** a new field: `public List<EventChoiceAction> actions;`.
    *   Retain `public string choiceText;` and `public string outcomeText;` (if `outcomeText` remains a display-only field).

5.  **Update Event UI Controller (`EventController.cs`) (Modify):**
    *   Open `Assets/Scripts/UI/EventController.cs`.
    *   In the `OnChoiceSelected(EventChoice choice)` method:
        *   Remove the `TODO` comment and the placeholder `Debug.Log`.
        *   Instantiate and pass the `PlayerContext` to the actions.
        *   Iterate through `choice.actions`: `foreach (var action in choice.actions) { action.Execute(playerContext); }`.
        *   Ensure `ReturnToMap()` is called after all actions are executed.

6.  **Refactor `EncounterEditorWindow.cs` (Modify):**
    *   Open `Assets/Editor/EncounterEditorWindow.cs`.
    *   The `EventChoiceRow` class will need a significant overhaul. Instead of binding to individual fields, it must be updated to display and manage a `ListView` or similar UI for the `List<EventChoiceAction> actions`. This will likely involve using `ObjectField`s to allow designers to drag-and-drop `EventChoiceAction` ScriptableObject assets. Custom property drawers might be necessary for a more user-friendly experience.

7.  **Implement Asset Migration Script (New - Editor Tool):**
    *   Create a new editor script (e.g., `Assets/Editor/EventMigrationTool.cs`).
    *   This script will iterate through all existing `EncounterSO` assets.
    *   For each `EncounterSO`, it will iterate through its `eventChoices`.
    *   For each `EventChoice`, it will read the values from the old fields (`goldCost`, `itemRewardId`, etc.).
    *   It will then programmatically create new instances of the corresponding `EventChoiceAction` ScriptableObjects (e.g., `GainResourceAction` asset, `GiveItemAction` asset), set their values, and add them to the new `actions` list on the `EventChoice`.
    *   This script must handle saving the modified `EncounterSO` assets and the newly created `EventChoiceAction` assets.

8.  **Thorough Testing:**
    *   After implementing the changes and running the migration script, rigorously test all existing event encounters to ensure they function as expected.
    *   Test the creation of new event encounters using the updated editor window and new `EventChoiceAction` assets.

---

### **Refinements and Edge Cases**

This section outlines key areas for further refinement and potential edge cases to consider for a more robust and resilient modular event system.

1.  **`PlayerContext` Design and Usage:**
    *   **Refinement:** The `PlayerContext` passed to `EventChoiceAction.Execute()` should be designed to provide only the necessary interfaces or services (e.g., `IEconomyService`, `IInventory`) rather than direct access to large, monolithic systems like `GameSession`. This adheres to the Principle of Least Knowledge, improving encapsulation, testability, and reducing coupling.
    *   **Edge Case:** Actions must gracefully handle scenarios where a required service within the `PlayerContext` might be `null` (e.g., due to improper initialization or unexpected game state). This should involve logging warnings or errors without causing crashes.

2.  **Action Execution Order and Atomicity:**
    *   **Refinement:** While `EventChoiceAction`s are executed sequentially based on their order in the `List<EventChoiceAction>`, it's crucial to consider if the order of execution matters for specific combinations of actions (e.g., "gain gold" then "spend gold").
    *   **Edge Case:** If an action fails mid-sequence (e.g., a "spend gold" action fails due to insufficient funds), how should the system react? Current actions would likely proceed regardless. For more complex or critical chains, consider implementing a mechanism for atomicity (all actions succeed or all fail) or transactional behavior, though this might be an advanced refinement.

3.  **Robust Error Handling and Player Feedback:**
    *   **Refinement:** Each `EventChoiceAction.Execute()` method should include robust error handling and logging. If an action cannot be performed (e.g., `GiveItemAction` attempts to give an item with an invalid ID, or `ModifyStatAction` tries to apply a negative health value to a system that doesn't support it), it should log a clear warning or error without crashing the game.
    *   **Refinement:** Beyond the static `outcomeText`, consider how individual actions can provide specific, dynamic feedback to the player (e.g., "You gained 50 gold!", "Your ship was repaired!"). This could involve a dedicated `UIMessageService` that actions can interact with to display transient messages.

4.  **Conditions System Integration:**
    *   **Refinement:** A robust condition system is a significant enhancement. This would involve defining `ConditionSO`s or an `ICondition` interface that `EventChoice` could reference. The `EventController` would then evaluate these conditions before enabling/disabling choice buttons in the UI.
    *   **Edge Case:** How are complex conditions handled (e.g., "player has item A AND at least 10 gold")? This would necessitate support for composite conditions (AND, OR, NOT logic).

5.  **Localization Strategy:**
    *   **Refinement:** For a production-ready game, all user-facing text fields (`eventTitle`, `eventDescription`, `choiceText`, and any text generated dynamically by `EventChoiceAction`s) should be integrated with a comprehensive localization system (e.g., using `LocalizedString` objects from a Unity localization package).

6.  **Editor Tool Usability (`EncounterEditorWindow.cs`):**
    *   **Refinement:** The refactoring of `EncounterEditorWindow.cs` is paramount for designer workflow. To improve usability, consider implementing custom property drawers for each `EventChoiceAction` subclass. This would allow designers to directly edit the specific parameters of each action (e.g., `amount` for `GainResourceAction`, `itemId` for `GiveItemAction`) within the `EventChoice` list view, rather than requiring them to select the action asset and edit it in the Inspector. This significantly streamlines the content creation process.

7.  **Asset Management and Null References:**
    *   **Refinement:** Establish clear folder structures and naming conventions for `EventChoiceAction` ScriptableObject assets (e.g., `Assets/Resources/GameData/EventActions/`).
    *   **Edge Case:** Implement robust checks to gracefully handle `null` references within the `actions` list (e.g., if an `EventChoiceAction` asset is accidentally deleted or unassigned). The system should log a warning and skip the `null` action rather than crashing.
