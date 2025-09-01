# Runtime Item System and Dynamic Tooltips

This document details the implementation of a new runtime item, ability, and action system, along with an enhanced dynamic tooltip description system. These changes address the limitations of using static `ScriptableObject`s for mutable game data and provide a more flexible and accurate way to display item information in the UI.

## 1. Problem Statement

Previously, game items, abilities, and actions were primarily defined by `ScriptableObject` assets. While excellent for defining static, immutable data templates, this approach proved rigid when dealing with dynamic gameplay scenarios such as:

*   Applying temporary buffs or debuffs to item stats (e.g., an item's damage increasing during combat).
*   Modifying item properties permanently within a game run (e.g., an event granting a permanent damage bonus to an item).
*   Displaying real-time, updated values in UI elements like tooltips, as the `ScriptableObject`s only held base values.

## 2. Solution: Runtime Representations

To overcome these limitations, a new layer of runtime C# classes has been introduced. These classes act as wrappers around their corresponding `ScriptableObject` blueprints, allowing for dynamic state management during gameplay. The core of this system is the `ItemInstance` class, which represents a unique item within the game world.

### 2.1. Core Components

*   **`ItemInstance` (`Assets/Scripts/Combat/ItemInstance.cs`):**
    *   The primary class representing a unique instance of an item in the game (e.g., in an inventory or equipped on a ship).
    *   Holds a reference to its `ItemSO` blueprint for base data.
    *   Contains mutable fields for instance-specific state like `CooldownRemaining` and `StunDuration`.
    *   Crucially, it holds a reference to a `RuntimeItem` instance.

*   **`RuntimeItem` (`Assets/Scripts/Runtime/RuntimeItem.cs`):**
    *   Represents the dynamic abilities and actions of an `ItemInstance`.
    *   Itself created from an `ItemSO` blueprint.
    *   Manages a collection of `RuntimeAbility` instances. This is where dynamic modifications to an item's *behavior* would be applied.

*   **`RuntimeAbility` (`Assets/Scripts/Runtime/RuntimeAbility.cs`):**
    *   Represents a unique instance of an ability associated with a `RuntimeItem`.
    *   Holds a reference to its `AbilitySO` blueprint.
    *   Manages a collection of `RuntimeAction` instances.

*   **`RuntimeAction` (`Assets/Scripts/Runtime/RuntimeAction.cs`):**
    *   An abstract base class for all unique action instances.
    *   Holds a reference to its `ActionSO` blueprint.
    *   Defines the `BuildDescription(IRuntimeContext context)` method for dynamic tooltip text generation.

*   **Concrete `RuntimeAction` Implementations (`Assets/Scripts/Runtime/`):**
    *   `RuntimeDamageAction`, `RuntimeHealAction`, `RuntimeApplyEffectAction`, etc. These wrap their corresponding `ActionSO` and hold mutable, runtime-specific values (e.g., `CurrentDamageAmount`).

### 2.2. How it Works

1.  When an item is needed in the game (e.g., from a shop, reward, or being equipped), an **`ItemInstance`** is created from an `ItemSO`.
2.  The `ItemInstance`'s constructor immediately creates a corresponding **`RuntimeItem`** instance, also from the `ItemSO`.
3.  The `RuntimeItem`'s constructor then recursively instantiates `RuntimeAbility` and `RuntimeAction` objects based on the `ItemSO`'s configuration.
4.  These `RuntimeAction` instances initialize their mutable fields (e.g., `CurrentDamageAmount`) from the base values in their `ActionSO` blueprints.
5.  The `ItemInstance` is what gets stored in the `Inventory` or `ShipState`. Any buffs, debuffs, or other runtime modifications are applied to the mutable fields within the `ItemInstance` or its nested `Runtime...` objects.

## 3. Dynamic Tooltip Description System

To ensure tooltips accurately reflect the dynamic state of items, the tooltip system has been updated to leverage these new runtime representations.

### 3.1. Core Components

*   **`IRuntimeContext` (`Assets/Scripts/Combat/IRuntimeContext.cs`):**
    *   A simple interface used to pass necessary runtime data (e.g., the current combat context, player/enemy `ShipState`s) to the `BuildDescription` methods of `RuntimeAction`s. This allows descriptions to be generated based on the current game state.

*   **`BuildDescription()` Method:**
    *   Each `RuntimeAction` implementation overrides an abstract `BuildDescription(IRuntimeContext context)` method.
    *   This method is responsible for generating a human-readable description of the action, incorporating its *current, dynamic* values.

### 3.2. UI Integration

*   **`TooltipController` (`Assets/Scripts/UI/TooltipController.cs`):**
    *   The `Show()` method accepts a `RuntimeItem` instance (retrieved from the `ISlotViewData`'s `ItemData` property) and the target `VisualElement`.
    *   It passes the `RuntimeAbility` instances (from the `RuntimeItem`) to an `EffectDisplay`.
    *   A `DummyRuntimeContext` is currently used as a placeholder, which will need to be replaced with a proper context from the game state (e.g., `CombatContext`) when fully integrated.

*   **`EffectDisplay` (`Assets/Scripts/UI/EffectDisplay.cs`):**
    *   The `SetData()` method accepts a `RuntimeAbility` and an `IRuntimeContext`.
    *   It iterates through the `RuntimeAbility`'s `RuntimeAction`s and calls `runtimeAction.BuildDescription(context)` to get the dynamic description string.

## 4. Enemy Panel Integration

The enemy panel now fully utilizes the new runtime item system and tooltip setup, with all logic consolidated into the `EnemyPanelController`. The previously separate `EnemyPanelView` class has been removed.

### 4.1. Core Components

*   **`EnemyPanelController` (`Assets/Scripts/UI/EnemyPanel/EnemyPanelController.cs`):**
    *   Manages the visual elements of the enemy ship panel.
    *   Acts as an adapter, creating an `EnemyShipViewData` view model from the enemy's `ShipState`.
    *   Dynamically creates `SlotElement` instances in code for each equipment slot.
    *   Uses `TooltipUtility.RegisterTooltipCallbacks` on each `SlotElement` to handle pointer events for showing and hiding tooltips.
    *   Subscribes to `ItemManipulationEvents.OnItemMoved` to refresh its display when items change.

### 4.2. Integration

*   The `EnemyPanelController` is instantiated and initialized within the `CombatController`, receiving the enemy's `ShipState`.
*   It creates a `SlotElement` for each equipped item and binds it to a `SlotDataViewModel`.
*   The `TooltipUtility` registers `PointerEnterEvent` and `PointerLeaveEvent` callbacks on these slots.
*   These callbacks trigger the `TooltipController.Show()` and `Hide()` methods, passing the `RuntimeItem` from the slot's view model. This ensures tooltips function correctly for enemy items and reflect any dynamic changes.

## 5. Benefits of the New System

*   **Flexibility:** Enables dynamic modification of item, ability, and action values at runtime.
*   **Accuracy:** Tooltips now display real-time, accurate information.
*   **Modularity:** Clearly separates static data (`ScriptableObject`s) from instance data (`ItemInstance`) and dynamic behavior data (`RuntimeItem`), improving code organization.
*   **Testability:** Runtime objects are easier to test in isolation.
*   **Consistency:** The same underlying system is used for both player and enemy item management and UI display.

## 6. Universal Item Manipulation System

This system centralizes the logic for moving, equipping, and swapping items, decoupling the UI from direct game state manipulation.

### 6.1. Core Components

*   **`ItemManipulationService` (`Assets/Scripts/Core/ItemManipulationService.cs`):**
    *   A singleton that acts as the central authority for all item operations.
    *   Interacts directly with `GameSession`'s `Inventory` and `PlayerShip` to modify the underlying game state.

*   **`ItemManipulationEvents` (`Assets/Scripts/Core/ItemManipulationEvents.cs`):**
    *   A static event bus that broadcasts notifications when an item manipulation occurs (e.g., `OnItemMoved`, `OnItemAdded`, `OnItemRemoved`).

*   **`SlotManipulator` (`Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs`):**
    *   A `PointerManipulator` attached to each `ItemElement`. It detects drag-and-drop gestures.
    *   Initiates requests by calling methods on `ItemManipulationService` (e.g., `SwapItems`). It does not modify game state directly.

*   **`Inventory` (`Assets/Scripts/Core/Inventory.cs`) and `ShipState` (`Assets/Scripts/Core/ShipState.cs`):**
    *   These classes manage the item collections and dispatch `ItemManipulationEvents` after any modification to their slots.

*   **`PlayerPanelDataViewModel` (`Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs`):**
    *   Subscribes to `ItemManipulationEvents` (`OnItemMoved`, `OnItemAdded`, `OnItemRemoved`).
    *   Upon receiving an event, it updates its `ObservableList<ISlotViewData>` collections, which automatically triggers UI updates through data binding.

*   **`SlotElement` (`Assets/Scripts/UI/Components/SlotElement.cs`):**
    *   Represents a fixed visual container for an item.
    *   Crucially, `SlotElement` observes its bound `ISlotViewData`. When the `CurrentItemInstance` property changes, it automatically creates, binds, or disposes of its child `ItemElement` as needed.

*   **`ItemElement` (`Assets/Scripts/UI/Components/ItemElement.cs`):**
    *   Represents the movable, visual representation of an item (icon, frame, etc.).
    *   Its `SlotManipulator` handles initiating drag-and-drop operations.

### 6.2. How it Works (Example: Item Swap)

1.  A user drags an `ItemElement` from `Slot A` and drops it onto `Slot B`.
2.  The `SlotManipulator` on the dragged `ItemElement` detects the drop and calls `ItemManipulationService.Instance.SwapItems(Slot A ID, Slot B ID)`.
3.  `ItemManipulationService` performs the logic to swap the `ItemInstance` objects between the source and destination containers (`Inventory` or `ShipState`).
4.  During this process, `Inventory` and/or `ShipState` dispatch `ItemManipulationEvents` (e.g., `OnItemMoved`).
5.  The `PlayerPanelDataViewModel` receives these events and updates the `CurrentItemInstance` property on the affected `SlotDataViewModel` objects in its `ObservableList`s.
6.  Because the `SlotElement`s are bound to these view models, their `PropertyChanged` event fires.
7.  The `SlotElement` for `Slot A` and `Slot B` detect the change to `CurrentItemInstance` and call their `UpdateItemElement` method, which visually reflects the swap by re-binding, creating, or destroying their child `ItemElement`s.

## 7. Benefits of the Universal System

*   **Centralized Logic:** All item manipulation logic is in one place, making it easier to maintain and debug.
*   **Decoupling:** UI components are decoupled from direct game state modification, reacting to data changes via view models and events.
*   **Data Consistency:** The service ensures all operations are performed correctly.
*   **Testability:** The centralized service is easier to test in isolation.
*   **Modularity:** New container types can be added more easily.

## 8. Key Files Involved

### Core Systems & Data
*   `Assets/Scripts/Core/ItemManipulationService.cs`
*   `Assets/Scripts/Core/ItemManipulationEvents.cs`
*   `Assets/Scripts/Core/GameSession.cs`
*   `Assets/Scripts/Core/Inventory.cs`
*   `Assets/Scripts/Core/ShipState.cs`
*   `Assets/Scripts/Saving/SerializableItemInstance.cs`

### Runtime Item Representation
*   `Assets/Scripts/Combat/ItemInstance.cs` (Primary runtime object)
*   `Assets/Scripts/Runtime/RuntimeItem.cs` (Handles abilities/actions)
*   `Assets/Scripts/Runtime/RuntimeAbility.cs`
*   `Assets/Scripts/Runtime/RuntimeAction.cs`
*   `Assets/Scripts/Combat/IRuntimeContext.cs`

### UI - Controllers & ViewModels
*   `Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs` (Contains `PlayerPanelDataViewModel` and `SlotDataViewModel`)
*   `Assets/Scripts/UI/EnemyPanel/EnemyPanelController.cs`
*   `Assets/Scripts/UI/PlayerPanel/PlayerPanelData.cs` (Contains `ISlotViewData`, `IPlayerPanelData` interfaces etc.)
*   `Assets/Scripts/UI/TooltipController.cs`

### UI - Components & Manipulators
*   `Assets/Scripts/UI/Components/SlotElement.cs` (The slot container)
*   `Assets/Scripts/UI/Components/ItemElement.cs` (The draggable item)
*   `Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs`
*   `Assets/Scripts/UI/EffectDisplay.cs`
