---
title: "Save Load System"
weight: 10
system: ["core"]
types: ["system-overview"]
status: "approved"
---

## 1. System Architecture

The save/load system is designed around a central RunState object that acts as a snapshot of the entire game session. A single save slot is used, stored in a JSON file. For a broader understanding of how data is managed in the game, refer to the [Data Systems Overview]({{< myrelref "../data/data-systems-overview.md" >}}).

### 1.1. Core Components

*   **`SaveManager` (`PirateRoguelike.Saving.SaveManager`):**
    >   File Path: Assets/Scripts/Saving/SaveManager.cs
    *   A static class responsible for the physical file operations.
    *   Handles serializing the `RunState` object to JSON and writing it to `run.json` in the game's persistent data path.
    *   Handles reading the JSON file and deserializing it back into a `RunState` object.

*   **`GameSession` (`PirateRoguelike.Core.GameSession`):**
    >   File Path: Assets/Scripts/Core/GameSession.cs
    *   The central static class holding the live, in-memory game state (e.g., `PlayerShip`, `Inventory`, `Economy`).
    *   Orchestrates the saving process by populating a `RunState` object with data from the live state before passing it to the `SaveManager`.
    *   Orchestrates the loading process by taking a `RunState` object and using it to re-initialize the live game state (creating new `ShipState`, `Inventory`, etc.).

*   **`RunState` (`PirateRoguelike.Core.RunState`):**
    >   File Path: Assets/Scripts/Core/GameSession.cs (Defined within GameSession.cs)
    *   A serializable class that acts as the root data container for a save file. It holds all the data that needs to be persisted between sessions.

### 1.2. Serializable Data Objects

To facilitate serialization with `JsonUtility`, the live game objects are converted to and from simple, serializable plain C# objects:

*   **`SerializableShipState` (`PirateRoguelike.Saving.SerializableShipState`):**
    >   File Path: Assets/Scripts/Saving/SerializableShipState.cs
    *   Captures the state of a ship (player or enemy), including health, shield, and a list of its equipped items.
*   **`SerializableItemInstance` (`PirateRoguelike.Saving.SerializableItemInstance`):**
    >   File Path: Assets/Scripts/Saving/SerializableItemInstance.cs
    *   Captures the state of a single item instance.

## 2. Data Flow

### 2.1. Saving Process

1.  The process is typically triggered at the end of a battle via `GameSession.EndBattle(). For details on how battles are managed, see the [Combat System Overview]({{< myrelref "../combat/combat-system-overview.md" >}}).`.
2.  `GameSession.UpdateCurrentRunStateForSaving()` is called. This method is the core of the save process.
3.  It populates the `GameSession.CurrentRunState` object with fresh data from the live game state:
    *   The live `PlayerShip` (`ShipState`) is converted to a `SerializableShipState` by calling its `.ToSerializable()` method.
    *   The live `Inventory` is iterated, and each `ItemInstance` is converted to a `SerializableItemInstance` via its `.ToSerializable()` method.
4.  The fully populated `RunState` object is passed to `SaveManager.SaveRun(RunState)`, which serializes it to JSON and writes it to disk.

### 2.2. Loading Process

1.  At game startup, a controller (e.g., `MainMenuController`) checks if a save file exists using `SaveManager.SaveFileExists()`.
2.  If it exists, `SaveManager.LoadRun()` is called to deserialize `run.json` into a `RunState` object.
3.  This `RunState` object is passed to `GameSession.LoadRun()`.
4.  `GameSession.LoadRun()` rehydrates the live game state:
    *   It creates a new `PlayerShip` (`ShipState`) and a new `Inventory`.
    *   **For Inventory:** When re-populating inventory slots, it iterates through the saved `CurrentRunState.inventoryItems`. For each non-null entry, it verifies the item's existence in `GameDataRegistry` and creates a new `ItemInstance` in the correct slot. Empty slots in the saved data (due to omission during saving) will remain `null` in the `Inventory` as it is pre-initialized with empty slots.
    *   **For Equipped Items:** When loading equipped items via `PlayerShip = new ShipState(CurrentRunState.playerShipState)`, the `ShipState` constructor iterates through `SerializableShipState.equippedItems`. It explicitly handles `null` entries by setting the corresponding `Equipped` slot to `null`, ensuring that empty equipped slots are correctly preserved.
    *   If an item's definition does not exist in `GameDataRegistry` (e.g., it was removed in a game update), a warning is logged, and the slot is treated as empty (either by omission for inventory or by explicit `null` assignment for equipped items). For more information on how game data is loaded and managed, refer to the [Data Systems Overview]({{< myrelref "../data/data-systems-overview.md" >}}).

## 3. Serialized Data Hierarchy

The structure of the saved JSON data is as follows:

```
RunState
├── playerLives
├── gold
├── currentColumnIndex
├── randomSeed
├── ... (other run metadata)
|
├── playerShipState (SerializableShipState)
│   ├── shipId
│   ├── currentHealth
│   └── equippedItems (List<SerializableItemInstance>)
│       └── [SerializableItemInstance]
│           ├── itemId
│           ├── rarity
│           ├── cooldownRemaining
│           └── stunDuration
|
└── inventoryItems (List<SerializableItemInstance>)
    └── [SerializableItemInstance]
        ├── itemId
        ├── rarity
        ├── cooldownRemaining
        └── stunDuration
```

## 4. Known Issues & Critical Gaps

**`RuntimeAction` State is Not Saved**

The current save system does **not** persist any changes made to the mutable properties within the `RuntimeItem` system, specifically the `RuntimeAction` instances.

*   **Problem:** The new item system is designed to allow for permanent, in-run modifications to an item's behavior (e.g., increasing its damage or healing amount). These changes are stored in properties like `CurrentDamageAmount` on a `RuntimeDamageAction` object.
*   **Gap:** The `ItemInstance.ToSerializable()` method only saves the item's ID, rarity, and its own direct properties (like cooldown). It does not inspect the state of its child `RuntimeItem` or its `RuntimeAction`s. When the game is reloaded, `ItemInstance` and `RuntimeItem` are recreated from the base `ScriptableObject`s, losing any modifications made during gameplay.

*   **Example Scenario:**
    1.  The player has a "Cannon" item that deals 10 damage.
    2.  The player encounters a shrine event that grants "+5 damage to all your cannons for the rest of this run."
    3.  The code updates the `CurrentDamageAmount` on the Cannon's `RuntimeDamageAction` to `15`.
    4.  The player saves and quits.
    5.  Upon reloading, the `SerializableItemInstance` is used to create a new `ItemInstance`. The `RuntimeDamageAction` is recreated from the base `DamageActionSO`, and its `CurrentDamageAmount` is reset to the default value of `10`. **The +5 buff is lost.**

**This is a critical flaw that prevents the full potential of the dynamic item system from being utilized across game sessions.** Addressing this will likely require extending the `SerializableItemInstance` to store a list of modifications or creating a more robust serialization strategy for the entire [RuntimeItem hierarchy]({{< myrelref "runtime-data-systems.md" >}}).

## 5. Proposed Solution

To address the gap, the `SerializableItemInstance` needs to be extended to store the dynamic values from the `RuntimeAction`s. This can be done in a flexible way using a dictionary.

Regarding item slots, the current system already implicitly saves an item's location by its position (index) in the `equippedItems` or `inventoryItems` list within the `RunState`. This is sufficient and does not need to be changed.

Here is a step-by-step plan to implement the fix:

### Step 1: Extend `SerializableItemInstance`

The `SerializableItemInstance` class should be modified to include a dictionary to hold the modified values. Since Unity's `JsonUtility` does not directly serialize dictionaries, a common workaround is to use two lists for keys and values.

**File:** `Assets/Scripts/Saving/SerializableItemInstance.cs`
```csharp
[Serializable]
public class SerializableItemInstance
{
    public string itemId;
    public Rarity rarity;
    public float cooldownRemaining;
    public float stunDuration;

    // New fields for dynamic properties
    public List<string> modifiedPropertyKeys;
    public List<float> modifiedPropertyValues;

    public SerializableItemInstance(/*...other params...*/)
    {
        // ... existing constructor logic ...
        modifiedPropertyKeys = new List<string>();
        modifiedPropertyValues = new List<float>();
    }
}
```

### Step 2: Update `ItemInstance.ToSerializable()`

This method must be updated to iterate through its `RuntimeItem` hierarchy and populate the new lists. A unique key must be generated for each property.

**File:** `Assets/Scripts/Combat/ItemInstance.cs`
```csharp
public SerializableItemInstance ToSerializable()
{
    var serializable = new SerializableItemInstance(Def.id, Def.rarity, CooldownRemaining, StunDuration);

    // Iterate through abilities and actions to find modified values
    for (int i = 0; i < RuntimeItem.Abilities.Count; i++)
    {
        var runtimeAbility = RuntimeItem.Abilities[i];
        for (int j = 0; j < runtimeAbility.Actions.Count; j++)
        {
            var runtimeAction = runtimeAbility.Actions[j];
            string keyPrefix = $"{i}/{j}"; // Key: "AbilityIndex/ActionIndex"

            // Check for specific action types and their properties
            if (runtimeAction is RuntimeDamageAction damageAction)
            {
                // If current value differs from base SO value, save it
                if (damageAction.CurrentDamageAmount != ((DamageActionSO)damageAction.BaseActionSO).damageAmount)
                {
                    serializable.modifiedPropertyKeys.Add($"{keyPrefix}/CurrentDamageAmount");
                    serializable.modifiedPropertyValues.Add(damageAction.CurrentDamageAmount);
                }
            }
            else if (runtimeAction is RuntimeHealAction healAction)
            {
                 if (healAction.CurrentHealAmount != ((HealActionSO)healAction.BaseActionSO).healAmount)
                {
                    serializable.modifiedPropertyKeys.Add($"{keyPrefix}/CurrentHealAmount");
                    serializable.modifiedPropertyValues.Add(healAction.CurrentHealAmount);
                }
            }
            // Add more cases for other modifiable actions here...
        }
    }

    return serializable;
}
```

### Step 3: Update Loading Logic in `ItemInstance`

After an `ItemInstance` is created during the loading process, it needs to apply the saved modifications from the `SerializableItemInstance`.

**File:** `Assets/Scripts/Combat/ItemInstance.cs`
```csharp
// Modify the constructor or add an ApplyModifications method
public ItemInstance(ItemSO def, SerializableItemInstance savedState = null)
{
    Def = def;
    RuntimeItem = new RuntimeItem(def);
    CooldownRemaining = 0;
    StunDuration = 0;

    // Apply saved state if provided
    if (savedState != null)
    {
        CooldownRemaining = savedState.cooldownRemaining;
        StunDuration = savedState.stunDuration;

        // Apply dynamic modifications
        if (savedState.modifiedPropertyKeys != null)
        {
            for (int k = 0; k < savedState.modifiedPropertyKeys.Count; k++)
            {
                string key = savedState.modifiedPropertyKeys[k];
                float value = savedState.modifiedPropertyValues[k];

                string[] parts = key.Split('/');
                int abilityIndex = int.Parse(parts[0]);
                int actionIndex = int.Parse(parts[1]);
                string propertyName = parts[2];

                var actionToModify = RuntimeItem.Abilities[abilityIndex].Actions[actionIndex];

                // Apply the value based on property name
                if (propertyName == "CurrentDamageAmount" && actionToModify is RuntimeDamageAction damageAction)
                {
                    damageAction.CurrentDamageAmount = (int)value;
                }
                else if (propertyName == "CurrentHealAmount" && actionToModify is RuntimeHealAction healAction)
                {
                    healAction.CurrentHealAmount = (int)value;
                }
                // Add more cases here...
            }
        }
    }
}

// The GameSession.LoadRun method would need to be updated to use this new constructor:
// new ItemInstance(GameDataRegistry.GetItem(itemData.itemId, itemData.rarity), itemData)
```

This approach provides a scalable way to persist any number of dynamic changes to item behaviors, ensuring that the player's progress and item buffs are correctly restored across game sessions.
