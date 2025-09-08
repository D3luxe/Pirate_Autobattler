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
    *   A persistent singleton (`MonoBehaviour`) that orchestrates all tooltip activity. It is instantiated by `GameInitializer` and initialized by `UIManager`.
    *   Exposes public `Show(RuntimeItem item, VisualElement targetElement, VisualElement panelRoot)` and `Hide()` methods to control tooltip visibility and positioning.
    *   Dynamically populates the tooltip's UXML fields with data from the provided `RuntimeItem`.
    *   Manages fade-in/out animations by toggling USS classes.
    *   **Instance Management:** To ensure tooltips work across different UI panels (e.g., main UI, shop, rewards), the controller maintains a dictionary of tooltip instances, creating one on-demand for each new panel root it encounters.
    *   **Stylesheet Injection:** When creating a new tooltip instance, it also injects the required `TooltipPanelStyle.uss` into the panel's stylesheets, guaranteeing correct rendering.

*   **`EffectDisplay` (`PirateRoguelike.UI.EffectDisplay`):**
    >   File Path: Assets/Scripts/UI/EffectDisplay.cs
    *   A small, reusable controller class created to manage the individual effect descriptions within the tooltip.
    *   Takes a `RuntimeAbility` as input.
    *   Populates the icon and description label of an `#ActiveEffect` or `#PassiveEffect` element.
    *   The `SetData()` method accepts a `RuntimeAbility` and an `IRuntimeContext`.
    *   It iterates through the `RuntimeAbility`'s `RuntimeAction`s and calls `runtimeAction.BuildDescription(context)` to get the dynamic description string.

*   **`TooltipUtility` (`PirateRoguelike.UI.Utilities.TooltipUtility`):**
    >   File Path: Assets/Scripts/UI/Utilities/TooltipUtility.cs
    *   A static helper class that provides a simple method (`RegisterTooltipCallbacks`) for registering the necessary `PointerEnterEvent` and `PointerLeaveEvent` on a UI element. It takes an `ISlotViewData` and a `panelRoot` as input, and passes the `RuntimeItem` from the `ISlotViewData` to the `TooltipController.Show()` method.

*   **`TooltipPanelStyle.uss`:**
    *   The stylesheet that contains the necessary styles (`.tooltip--visible`, `.tooltip--hidden`, etc.) to control the tooltip's appearance and animations.

### 1.2. UI Integration

This system is used by multiple parts of the UI to ensure consistent behavior.

*   **EventController.cs:** The `EventController` now leverages the modular event system to dynamically display and execute event choices. It processes `EventChoiceAction`s, which define the UI and game logic for event outcomes. For more details on the modular event system, refer to the [Encounter System Overview]({{< myrelref "systems/encounters/encounter-system-overview.md" >}}).

*   **`PlayerPanelView.cs`:** The player's inventory and equipment slots are composed of `SlotElement`s, which are populated with `ItemElement`s based on data from the `PlayerPanelDataViewModel`.
*   **`EnemyPanelController.cs`:** The enemy's equipment display dynamically creates `SlotElement`s and binds them to the enemy's `ShipState` data.
*   **`ShopController.cs`:** The shop interface is now a primary consumer of this system. It uses `SlotElement`s to display items for sale. It uses a special `ShopSlotViewModel` to provide price data, and contextual CSS to make the price visible only within the shop. Crucially, `ShopController` now explicitly registers tooltip callbacks for each shop item, ensuring tooltips appear on hover.
*   **`RewardUIController.cs`:** The reward interface utilizes this system for displaying reward items. It uses `SlotElement`s to display items and `RewardItemSlotViewData` to provide item data. It is explicitly called by `RunManager` (for battle rewards) or `DebugConsoleController` (for debug rewards) to display rewards. It integrates with the `GlobalUIService`'s global root for proper layering and event handling, and registers tooltip callbacks for reward items.

## 2. Universal Item Manipulation System

This system centralizes the logic for moving, equipping, and swapping items, decoupling the UI from direct game state manipulation. It interacts with the [runtime item system]({{< myrelref "../core/runtime-data-systems.md" >}}).

### 2.0. Command Pattern for UI Interactions

To further decouple UI components from direct business logic and validation, a Command Pattern has been implemented for all item manipulation actions. This pattern centralizes the logic for validating and executing user requests, making the system more robust, testable, and extensible.

#### Core Components (Command Pattern):

*   **`ICommand` (`PirateRoguelike.Commands.ICommand`):**
    >   File Path: Assets/Scripts/Commands/ICommand.cs
    *   An interface defining the contract for all UI commands, with `CanExecute()` for validation and `Execute()` for performing the action.

*   **`UICommandProcessor` (`PirateRoguelike.Commands.UICommandProcessor`):**
    >   File Path: Assets/Scripts/Commands/UICommandProcessor.cs
    *   A singleton service that acts as the central dispatcher for all UI commands. It receives an `ICommand` object, first calls `command.CanExecute()` to validate the request, and if valid, proceeds to call `command.Execute()`.

*   **Specific Command Implementations:**
    *   **`PurchaseItemCommand` (`PirateRoguelike.Commands.PurchaseItemCommand`):** Handles the logic for purchasing items from the shop.
    *   **`SwapItemCommand` (`PirateRoguelike.Commands.SwapItemCommand`):** Handles the logic for swapping items between inventory and equipment slots.
    *   **`ClaimRewardItemCommand` (`PirateRoguelike.Commands.ClaimRewardItemCommand`):** Handles the logic for claiming reward items.

    These command classes encapsulate all necessary data, validation rules (e.g., gold checks, slot availability, combat state), and the specific calls to underlying services (like `ItemManipulationService`) required to perform their respective actions.

### 2.1. Core Components

*   **`ItemManipulationService` (`PirateRoguelike.Services.ItemManipulationService`):**
    >   File Path: Assets/Scripts/Core/ItemManipulationService.cs
    *   A singleton that acts as the central authority for all item operations. Its `Request...` methods have been removed. It now exposes `Perform...` methods (e.g., `PerformSwap`, `PerformPurchase`, `PerformClaimReward`) that are called by the command objects to execute the core item manipulation logic after validation has occurred. It interacts directly with `GameSession`'s `Inventory` and `PlayerShip` to modify the underlying game state.

*   **`ItemManipulationEvents` (`PirateRoguelike.Events.ItemManipulationEvents`):**
    >   File Path: Assets/Scripts/Core/ItemManipulationEvents.cs
    *   A static event bus that broadcasts notifications when an item manipulation occurs (e.g., `OnItemMoved`, `OnItemAdded`, `OnItemRemoved`).

*   **`SlotManipulator` (`PirateRoguelike.UI.SlotManipulator`):**
    >   File Path: Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs
    *   A `PointerManipulator` attached to each `ItemElement`. It detects drag-and-drop gestures.
    *   **Drag Initiation:** Dragging is now initiated only after the mouse moves beyond a small threshold from the initial click position, allowing for distinct click actions.
    *   **Initiates Commands:** Instead of directly calling `ItemManipulationService`, the `SlotManipulator` now creates and dispatches command objects (e.g., `PurchaseItemCommand`, `SwapItemCommand`, `ClaimRewardItemCommand`) to the `UICommandProcessor`. This significantly decouples the UI input handling from the business logic and validation.
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

*   **`UIInteractionService` (`PirateRoguelike.UI.UIInteractionService`):**
    >   File Path: Assets/Scripts/UI/UIInteractionService.cs
    *   A static class that holds the current global UI state (e.g., `IsInCombat`). It provides methods that the UI can query to ask for permission to perform an action.
    *   The `ItemManipulationService` will only perform actions after they have been approved by the `UIInteractionService`.n    *   This service now allows item manipulation for `SlotContainerType.Shop` when not in combat, enabling shop item purchases.

### 2.2. How it Works (Example: Item Swap with Command Pattern)

1.  A user drags an `ItemElement` from `Slot A` and drops it onto `Slot B`.
2.  The `SlotManipulator` on the dragged `ItemElement` detects the drop and creates a `SwapItemCommand` object, encapsulating the source and target `SlotId`s.
3.  The `SlotManipulator` then sends this `SwapItemCommand` to the `UICommandProcessor.Instance.ProcessCommand()` method.
4.  The `UICommandProcessor` first calls `command.CanExecute()`.
    *   The `SwapItemCommand.CanExecute()` method performs all necessary validation (e.g., checking `UIStateService.IsConsoleOpen`, `UIInteractionService.CanManipulateItem` for both slots).
5.  If `CanExecute()` returns `true`, the `UICommandProcessor` then calls `command.Execute()`.
6.  The `SwapItemCommand.Execute()` method calls `ItemManipulationService.Instance.PerformSwap(Slot A ID, Slot B ID)`.
7.  `ItemManipulationService` performs the logic to swap the `ItemInstance` objects between the source and destination containers (`Inventory` or `ShipState`).
8.  During this process, `Inventory` and/or `ShipState` dispatch `ItemManipulationEvents` (e.g., `OnItemMoved`).
9.  The `PlayerPanelDataViewModel` receives these events and updates the `CurrentItemInstance` property on the affected `SlotDataViewModel` objects in its `ObservableList`s.
10. Because the `SlotElement`s are bound to these view models, their `PropertyChanged` event fires.
11. The `SlotElement` for `Slot A` and `Slot B` detect the change to `CurrentItemInstance` and call their `UpdateItemElement` method, which visually reflects the swap by re-binding, creating, or destroying their child `ItemElement`s.

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
*   `Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs` (Contains `PlayerPanelDataViewModel` and `SlotDataViewModel`)
*   `Assets/Scripts/UI/EnemyPanel/EnemyPanelController.cs`
*   `Assets/Scripts/UI/PlayerPanel/PlayerPanelData.cs` (Contains `ISlotViewData`, `IPlayerPanelData` interfaces etc.)
*   `Assets/Scripts/UI/TooltipController.cs`

### UI - Components & Manipulators
*   `Assets/Scripts/UI/Components/SlotElement.cs` (The slot container)
*   `Assets/Scripts/UI/Components/ItemElement.cs` (The draggable item)
*   `Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs`
*   `Assets/Scripts/UI/EffectDisplay.cs`
*   `Assets/Scripts/UI/Utilities/TooltipUtility.cs`
*   `Assets/Scripts/UI/UIInteractionService.cs`

### New Infrastructure
*   `Assets/Scripts/Core/ServiceLocator.cs`
*   `Assets/Scripts/UI/UIAssetRegistry.cs`
*   `Assets/Scripts/UI/GlobalUIService.cs`

## 5. UI Toolkit Event Handling Notes

*   **`TrickleDown.TrickleDown`:** When registering callbacks for `KeyDownEvent` (and other events where early intervention is critical), using `TrickleDown.TrickleDown` ensures that your callback is executed during the TrickleDown phase of event propagation. This allows you to process or `PreventDefault()` the event before other elements or the default behavior of the target element (e.g., `TextField`) can consume or modify it. This is crucial for achieving precise control over UI element behavior, especially for input fields.

## 7. Editor UI Tools

The project utilizes custom Unity Editor UI tools to streamline content creation and management.

*   **`EncounterEditorWindow` (`Assets/Editor/EncounterEditorWindow.cs`):**
    *   A dedicated editor window for creating and configuring `EncounterSO` assets.
    *   It provides a user-friendly interface for defining encounter properties, including dynamic UI based on `EncounterType`.
    *   Crucially, it supports the modular event system, allowing designers to assign `EventChoiceAction` ScriptableObjects to event choices directly within the editor. For more details on the modular event system, refer to the [Encounter System Overview]({{< myrelref "systems/encounters/encounter-system-overview.md" >}}).

## 6. UI Systems Architecture (PlantUML Sequence Diagram - Purchase Item Flow)

This diagram illustrates the flow of control for a purchase item interaction using the Command Pattern.

```plantuml
@startuml
left to right direction
' --- STYLING ---
skinparam style strictuml
skinparam shadowing true
skinparam defaultFontName "Segoe UI"
skinparam defaultFontSize 16
skinparam linetype ortho
skinparam ArrowFontName Impact
skinparam ArrowThickness 1
skinparam ArrowColor #000000
skinparam backgroundColor #b4b4b42c

skinparam class {
    BackgroundColor WhiteSmoke
    BorderColor #666666
    ArrowColor #1d1d1dff
    !option handWritten true
}
skinparam package {
    BorderColor #555555
    FontColor #333333
    StereotypeFontColor #333333
}

' --- TOP LEVEL: COMMAND PROCESSOR ---
package "Command Processor" <<Service>> #LightCyan {
    class UICommandProcessor {
        + Instance: UICommandProcessor
        + ProcessCommand(command: ICommand)
    }
    interface ICommand {
        + CanExecute(): bool
        + Execute(): void
    }
    UICommandProcessor -> ICommand : processes
}

' --- MIDDLE LAYER: CONCRETE COMMANDS ---
package "Commands" <<Command>> #LightCyan {
    class SwapItemCommand {
        - _fromSlot: SlotId
        - _toSlot: SlotId
        + CanExecute(): bool
        + Execute(): void
    }
    class ClaimRewardItemCommand {
        - _sourceSlot: SlotId
        - _destinationSlot: SlotId
        - _itemToClaim: ItemSO
        + CanExecute(): bool
        + Execute(): void
    }
    class PurchaseItemCommand {
        - _shopSlot: SlotId
        - _playerTargetSlot: SlotId
        - _itemToPurchase: ItemSO
        + CanExecute(): bool
        + Execute(): void
    }
    ICommand <|-- SwapItemCommand
    ICommand <|-- ClaimRewardItemCommand
    ICommand <|-- PurchaseItemCommand
}

' --- SERVICE LAYER ---
package "Services & Managers" #LightCyan {
    package "Core Services" <<Service>> #LightBlue {
        class ItemManipulationService {
            + Instance: ItemManipulationService
            - _gameSession: IGameSession
            + Initialize(gameSession: IGameSession)
            + PerformSwap(slotA: SlotId, slotB: SlotId)
            + PerformPurchase(itemToPurchase: ItemSO, targetFinalSlot: SlotId, shopSlot: SlotId)
            + PerformClaimReward(itemToClaim: ItemSO, targetFinalSlot: SlotId, sourceSlot: SlotId)
        }
        class UIInteractionService {
            + CanManipulateItem(containerType: SlotContainerType): bool
        }
    }
    package "Domain Managers" {
        class ShopManager <<Manager>> #Plum {
            + Instance: ShopManager
            + GetShopItem(index: int): ItemSO
            + RemoveShopItem(index: int)
            + DisplayMessage(message: string)
        }
        class EconomyService <<Service>> #LightBlue {
            + Gold: int
            + CanAfford(amount: int): bool
            + SpendGold(amount: int)
            + AddGold(amount: int)
        }
    }
}

' --- DATA & STATE LAYER ---
package "Data Models & State" #LightCyan {
    package "Game Session" #Yellow {
        interface IGameSession {
            + Economy: EconomyService
            + Inventory: Inventory
            + PlayerShip: ShipState
        }
    }
    package "Data Structures" <<Data>> #LightBlue {
        enum SlotContainerType {
            Inventory
            Equipment
            Shop
            Crafting
        }
        class SlotId {
            + Index: int
            + ContainerType: SlotContainerType
        }
    }
    package "State Containers" <<Data>> #LightBlue {
        class Inventory {
            + AddItemAt(item: ItemInstance, index: int): bool
            + RemoveItemAt(index: int): ItemInstance
            + GetItemAt(index: int): ItemInstance
            + IsSlotOccupied(index: int): bool
            + GetFirstEmptySlot(): int
        }
        class ShipState {
            + SetEquipment(index: int, item: ItemInstance): bool
            + RemoveEquippedAt(index: int): ItemInstance
            + GetEquippedItem(index: int): ItemInstance
            + IsEquipmentSlotOccupied(index: int): bool
            + GetFirstEmptyEquipmentSlot(): int
        }
    }
    package "Item Representation" <<Data>> #LightBlue {
        class ItemInstance {
        }
        class ItemSO {
            + Cost: int
            + displayName: string
        }
    }
}

' --- RELATIONSHIPS BETWEEN LAYERS ---

' Commands -> Services
' Lengthened the arrows to give labels more space
SwapItemCommand --> ItemManipulationService : calls PerformSwap
ClaimRewardItemCommand ---> ItemManipulationService : calls PerformClaimReward
PurchaseItemCommand ----> ItemManipulationService : Core Calls PerformPurchase

' Each command individually checks permission with the UIInteractionService
' Lengthened the dotted arrows significantly
SwapItemCommand ....> UIInteractionService : checks permission
ClaimRewardItemCommand .....> UIInteractionService : checks permission
PurchaseItemCommand ......> UIInteractionService : checks permission

' Purchase Command -> Domain Managers
' Lengthened arrows and added a newline to the label
PurchaseItemCommand ....> ShopManager : interacts with
PurchaseItemCommand .....> EconomyService : "checks/\nspends gold"

' Services -> Data
ItemManipulationService "1" *-- "1" IGameSession : uses
ItemManipulationService ..> SlotId : uses

' GameSession Interface Implementation (Conceptual)
IGameSession ..> EconomyService
IGameSession ..> Inventory
IGameSession ..> ShipState

' Data Structure Relationships
Inventory "1" -- "*" ItemInstance : contains
ShipState "1" -- "*" ItemInstance : equips
ItemInstance "1" -- "1" ItemSO : wraps

@enduml
```