---
title: "UI Systems Overview"
weight: 10
system: ["ui"]
types: ["system-overview"]
---

## 1. Universal Tooltip System

This section details the implementation of the dynamic tooltip description system. It leverages the [runtime item system]({{< myrelref "../core/runtime-data-systems.md" >}}) to display real-time, updated values in UI elements.

### 1.1. Core Components

*   **`IRuntimeContext` (`PirateRoguelike.Combat.IRuntimeContext`):**
    >   File Path: Assets/Scripts/Combat/IRuntimeContext.cs
    *   A simple interface used to pass necessary runtime data (e.g., the current combat context, player/enemy `ShipState`s) to the `BuildDescription` methods of `PirateRoguelike.Runtime.RuntimeAction`s. This allows descriptions to be generated based on the current game state.

*   **`BuildDescription()` Method:**
    *   Each `RuntimeAction` implementation now overrides an abstract `BuildDescription(IRuntimeContext context)` method.
    *   This method is responsible for generating a human-readable description of the action, incorporating its *current, dynamic* values.

*   **`TooltipController` (`PirateRoguelike.UI.TooltipController`):**
    >   File Path: Assets/Scripts/UI/TooltipController.cs
    *   A persistent singleton (`MonoBehaviour`) that orchestrates all tooltip activity.
    *   Instantiates the tooltip from `TooltipPanel.uxml` and adds it to a global UI `rootVisualElement` to ensure correct z-ordering across all UI elements.
    *   Exposes a public `Show(RuntimeItem item, VisualElement targetElement)` method to display and position the tooltip.
    *   Exposes a public `Hide()` method to conceal the tooltip.
    *   Dynamically populates the tooltip's UXML fields with data from the provided `RuntimeItem`.
    *   Manages the fade-in/out animation by toggling USS classes.
    *   Positions the tooltip adjacent to the `targetElement`.
    *   Ensures proper initialization order by being initialized in `RunManager.Awake()`.
        *   A persistent singleton (`MonoBehaviour`) that orchestrates all tooltip activity.
    *   Instantiates the tooltip from `TooltipPanel.uxml` and adds it to a global UI `rootVisualElement` to ensure correct z-ordering across all UI elements.
    *   Exposes a public `Show(RuntimeItem item, VisualElement targetElement)` method to display and position the tooltip.
    *   Exposes a public `Hide()` method to conceal the tooltip.
    *   Dynamically populates the tooltip's UXML fields with data from the provided `RuntimeItem`.
    *   Manages the fade-in/out animation by toggling USS classes.
    *   Positions the tooltip adjacent to the `targetElement`.
    *   Ensures proper initialization order by being initialized by the `UIManager`.
    *   **Instance Management:** The `TooltipController` maintains a `Dictionary<VisualElement, VisualElement>`. It stores a unique tooltip UI element instance for each `panelRoot` it interacts with.
    *   **On-Demand Instantiation:** When a tooltip needs to be shown on a panel for the first time, the `TooltipController` instantiates a new tooltip from a `VisualTreeAsset` and adds it to that panel's visual tree. This new instance is then cached in the dictionary.
    *   **Stylesheet Injection:** Crucially, when creating a new tooltip instance for a panel, the `TooltipController` also ensures that its required stylesheet (`TooltipPanelStyle.uss`) is added to the panel's `styleSheets` list. This guarantees the tooltip will always render correctly, regardless of the panel's default styles.

*   **`EffectDisplay` (`PirateRoguelike.UI.EffectDisplay`):**
    >   File Path: Assets/Scripts/UI/EffectDisplay.cs
    *   A small, reusable controller class created to manage the individual effect descriptions within the tooltip.
    *   Takes a `RuntimeAbility` as input.
    *   Populates the icon and description label of an `#ActiveEffect` or `#PassiveEffect` element.
    *   The `SetData()` method accepts a `RuntimeAbility` and an `IRuntimeContext`.
    *   It iterates through the `RuntimeAbility`'s `RuntimeAction`s and calls `runtimeAction.BuildDescription(context)` to get the dynamic description string.

*   **`TooltipUtility` (`PirateRoguelike.UI.Utilities.TooltipUtility`):**
    >   File Path: Assets/Scripts/UI/TooltipUtility.cs
    *   A static helper class that provides a simple method (`RegisterTooltipCallbacks`) for registering the necessary `PointerEnterEvent` and `PointerLeaveEvent` on a UI element. It takes an `ISlotViewData` and a `panelRoot` as input, and passes the `RuntimeItem` from the `ISlotViewData` to the `TooltipController.Show()` method.

*   **`TooltipPanelStyle.uss`:**
    *   The stylesheet that contains the necessary styles (`.tooltip--visible`, `.tooltip--hidden`, etc.) to control the tooltip's appearance and animations.

### 1.2. UI Integration

This system is used by multiple parts of the UI to ensure consistent behavior.

*   **`PlayerPanelView.cs`:** The player's inventory and equipment slots are composed of `SlotElement`s, which are populated with `ItemElement`s based on data from the `PlayerPanelDataViewModel`.
*   **`EnemyPanelController.cs`:** The enemy's equipment display dynamically creates `SlotElement`s and binds them to the enemy's `ShipState` data.
*   **`ShopController.cs`:** The shop interface is now a primary consumer of this system. It uses `SlotElement`s to display items for sale. It uses a special `ShopSlotViewModel` to provide price data, and contextual CSS to make the price visible only within the shop. Crucially, `ShopController` now explicitly registers tooltip callbacks for each shop item, ensuring tooltips appear on hover.
*   **`RewardUIController.cs`:** The reward interface utilizes this system for displaying reward items. It uses `SlotElement`s to display items and `RewardItemSlotViewData` to provide item data. It is explicitly called by `RunManager` (for battle rewards) or `DebugConsoleController` (for debug rewards) to display rewards. It integrates with the `UIManager`'s global root for proper layering and event handling, and registers tooltip callbacks for reward items.

## 2. Universal Item Manipulation System

This system centralizes the logic for moving, equipping, and swapping items, decoupling the UI from direct game state manipulation. It interacts with the [runtime item system]({{< myrelref "../core/runtime-data-systems.md" >}}).

### 2.1. Core Components

*   **`ItemManipulationService` (`PirateRoguelike.Services.ItemManipulationService`):**
    >   File Path: Assets/Scripts/Core/ItemManipulationService.cs
    *   A singleton that acts as the central authority for all item operations.
    *   Interacts directly with `GameSession`'s `Inventory` and `PlayerShip` to modify the underlying game state.

*   **`ItemManipulationEvents` (`PirateRoguelike.Events.ItemManipulationEvents`):**
    >   File Path: Assets/Scripts/Core/ItemManipulationEvents.cs
    *   A static event bus that broadcasts notifications when an item manipulation occurs (e.g., `OnItemMoved`, `OnItemAdded`, `OnItemRemoved`).

*   **`SlotManipulator` (`PirateRoguelike.UI.SlotManipulator`):**
    >   File Path: Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs
    *   A `PointerManipulator` attached to each `ItemElement`. It detects drag-and-drop gestures.
    *   **Drag Initiation:** Dragging is now initiated only after the mouse moves beyond a small threshold from the initial click position, allowing for distinct click actions.
    *   Initiates requests by calling methods on `ItemManipulationService` (e.g., `SwapItems`, `RequestPurchase`, `RequestClaimReward`). It passes the specific `ItemSO` to be claimed, ensuring correct item manipulation. It does not modify game state directly.
    *   **Shop Item Interaction:**
        *   **Click-to-Buy:** A quick click on a shop item triggers an immediate purchase to an available inventory slot.
        *   **Drag-and-Drop Purchase:** Dragging a shop item and dropping it over a valid inventory or equipment slot triggers a purchase. Dropping it elsewhere cancels the purchase, and the item visually returns to the shop.
    *   **Conflict Prevention:** This manipulator now checks `UIStateService.IsConsoleOpen` and will not initiate drag operations if the debug console is active, preventing input conflicts.

*   **`Inventory` (`PirateRoguelike.Services.Inventory`):**
    >   File Path: Assets/Scripts/Core/Inventory.cs
*   **`ShipState` (`PirateRoguelike.Core.ShipState`):**
    >   File Path: Assets/Scripts/Core/ShipState.cs
    *   These classes manage the item collections and dispatch `ItemManipulationEvents` after any modification to their slots.

*   **`PlayerPanelDataViewModel` (`PirateRoguelike.UI.PlayerPanelDataViewModel`):**
    >   File Path: Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs
    *   Subscribes to `ItemManipulationEvents` (`OnItemMoved`, `OnItemAdded`, `OnItemRemoved`).
    *   Upon receiving an event, it updates its `ObservableList<ISlotViewData>` collections, which automatically triggers UI updates through data binding.

*   **`SlotElement` (`PirateRoguelike.UI.Components.SlotElement`):**
    >   File Path: Assets/Scripts/UI/Components/SlotElement.cs
    *   Represents a fixed visual container for an item.
    *   Crucially, `SlotElement` observes its bound `ISlotViewData`. When the `CurrentItemInstance` property changes, it automatically creates, binds, or disposes of its child `ItemElement` as needed.

*   **`ItemElement` (`PirateRoguelike.UI.Components.ItemElement`):**
    >   File Path: Assets/Scripts/UI/Components/ItemElement.cs
    *   Represents the movable, visual representation of an item (icon, frame, etc.).
    *   Its `SlotManipulator` handles initiating drag-and-drop operations.

*   **`UIInteractionService` (`PirateRoguelike.Services.UIInteractionService`):**
    >   File Path: Assets/Scripts/Core/UIInteractionService.cs
    *   A static class that holds the current global UI state (e.g., `IsInCombat`). It provides methods that the UI can query to ask for permission to perform an action.
    *   The `ItemManipulationService` will only perform actions after they have been approved by the `UIInteractionService`.
    *   This service now allows item manipulation for `SlotContainerType.Shop` when not in combat, enabling shop item purchases.

### 2.2. How it Works (Example: Item Swap)

1.  A user drags an `ItemElement` from `Slot A` and drops it onto `Slot B`.
2.  The `SlotManipulator` on the dragged `ItemElement` detects the drop and calls `ItemManipulationService.Instance.SwapItems(Slot A ID, Slot B ID)`.
3.  `ItemManipulationService` performs the logic to swap the `ItemInstance` objects between the source and destination containers (`Inventory` or `ShipState`).
4.  During this process, `Inventory` and/or `ShipState` dispatch `ItemManipulationEvents` (e.g., `OnItemMoved`).
5.  The `PlayerPanelDataViewModel` receives these events and updates the `CurrentItemInstance` property on the affected `SlotDataViewModel` objects in its `ObservableList`s.
6.  Because the `SlotElement`s are bound to these view models, their `PropertyChanged` event fires.
7.  The `SlotElement` for `Slot A` and `Slot B` detect the change to `CurrentItemInstance` and call their `UpdateItemElement` method, which visually reflects the swap by re-binding, creating, or destroying their child `ItemElement`s.

### 2.3. Benefits of the Universal System

*   **Centralized Logic:** All item manipulation logic is in one place, making it easier to maintain and debug.
*   **Decoupling:** UI components are decoupled from direct game state modification, reacting to data changes via view models and events.
*   **Data Consistency:** The service ensures all operations are performed correctly.
*   **Testability:** The centralized service is easier to test in isolation.
*   **Modularity:** New container types can be added more easily.

## 3. Enemy Panel Integration

The enemy panel now fully utilizes the new runtime item system and tooltip setup, with all logic consolidated into the `EnemyPanelController`. The previously separate `EnemyPanelView` class has been removed.

### 3.1. Core Components

*   **`EnemyPanelController` (`PirateRoguelike.UI.EnemyPanelController`):**
    >   File Path: Assets/Scripts/UI/EnemyPanel/EnemyPanelController.cs
    *   Manages the visual elements of the enemy ship panel.
    *   Acts as an adapter, creating an `EnemyShipViewData` view model from the enemy's `ShipState`.
    *   Dynamically creates `SlotElement` instances in code for each equipment slot.
    *   Uses `TooltipUtility.RegisterTooltipCallbacks` on each `SlotElement` to handle pointer events for showing and hiding tooltips.
    *   Subscribes to `ItemManipulationEvents.OnItemMoved` to refresh its display when items change.

### 3.2. Integration

*   The `EnemyPanelController` is instantiated and initialized within the [CombatController]({{< myrelref "../combat/combat-system-overview.md" >}}), receiving the enemy's `ShipState`.
*   It creates a `SlotElement` for each equipped item and binds it to a `SlotDataViewModel`.
*   The `TooltipUtility` registers `PointerEnterEvent` and `PointerLeaveEvent` callbacks on these slots.
*   These callbacks trigger the `TooltipController.Show()` and `Hide()` methods, passing the `RuntimeItem` from the slot's view model. This ensures tooltips function correctly for enemy items and reflect any dynamic changes.

## 4. Key Files Involved

### UI - Controllers & ViewModels
*   `Assets/Scripts/UI/UIManager.cs` (New central UI manager)
*   `Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs` (Contains `PlayerPanelDataViewModel` and `SlotDataViewModel`) - For more details on the underlying data structures, refer to the [Runtime Data Systems Overview]({{< myrelref "../core/runtime-data-systems.md" >}}).
*   `Assets/Scripts/UI/EnemyPanel/EnemyPanelController.cs` - This controller manages the enemy's UI panel. For more details on combat systems, refer to the [Combat System Overview]({{< myrelref "../combat/combat-system-overview.md" >}}). For a comprehensive understanding of core game systems, refer to the [Core Systems Overview]({{< myrelref "../core/_index.md" >}}).
*   `Assets/Scripts/UI/PlayerPanel/PlayerPanelData.cs` (Contains `ISlotViewData`, `IPlayerPanelData` interfaces etc.)
*   `Assets/Scripts/UI/TooltipController.cs` - This controller manages the tooltip system. For more details on combat systems, refer to the [Combat System Overview]({{< myrelref "../combat/combat-system-overview.md" >}}).

### UI - Components & Manipulators
*   `Assets/Scripts/UI/Components/SlotElement.cs` (The slot container)
*   `Assets/Scripts/UI/Components/ItemElement.cs` (The draggable item)
*   `Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs`
*   `Assets/Scripts/UI/EffectDisplay.cs`
*   `Assets/Scripts/UI/TooltipUtility.cs` - This utility provides helper methods for tooltip management. For a comprehensive understanding of core game systems, refer to the [Core Systems Overview]({{< myrelref "../core/_index.md" >}}).
*   `Assets/Scripts/Core/UIInteractionService.cs` - This service is crucial for managing global UI state and ensuring that UI actions are only performed when appropriate. For a comprehensive understanding of core game systems, refer to the [Core Systems Overview]({{< myrelref "../core/_index.md" >}}).

## 5. UI Toolkit Event Handling Notes

*   **`TrickleDown.TrickleDown`:** When registering callbacks for `KeyDownEvent` (and other events where early intervention is critical), using `TrickleDown.TrickleDown` ensures that your callback is executed during the TrickleDown phase of event propagation. This allows you to process or `PreventDefault()` the event before other elements or the default behavior of the target element (e.g., `TextField`) can consume or modify it. This is crucial for achieving precise control over UI element behavior, especially for input fields.


