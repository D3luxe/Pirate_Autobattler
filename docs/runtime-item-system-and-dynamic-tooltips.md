# Runtime Item System and Dynamic Tooltips

This document details the implementation of a new runtime item, ability, and action system, along with an enhanced dynamic tooltip description system. These changes address the limitations of using static `ScriptableObject`s for mutable game data and provide a more flexible and accurate way to display item information in the UI.

## 1. Problem Statement

Previously, game items, abilities, and actions were primarily defined by `ScriptableObject` assets. While excellent for defining static, immutable data templates, this approach proved rigid when dealing with dynamic gameplay scenarios such as:

*   Applying temporary buffs or debuffs to item stats (e.g., an item's damage increasing during combat).
*   Modifying item properties permanently within a game run (e.g., an event granting a permanent damage bonus to an item).
*   Displaying real-time, updated values in UI elements like tooltips, as the `ScriptableObject`s only held base values.

## 2. Solution: Runtime Representations

To overcome these limitations, a new layer of runtime C# classes has been introduced. These classes act as mutable wrappers around their corresponding `ScriptableObject` blueprints, allowing for dynamic state management during gameplay.

### 2.1. Core Components

*   **`RuntimeItem` (`Assets/Scripts/Runtime/RuntimeItem.cs`):**
    *   Represents a unique instance of an item in the game.
    *   Holds a reference to its `ItemSO` blueprint for base data.
    *   Contains mutable fields for dynamic properties (e.g., `CooldownRemaining`).
    *   Manages a collection of `RuntimeAbility` instances.

*   **`RuntimeAbility` (`Assets/Scripts/Runtime/RuntimeAbility.cs`):**
    *   Represents a unique instance of an ability associated with a `RuntimeItem`.
    *   Holds a reference to its `AbilitySO` blueprint.
    *   Manages a collection of `RuntimeAction` instances.

*   **`RuntimeAction` (`Assets/Scripts/Runtime/RuntimeAction.cs`):**
    *   An abstract base class for all unique action instances.
    *   Holds a reference to its `ActionSO` blueprint.
    *   Defines the `BuildDescription(IRuntimeContext context)` method for dynamic tooltip text generation.

*   **Concrete `RuntimeAction` Implementations (`Assets/Scripts/Runtime/`):
    *   `RuntimeDamageAction`:
        *   Wraps `DamageActionSO`.
        *   Holds `CurrentDamageAmount` (mutable).
    *   `RuntimeHealAction`:
        *   Wraps `HealActionSO`.
        *   Holds `CurrentHealAmount` (mutable).
    *   `RuntimeApplyEffectAction`:
        *   Wraps `ApplyEffectActionSO`.
        *   References `EffectToApply`.

### 2.2. How it Works

1.  When an item is generated or spawned in the game (e.g., from a shop, inventory, or reward), a `RuntimeItem` instance is created.
2.  The `RuntimeItem`'s constructor recursively instantiates `RuntimeAbility` and `RuntimeAction` objects based on the `ItemSO`'s abilities and actions.
3.  These `RuntimeAction` instances initialize their mutable fields (e.g., `CurrentDamageAmount`) from the base values defined in their respective `ActionSO` blueprints.
4.  Any buffs, debuffs, or other runtime modifications are applied directly to the mutable fields of these `RuntimeItem`, `RuntimeAbility`, and `RuntimeAction` instances.

## 3. Dynamic Tooltip Description System

To ensure tooltips accurately reflect the dynamic state of items, the tooltip system has been updated to leverage these new runtime representations.

### 3.1. Core Components

*   **`IRuntimeContext` (`Assets/Scripts/Combat/IRuntimeContext.cs`):**
    *   A simple interface used to pass necessary runtime data (e.g., the current combat context, player/enemy `ShipState`s) to the `BuildDescription` methods of `RuntimeAction`s. This allows descriptions to be generated based on the current game state.

*   **`BuildDescription()` Method:**
    *   Each `RuntimeAction` implementation now overrides an abstract `BuildDescription(IRuntimeContext context)` method.
    *   This method is responsible for generating a human-readable description of the action, incorporating its *current, dynamic* values.

### 3.2. UI Integration

*   **`TooltipController` (`Assets/Scripts/UI/TooltipController.cs`):**
    *   The `Show()` method now accepts a `RuntimeItem` instance instead of an `ItemSO`.
    *   It passes the `RuntimeAbility` instances (from the `RuntimeItem`) to the `EffectDisplay`.
    *   A `DummyRuntimeContext` is currently used as a placeholder, which will need to be replaced with a proper context from the game state (e.g., `CombatContext`) when fully integrated.

*   **`EffectDisplay` (`Assets/Scripts/UI/EffectDisplay.cs`):**
    *   The `SetData()` method now accepts a `RuntimeAbility` and an `IRuntimeContext`.
    *   It iterates through the `RuntimeAbility`'s `RuntimeAction`s and calls `runtimeAction.BuildDescription(context)` to get the dynamic description string.

## 4. Enemy Panel Integration

The enemy panel now fully utilizes the new runtime item system and tooltip setup, with all logic consolidated into the `EnemyPanelController`. The previously separate `EnemyPanelView` is deprecated.

### 4.1. Core Components

*   **`EnemyPanelController` (`Assets/Scripts/UI/EnemyPanelController.cs`):**
    *   Manages the visual elements of the enemy ship panel (ship display, equipment slots).
    *   Acts as an adapter between the enemy's `ShipState` data and the UI.
    *   Dynamically instantiates item slots from a UXML template.
    *   Populates the enemy's equipment slots with `RuntimeItem` instances.
    *   Registers pointer events for tooltip display.
    *   Subscribes to enemy `ShipState` events (`OnHealthChanged`, `OnEquipmentChanged`) to ensure the UI updates dynamically.

### 4.2. Integration

*   The `EnemyPanelController` is instantiated and initialized within the `CombatController` (specifically in its `Init()` method), receiving the enemy's `ShipState`.
*   It dynamically creates UI Toolkit `VisualElement`s for each equipped item, using a shared slot UXML template.
*   It registers `PointerEnterEvent` and `PointerLeaveEvent` callbacks on these dynamically created slots.
*   These callbacks trigger the `TooltipController.Show()` and `Hide()` methods, passing the `RuntimeItem` associated with the slot, ensuring that tooltips function correctly for enemy-equipped items and reflect any dynamic changes to their stats.

## 5. Benefits of the New System

*   **Flexibility:** Enables dynamic modification of item, ability, and action values at runtime, supporting buffs, debuffs, and permanent in-run changes.
*   **Accuracy:** Tooltips now display real-time, accurate information, reflecting the current state of an item's properties.
*   **Modularity:** Clearly separates static data definitions (`ScriptableObject`s) from mutable runtime state, improving code organization and maintainability.
*   **Testability:** Runtime objects are easier to test in isolation, as their state can be manipulated directly.
*   **Consistency:** The same underlying system is used for both player and enemy item management and UI display.

## 6. Key Files Involved

*   `Assets/Scripts/Combat/ItemInstance.cs`
*   `Assets/Scripts/Runtime/RuntimeItem.cs`
*   `Assets/Scripts/Runtime/RuntimeAbility.cs`
*   `Assets/Scripts/Runtime/RuntimeAction.cs`
*   `Assets/Scripts/Runtime/RuntimeDamageAction.cs`
*   `Assets/Scripts/Runtime/RuntimeHealAction.cs`
*   `Assets/Scripts/Runtime/RuntimeApplyEffectAction.cs`
*   `Assets/Scripts/Combat/IRuntimeContext.cs`
*   `Assets/Scripts/UI/TooltipController.cs`
*   `Assets/Scripts/UI/EffectDisplay.cs`
*   `Assets/Scripts/UI/PlayerPanel/PlayerPanelData.cs`
*   `Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs`
*   `Assets/Scripts/UI/PlayerPanel/DevDriver.cs`
*   `Assets/Scripts/UI/EnemyPanel/EnemyPanelView.cs`
*   `Assets/Scripts/UI/EnemyPanel/EnemyPanelController.cs`
*   `Assets/Scripts/Core/GameSession.cs`
*   `Assets/Scripts/Core/ShipState.cs`
*   `Assets/Scripts/Saving/SerializableItemInstance.cs`
