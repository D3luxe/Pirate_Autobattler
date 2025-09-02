---
title: "Data Systems Overview"
linkTitle: "Data Overview"
weight: 10
system: ["data"]
types: ["system-overview"]
status: "approved"
---

## 1. Static Game Data (ScriptableObjects & GameDataRegistry)

### Purpose
To define immutable, static game content such as items, ships, enemies, and game configuration. These are created and managed within the Unity Editor.

### Storage
`ScriptableObject` assets are stored under `Assets/Resources/GameData/` in various subdirectories (e.g., `Items`, `Ships`, `Encounters`, `Abilities`, `Actions`, `Effects`).

### Loading Mechanism (`GameDataRegistry.cs`)
*   **`GameDataRegistry` (`PirateRoguelike.Core.GameDataRegistry`):**
    *   File Path: Assets/Scripts/Core/GameDataRegistry.cs
    *   A static class responsible for loading all `ScriptableObject` data into memory at application startup.
*   The `Initialize()` method, marked with `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`, ensures that all data is loaded *before* any scene loads.
*   It uses `Resources.LoadAll<T>("GameData/...")` to load all assets of a specific type from the designated `Resources` folder paths.
*   Loaded assets are stored in `Dictionary<string, T>` for quick lookup by their `id` (e.g., `_items`, `_ships`, `_encounters`).
*   `RunConfigSO` is loaded directly using `Resources.Load<RunConfigSO>("GameData/RunConfiguration").

### Access
Provides static `Get` methods (e.g., `GetItem(string id)`, `GetShip(string id)`, `GetEncounter(string id)`, `GetRunConfig()`) to retrieve loaded data.

### Dynamic Rarity Calculation
`GameDataRegistry` also contains logic (`GetRarityProbabilitiesForFloor`) to calculate item rarity probabilities based on `RunConfigSO`'s `rarityMilestones`, interpolating between milestones based on the current floor index.

## 2. Runtime Data (Item Instances & Runtime Objects)

### Problem Addressed
`ScriptableObject`s are static. To handle dynamic changes to item properties (e.g., temporary buffs, cooldowns, stun durations) and to display real-time values in UI, a runtime layer is necessary.

### Core Components (detailed in [Runtime Data Systems]({{< myrelref "../core/runtime-data-systems.md" >}})):
*   **`ItemInstance` (`PirateRoguelike.Data.ItemInstance`):**
    *   File Path: Assets/Scripts/Combat/ItemInstance.cs
    *   The primary runtime object representing a unique instance of an item. It holds a reference to its `ItemSO` blueprint and mutable fields like `CooldownRemaining` and `StunDuration`. Crucially, it contains a `RuntimeItem`.
*   **`RuntimeItem` (`PirateRoguelike.Runtime.RuntimeItem`):**
    *   File Path: Assets/Scripts/Runtime/RuntimeItem.cs
    *   Represents the dynamic abilities and actions of an `ItemInstance`. It's created from an `ItemSO` and manages a collection of `RuntimeAbility` instances.
*   **`RuntimeAbility` (`PirateRoguelike.Runtime.RuntimeAbility`):**
    *   File Path: Assets/Scripts/Runtime/RuntimeAbility.cs
    *   Represents a unique instance of an ability, created from an `AbilitySO`, and manages a collection of `RuntimeAction` instances.
*   **`RuntimeAction` (`PirateRoguelike.Runtime.RuntimeAction`):**
    *   File Path: Assets/Scripts/Runtime/RuntimeAction.cs
    *   An abstract base class for all unique action instances (e.g., `RuntimeDamageAction`, `RuntimeHealAction`). These wrap their `ActionSO` blueprints and hold mutable, runtime-specific values (e.g., `CurrentDamageAmount`).

### How it Works
When an item is needed, an `ItemInstance` is created. This, in turn, recursively creates `RuntimeItem`, `RuntimeAbility`, and `RuntimeAction` objects. Dynamic modifications are applied to the mutable fields within these runtime objects.

### Benefits
Flexibility for dynamic modifications, accurate UI display, clear separation of static vs. runtime data, improved testability.

## 3. Save/Load System

### Purpose
To persist the game state between sessions.

### Architecture (detailed in [Save Load System]({{< myrelref "../core/save-load.md" >}})):
*   **`SaveManager` (`Assets/Scripts/Saving/SaveManager.cs`):** A static class handling the physical file operations (serializing `RunState` to JSON and writing to `run.json`, and deserializing back).
*   **`GameSession` (`Assets/Scripts/Core/GameSession.cs`):** The central static class holding the live, in-memory game state. It orchestrates the saving process by populating a `RunState` object and the loading process by re-initializing the live state from a `RunState` object.
*   **`RunState` (defined in `GameSession.cs`):** A serializable class acting as the root data container for a save file, holding all data to be persisted.

### Serializable Data Objects
To work with Unity's `JsonUtility`, live game objects are converted to simple, serializable plain C# objects:
*   `SerializableShipState` (`Assets/Scripts/Saving/SerializableShipState.ShipState.cs`)
*   `SerializableItemInstance` (`Assets/Scripts/Saving/SerializableItemInstance.cs`)

### Saving Process
`GameSession.UpdateCurrentRunStateForSaving()` populates `GameSession.CurrentRunState` with data converted to serializable forms (e.g., `PlayerShip.ToSerializable()`, `Inventory.ToSerializable()`). This `RunState` is then passed to `SaveManager.SaveRun()`.

### Loading Process
`SaveManager.LoadRun()` deserializes `run.json` into a `RunState` object, which is then passed to `GameSession.LoadRun()` to rehydrate the live game state.

### Known Issue (Critical Gap)
The save system currently **does not persist changes made to mutable properties within `RuntimeAction` instances**. This means dynamic buffs or modifications to item behaviors are lost upon saving and loading. A proposed solution involves extending `SerializableItemInstance` to store these modified properties.

## 4. Summary of Data Flow

1.  **Startup:** `GameDataRegistry` loads all static `ScriptableObject` data from `Resources`.
2.  **Gameplay:** `ItemInstance` and its nested `Runtime` objects are created from `ItemSO` blueprints. Dynamic changes occur on these runtime objects. `ShipState` manages its own internal state and equipped `ItemInstance`s.
3.  **Saving:** Live game state (`GameSession`, `ShipState`, `Inventory`, `ItemInstance`s) is converted into serializable `RunState`, `SerializableShipState`, and `SerializableItemInstance` objects, then saved to JSON.
4.  **Loading:** Saved JSON is deserialized into `RunState`, then used to re-create live game objects (`GameSession`, `ShipState`, `ItemInstance`s) from their `ScriptableObject` definitions, applying any *persisted* runtime modifications.

This comprehensive data system allows for flexible game content definition, dynamic runtime behavior, and persistent game state, though the current save system has a critical limitation regarding runtime action state.