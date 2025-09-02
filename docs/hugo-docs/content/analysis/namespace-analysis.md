---
title: "Namespace Analysis"
weight: 10
system: ["core"]
types: ["analysis"]
status: "archived"
stage: ["completed"]
---

### Analysis

Based on the provided `Namespaces.md` file, here's a detailed analysis of namespace consistency, citing specific files as evidence:

**Root Namespace:** The project consistently uses `PirateRoguelike` as the primary root namespace, with `Pirate.MapGen` used for map generation specific logic.

**Inconsistencies Identified:**

1.  **Missing Namespace Declarations:** A significant number of files lack explicit `namespace` declarations, implicitly placing them in the global namespace. This is inconsistent with files in the same logical modules that *do* declare namespaces.
    *   **Evidence:**
        *   `Assets/Scripts/Combat/ActiveCombatEffect.cs` (no explicit namespace) vs. `Assets/Scripts/Combat/CombatContext.cs` (declares `PirateRoguelike.Combat`).
        *   `Assets/Scripts/Core/AbilityManager.cs` (no explicit namespace) vs. `Assets/Scripts/Core/GameSessionWrapper.cs` (declares `PirateRoguelike.Core`).
        *   `Assets/Scripts/Data/EnemySO.cs` (no explicit namespace) vs. `Assets/Scripts/Data/EncounterSO.cs` (declares `PirateRoguelike.Data`).
        *   Many files in `Assets/Scripts/UI/` (e.g., `BattleUIController.cs`, `InventoryUI.cs`) lack explicit namespaces, while others in the same directory (e.g., `Assets/Scripts/UI/Components/ItemElement.cs`) declare `PirateRoguelike.UI.Components`.

2.  **Misplaced File/Namespace Mismatch:** One file is located in a directory that contradicts its declared namespace.
    *   **Evidence:** `Assets/Scripts/Combat/ItemInstance.cs` declares `namespace PirateRoguelike.Data`. This file is physically located in the `Combat` directory but logically belongs to the `Data` namespace.

3.  **Inconsistent Namespace for Shared Interface:** A shared interface is declared within a UI-specific namespace, despite being in a `Shared` directory.
    *   **Evidence:** `Assets/Scripts/Shared/Interfaces/INotifyPropertyChanged.cs` declares `namespace PirateRoguelike.UI`. Given its location in `Shared`, it should logically belong to `PirateRoguelike.Shared`.

4.  **Inconsistent Map-Related Namespaces:** Files within the `Assets/Scripts/Map/` directory have inconsistent namespace declarations or lack them, while related map generation files consistently use `Pirate.MapGen`.
    *   **Evidence:**
        *   `Assets/Scripts/Map/MapNodeData.cs` has no explicit namespace, but `Assets/Scripts/Map/MapGraphData.cs` declares `Pirate.MapGen`.
        *   `Assets/Scripts/Map/MapManager.cs` has no explicit namespace, but all files in `Assets/Scripts/MapGeneration/` consistently declare `Pirate.MapGen`.

### Proposed Consistent Namespace Structure:

To address these inconsistencies, the following namespace structure will be enforced:

*   **`PirateRoguelike.Combat`**: For all files within `Assets/Scripts/Combat/`.
*   **`PirateRoguelike.Core`**: For core game logic files within `Assets/Scripts/Core/` that don't fit into more specific sub-namespaces.
*   **`PirateRoguelike.Services`**: For service classes within `Assets/Scripts/Core/` (e.g., `EconomyService`, `Inventory`, `RewardService`, `ItemManipulationService`).
*   **`PirateRoguelike.Events`**: For event-related classes within `Assets/Scripts/Core/` (e.g., `GameEvents`, `ItemManipulationEvents`).
*   **`PirateRoguelike.Data`**: For `ScriptableObject` definitions and core data structures within `Assets/Scripts/Data/`.
*   **`PirateRoguelike.Data.Abilities`**: For ability-related data.
*   **`PirateRoguelike.Data.Actions`**: For action-related data.
*   **`PirateRoguelike.Data.Effects`**: For effect-related data.
*   **`PirateRoguelike.Encounters`**: For files within `Assets/Scripts/Encounters/`.
*   **`Pirate.MapGen`**: For all map generation and map data related logic within `Assets/Scripts/MapGeneration/` and `Assets/Scripts/Map/`.
*   **`PirateRoguelike.Runtime`**: For runtime instances of game entities within `Assets/Scripts/Runtime/`.
*   **`PirateRoguelike.Saving`**: For saving/loading logic within `Assets/Scripts/Saving/`.
*   **`PirateRoguelike.Shared`**: For shared utilities and interfaces within `Assets/Scripts/Shared/`.
*   **`PirateRoguelike.UI`**: For general UI logic within `Assets/Scripts/UI/`.
*   **`PirateRoguelike.UI.Components`**: For UI component classes.
*   **`PirateRoguelike.UI.Utilities`**: For UI utility classes.
*   **`PirateRoguelike.Utility`**: For general utility classes within `Assets/Scripts/Utility/`.

### Plan to Correct Namespace Inconsistencies:

1.  **Move `ItemInstance.cs`:**
    *   [x] Move `Assets/Scripts/Combat/ItemInstance.cs` to `Assets/Scripts/Data/ItemInstance.cs`. This aligns its physical location with its declared `PirateRoguelike.Data` namespace.

2.  **Update `INotifyPropertyChanged.cs` Namespace:**
    *   [x] Change the namespace declaration in `Assets/Scripts/Shared/Interfaces/INotifyPropertyChanged.cs` from `PirateRoguelike.UI` to `PirateRoguelike.Shared`.

3.  **Add/Update Namespaces for Files in `Assets/Scripts/Combat/`:**
    *   [x] Add `namespace PirateRoguelike.Combat` to:
        *   `ActiveCombatEffect.cs`
        *   `BattleManager.cs`
        *   `CombatController.cs`
        *   `ShipStateView.cs`
        *   `StatModifier.cs`

4.  **Add/Update Namespaces for Files in `Assets/Scripts/Core/`:**
    *   [x] Add `namespace PirateRoguelike.Core` to:
        *   `AbilityManager.cs`
        *   `EventBus.cs`
        *   `GameDataRegistry.cs`
        *   `GameInitializer.cs`
        *   `GameSession.cs`
        *   `ITickService.cs`
        *   `RunManager.cs`
        *   `ShipState.cs`
        *   `TickService.cs`
    *   [x] Add `namespace PirateRoguelike.Services` to:
        *   `EconomyService.cs`
        *   `Inventory.cs`
        *   `RewardService.cs`
    *   [x] Add `namespace PirateRoguelike.Events` to:
        *   `GameEvents.cs`

5.  **Add Namespaces for Files in `Assets/Scripts/Data/`:**
    *   [x] Add `namespace PirateRoguelike.Data` to:
        *   `EnemySO.cs`
        *   `ItemSO.cs`
        *   `RunConfigSO.cs`

6.  **Add Namespace for `Assets/Scripts/Encounters/ShopManager.cs`:**
    6.  **Add Namespace for `Assets/Scripts/Encounters/ShopManager.cs`:**
    *   [x] Add `namespace PirateRoguelike.Encounters` to `ShopManager.cs`.

7.  **Add/Update Namespaces for Files in `Assets/Scripts/Map/`:**
    *   [x] Add `namespace Pirate.MapGen` to:
        *   `MapNodeData.cs`
        *   `MapManager.cs`

8.  **Add Namespaces for Files in `Assets/Scripts/Saving/`:**
    *   [x] Add `namespace PirateRoguelike.Saving` to:
        *   `RunModifier.cs`
        *   `RunState.cs`
        *   `SaveManager.cs`
        *   `SerializableItemInstance.cs`
        *   `SerializableShipState.cs`

9.  **Add Namespaces for Files in `Assets/Scripts/UI/`:**
    9.  **Add Namespaces for Files in `Assets/Scripts/UI/`:**
    *   [x] Add `namespace PirateRoguelike.UI` to:
        *   `BattleUIController.cs`
        *   `EffectDisplay.cs`
        *   `InventorySlotUI.cs`
        *   `InventoryUI.cs`
        *   `MainMenuController.cs`
        *   `MapView.cs`
        *   `RewardItemSlot.cs`
        *   `RewardUIController.cs`
        *   `ShipViewUI.cs`
        *   `ShopItemViewUI.cs`
        *   `TooltipController.cs`
        *   `UIManager.cs`

10. **Add Namespace for `Assets/Scripts/Utility/SerializableDictionary.cs`:**
    *   [x] Add `namespace PirateRoguelike.Utility` to `SerializableDictionary.cs`.

11. **Review and Update `using` Statements:** After applying the namespace changes, a comprehensive review of all C# files will be necessary to update `using` statements to reflect the new namespace structure. This will be done as part of the implementation phase.
    *   [x] All `using` statements reviewed and updated.

This plan ensures that all C# files within the project adhere to a consistent and logical namespace structure, improving code organization, readability, and maintainability.

Present this plan for user approval.