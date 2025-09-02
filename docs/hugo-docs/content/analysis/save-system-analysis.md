---
title: "Save System Analysis"
weight: 10
system: ["core", "data"]
types: ["analysis", "bug-fix", "overview","system-overview"]
status: "archived"
discipline: ["engineering"]
stage: ["production"]
---

# Save System Analysis: Inventory and Equipment

This document details the serialization, saving, and loading mechanisms for player inventory and equipped items within the Pirate Autobattler project, based on an analysis of the following files:

*   `Assets/Scripts/Core/GameSession.cs`
*   `Assets/Scripts/Saving/RunState.cs`
*   `Assets/Scripts/Saving/SaveManager.cs`
*   `Assets/Scripts/Combat/ItemInstance.cs`
*   `Assets/Scripts/Saving/SerializableItemInstance.cs`
*   `Assets/Scripts/Core/Inventory.cs`
*   `Assets/Scripts/Core/ShipState.cs`
*   `Assets/Scripts/Saving/SerializableShipState.cs`

## Problem/Topic

Detailed analysis of the serialization, saving, and loading mechanisms for player inventory and equipped items, and an investigation into the "Missing Items" bug.

## Analysis

### 1. Data Structures for Serialization

The core of the saving system relies on `[Serializable]` plain C# objects that mirror the runtime data structures but are designed for easy conversion to/from JSON using Unity's `JsonUtility`.

*   **`RunState.cs`**: This class acts as the top-level container for all savable game state.
    *   `public SerializableShipState playerShipState;`: Stores the serialized state of the player's ship, including its equipped items.
    *   `public List<SerializableItemInstance> inventoryItems;`: Stores a list of serialized items currently in the player's inventory.

*   **`SerializableItemInstance.cs`**: A simple serializable representation of an `ItemInstance`.
    *   `public string itemId;`: The ID of the item (used to retrieve its `ItemSO` definition).
    *   `public float cooldownRemaining;`: The item's current cooldown.
    *   `public float stunDuration;`: (Note: `ItemInstance`'s `ToSerializable` passes `0` for this, as stun is now handled as an effect on the ship).

*   **`SerializableShipState.cs`**: A serializable representation of a `ShipState`.
    *   `public string shipId;`: The ID of the ship.
    *   `public List<SerializableItemInstance> equippedItems;`: A list of serialized items currently equipped on the ship.
    *   Other fields include `currentHealth`, `currentShield`, `stunDuration`, `activeEffects`, and `activeStatModifiers`.

### 2. Saving Process (Serialization and Persistence)

The saving process is primarily orchestrated by `GameSession.EndBattle()` and handled by `SaveManager.SaveRun()`.

1.  **`GameSession.EndBattle()`**:
    *   **Player Ship State**: `PlayerShip.ToSerializable()` is called on the active `ShipState` object (`GameSession.PlayerShip`). This method iterates through the `ShipState`'s `Equipped` array, converts each `ItemInstance` to a `SerializableItemInstance` by calling `item.ToSerializable()`, and collects them into a `List<SerializableItemInstance>`. This list, along with other ship data, forms a `SerializableShipState` object, which is then assigned to `CurrentRunState.playerShipState`.
    *   **Inventory Items**: A new `List<SerializableItemInstance>` is created. The code then iterates through `Inventory.Items` (the `ItemInstance` array within `GameSession.Inventory`). For each non-null `ItemInstance`, `item.ToSerializable()` is called to convert it into a `SerializableItemInstance`, which is then added to the list. This list is assigned to `CurrentRunState.inventoryItems`.
    *   **Persistence**: The fully populated `GameSession.CurrentRunState` object (which now contains `playerShipState` and `inventoryItems`) is passed to `SaveManager.SaveRun()`.

2.  **`SaveManager.SaveRun(RunState state)`**:
    *   This static method takes the `RunState` object.
    *   It converts the `RunState` object into a JSON string using `JsonUtility.ToJson(state, true)`. The `true` argument enables pretty printing for readability.
    *   The JSON string is then written to a file named `run.json` located in `Application.persistentDataPath`.

### 3. Loading Process (Deserialization and Reconstruction)

The loading process is initiated by `SaveManager.LoadRun()` and then processed by `GameSession.LoadRun()`.

1.  **`SaveManager.LoadRun()`**:
    *   This static method checks if the `run.json` save file exists in `Application.persistentDataPath`.
    *   If the file exists, it reads the entire JSON content from the file.
    *   It then uses `JsonUtility.FromJson<RunState>(json)` to deserialize the JSON string back into a `RunState` object.
    *   This `RunState` object is returned. If the file doesn't exist, it returns `null`.

2.  **`GameSession.LoadRun(RunState loadedState, RunConfigSO config)`**:
    *   The `loadedState` (the `RunState` object returned by `SaveManager.LoadRun()`) is assigned to `GameSession.CurrentRunState`.
    *   **Inventory Items**:
        *   A *new* `Inventory` object is instantiated: `Inventory = new Inventory(config.inventorySize);`.
        *   The code then iterates through `loadedState.inventoryItems` (the `List<SerializableItemInstance>` that was saved).
        *   For each `SerializableItemInstance` (`itemData`), a new `ItemInstance` is created using the constructor `new ItemInstance(itemData)`. This constructor uses `GameDataRegistry.GetItem(itemData.itemId)` to retrieve the correct `ItemSO` definition and sets the `CooldownRemaining`.
        *   The newly created `ItemInstance` is then added to the `Inventory` using `Inventory.AddItem()`.
    *   **Player Ship State**:
        *   A *new* `ShipState` object is instantiated: `PlayerShip = new ShipState(loadedState.playerShipState);`.
        *   The `ShipState` constructor that takes a `SerializableShipState` (`data`) is responsible for reconstructing the ship's state. It iterates through `data.equippedItems` (the `List<SerializableItemInstance>` saved within the ship's state).
        *   For each `SerializableItemInstance`, a new `ItemInstance` is created using `new ItemInstance(data.equippedItems[i])` (the same constructor used for inventory items).
        *   These reconstructed `ItemInstance` objects are then directly assigned to the `ShipState`'s `Equipped` array.

### 4. Analysis of the "Missing Items" Bug

Based on the code analysis, the serialization and deserialization logic for both inventory and equipped items appears to be correctly implemented:

*   **Separate Storage**: Inventory items are stored in `RunState.inventoryItems`, and equipped items are stored within `RunState.playerShipState.equippedItems`. This separation is correctly maintained during both saving and loading.
*   **Correct Reconstruction**: Both `ItemInstance` (from `SerializableItemInstance`) and `ShipState` (from `SerializableShipState`) constructors correctly retrieve the base `ScriptableObject` definitions (`ItemSO`, `ShipSO`) from `GameDataRegistry` and reconstruct the runtime `ItemInstance` objects, including their cooldowns.

Given this, if items are missing after loading a save, the issue is likely *not* in the core serialization/deserialization logic itself, but rather in one of the following areas:

1.  **`GameDataRegistry` Initialization/Availability**: If `GameDataRegistry.GetItem()` or `GameDataRegistry.GetShip()` returns `null` during the loading process (e.g., if the `GameDataRegistry` hasn't fully loaded all `ScriptableObject` assets before `GameSession.LoadRun` is called, or if an item ID in the save file doesn't match any existing `ItemSO`), then the `ItemInstance` or `ShipState` will not be correctly reconstructed, leading to missing items.
2.  **Save File Integrity**: The `run.json` file itself might be corrupted, empty, or not containing the expected item data. This could happen due to an incomplete save operation or external modification.
3.  **UI Synchronization**: The items might be correctly loaded into `GameSession.Inventory` and `GameSession.PlayerShip.Equipped` in memory, but the UI elements responsible for displaying these items might not be refreshing or updating correctly after the `GameSession.LoadRun` method completes.
4.  **Post-Load Modifications**: There might be other game logic that runs *after* `GameSession.LoadRun` but *before* the player interacts with the game, which inadvertently clears or modifies the inventory or equipped slots.
5.  **`Inventory.AddItem` Edge Cases**: While `Inventory.AddItem` is used for loading, if there's an edge case (e.g., related to item merging or inventory being full) where `AddItem` returns `false` during loading, the item would effectively be lost. However, the current loading loop for inventory items does not check the return value of `AddItem`, implying that it expects all items to be added successfully.

## Conclusion/Recommendations

To diagnose the exact cause of the "Missing Items" bug, further debugging would involve:
*   Inspecting the `run.json` file directly to verify its contents.
*   Adding `Debug.Log` statements within the `ItemInstance` and `ShipState` constructors (the ones taking serializable data) to confirm that items are being reconstructed.
*   Adding `Debug.Log` statements in `GameSession.LoadRun` to trace the population of `GameSession.Inventory.Items` and `GameSession.PlayerShip.Equipped`.
*   Verifying the timing of `GameDataRegistry` initialization relative to `GameSession.LoadRun`.