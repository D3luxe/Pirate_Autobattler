# UI Interaction System

This document describes the architecture of core UI systems that are designed to be universal, context-aware, and robust. This includes the Tooltip system and the Item Manipulation rule service.

## 1. Universal Tooltip System

The tooltip system is designed to function correctly on any item slot within any UI panel or `UIDocument`, regardless of the scene it belongs to.

### 1.1. Core Components

*   **`TooltipController.cs`:** A persistent singleton (`MonoBehaviour`) that orchestrates all tooltip activity.
*   **`TooltipUtility.cs`:** A static helper class that provides a simple method (`RegisterTooltipCallbacks`) for registering the necessary `PointerEnterEvent` and `PointerLeaveEvent` on a UI element.
*   **`TooltipPanelStyle.uss`:** The stylesheet that contains the necessary styles (`.tooltip--visible`, `.tooltip--hidden`, etc.) to control the tooltip's appearance and animations.

### 1.2. Architecture

The primary challenge is that different UI panels (e.g., the player's persistent panel vs. the battle scene's enemy panel) exist in different `UIDocument` instances with different style collections. To solve this, the `TooltipController` is designed to be context-aware.

1.  **Instance Management:** The `TooltipController` maintains a `Dictionary<VisualElement, VisualElement>`. It stores a unique tooltip UI element instance for each `panelRoot` it interacts with.
2.  **On-Demand Instantiation:** When a tooltip needs to be shown on a panel for the first time, the `TooltipController` instantiates a new tooltip from a `VisualTreeAsset` and adds it to that panel's visual tree. This new instance is then cached in the dictionary.
3.  **Stylesheet Injection:** Crucially, when creating a new tooltip instance for a panel, the `TooltipController` also ensures that its required stylesheet (`TooltipPanelStyle.uss`) is added to the panel's `styleSheets` list. This guarantees the tooltip will always render correctly, regardless of the panel's default styles.
4.  **Callback Registration:** UI controllers like `PlayerPanelView` and `EnemyPanelController` use `TooltipUtility.RegisterTooltipCallbacks`, passing in their root visual element. This ensures the `TooltipController` receives the correct panel context when a `Show` event is triggered.

## 2. Context-Aware Item Manipulation

To ensure item manipulation (like dragging and dropping) is safe and respects gameplay rules, the logic is not handled by the UI directly. Instead, the UI queries a central service that acts as a gatekeeper.

### 2.1. Core Components

*   **`UIInteractionService.cs`:** A static class that holds the current global UI state (e.g., `IsInCombat`). It provides methods that the UI can query to ask for permission to perform an action.
*   **`ItemManipulationService.cs`:** A service that contains the actual logic for modifying game state (swapping items in inventory, etc.). It will only perform actions after they have been approved by the `UIInteractionService`.
*   **`SlotManipulator.cs`:** The UI component that detects drag-and-drop gestures. It does not contain any game logic itself.

### 2.2. Architecture (Example: Drag-and-Drop)

1.  **State Management:** At the start of a battle, `CombatController.cs` sets a global flag: `UIInteractionService.IsInCombat = true;`. It sets it to `false` when the battle ends.
2.  **Permission Check:** The user attempts to drag an item. The `SlotManipulator` on the item catches the `PointerDownEvent`.
3.  Before starting the drag, it asks the gatekeeper for permission: `UIInteractionService.CanManipulateItem(...)`.
4.  The `UIInteractionService` checks its state. If `IsInCombat` is `true`, it returns `false`. If the item is not one the player owns (e.g., an enemy or shop item), it returns `false`.
5.  **Action Execution:** If permission is denied, the `SlotManipulator` does nothing. If permission is granted, the drag proceeds. When the item is dropped, the `SlotManipulator` calls a method on the `ItemManipulationService` (e.g., `RequestSwap()`) to perform the action.

This architecture keeps game rules decoupled from the UI, prevents illegal actions, and creates a secure foundation for adding more complex interactions like shops.