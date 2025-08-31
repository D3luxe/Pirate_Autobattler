# Item Tooltip Implementation Plan

This document outlines the plan to implement a mouseover tooltip for items within the game's UI.

## 1. Objective

Create a tooltip system that displays detailed information about an item when the user hovers their mouse over it. The implementation will be exclusively using **UI Toolkit**. All legacy UGUI elements, such as `InventorySlotUI`, will be ignored.

## 2. Scope

The initial implementation will focus on adding tooltip functionality to the `ShopItemViewUI` element, which is confirmed to be a UI Toolkit asset. The system was later extended to also support item slots in the Player Panel (inventory and equipment).

## 3. Core Components

-   **`TooltipPanel.uxml`**: The UXML file that defines the visual structure of the tooltip.
-   **`ShopItemViewUI.cs`**: A target UI Toolkit component where the tooltip is triggered.
-   **`PlayerPanelView.cs`**: The script responsible for populating item slots in the Player Panel, where tooltip triggering was also integrated.
-   **`ItemSO.cs`**: The ScriptableObject containing the data to be displayed in the tooltip.
-   **`AbilitySO.cs` and `ActionSO.cs`**: Data structures crucial for populating the tooltip's effect descriptions.

## 4. Implementation Steps

### Step 1: Create `TooltipController.cs`

A `MonoBehaviour` script was created to manage the tooltip's state and content.

-   **Responsibilities:**
    -   Acts as a singleton (`TooltipController.Instance`).
    -   Instantiates the tooltip from `TooltipPanel.uxml` and adds it to a global UI `rootVisualElement` to ensure correct z-ordering across all UI elements.
    -   Exposes a public `Show(ItemSO item, VisualElement targetElement)` method to display and position the tooltip.
    -   Exposes a public `Hide()` method to conceal the tooltip.
    -   Dynamically populates the tooltip's UXML fields with data from the provided `ItemSO`.
    -   Manages the fade-in/out animation by toggling USS classes.
    -   Positions the tooltip adjacent to the `targetElement`.
    -   Ensures proper initialization order by being initialized in `RunManager.Awake()`.

### Step 2: Create `EffectDisplay.cs`

A small, reusable controller class was created to manage the individual effect descriptions within the tooltip.

-   **Responsibilities:**
    -   Takes an `AbilitySO` as input.
    -   Populates the icon and description label of an `#ActiveEffect` or `#PassiveEffect` element.

### Step 3: Modify `ShopItemViewUI.cs`

The `ShopItemViewUI.cs` script was updated to trigger the tooltip.

-   **Changes:**
    -   Registers `PointerEnterEvent` and `PointerLeaveEvent` callbacks on the root visual element.
    -   The `PointerEnterEvent` callback invokes `TooltipController.Show()`, passing the item's data and a reference to itself for positioning.
    -   The `PointerLeaveEvent` callback invokes `TooltipController.Hide()`.

### Step 4: Modify `PlayerPanelView.cs`

The `PlayerPanelView.cs` script was updated to trigger the tooltip for inventory and equipment slots.

-   **Changes:**
    -   `ISlotViewData` was extended to include `ItemSO ItemData`.
    -   `SlotDataViewModel` and `MockSlotViewData` were updated to implement `ItemData`.
    -   In the `PopulateSlots` method, `PointerEnterEvent` and `PointerLeaveEvent` callbacks are registered on the `slotElement`s.
    -   These callbacks invoke `TooltipController.Show()` and `Hide()`, passing the `ItemSO` from `slotData.ItemData`.

### Step 5: Create Tooltip Prefab and Integrate with `RunManager`

A `TooltipManager` prefab was created and integrated into the `RunManager` for proper lifecycle management. Additionally, a `GlobalUIOverlay` prefab was introduced to manage the global UI layer for elements like tooltips.

-   **`TooltipManager` Prefab Contents:**
    -   A `GameObject` with the `TooltipController.cs` script attached.
    -   The `TooltipPanel.uxml`, `ActiveEffect.uxml`, and `PassiveEffect.uxml` assets are assigned to the `TooltipController`'s serialized fields.
    -   **Note:** The `UIDocument` component was removed from this GameObject, as the tooltip is now added to a global `UIDocument`.

-   **`GlobalUIOverlay` Prefab Contents:**
    -   A `GameObject` with a `UIDocument` component attached.
    -   This `UIDocument` uses a simple UXML asset (e.g., `GlobalUIOverlay.uxml`) and serves as the root for all global UI elements, ensuring they render on top.

-   **`RunManager.cs` Integration:**
    -   Instantiates the `TooltipManager` prefab in `Awake()`.
    -   Instantiates the `GlobalUIOverlay` prefab in `Awake()`.
    -   Initializes the `TooltipController` by passing the `GlobalUIOverlay`'s `UIDocument`'s `rootVisualElement` to its `Initialize()` method, ensuring correct z-ordering and initialization timing.

### Step 6: Create `ActiveEffect.uxml` and `PassiveEffect.uxml`

The individual effect display elements were extracted into their own UXML files for reusability.

### Step 7: Update Stylesheet (`.uss`)

The `TooltipPanelStyle.uss` was updated to control the tooltip's animation and initial visibility.

-   **Styles:**
    -   `.tooltip--hidden` class with `opacity: 0;` and `display: none;`.
    -   `.tooltip--visible` class with `opacity: 1;`.
    -   `transition` property on the `.tooltip` class to enable a smooth fade between the hidden and visible states.

## 5. Conclusion

The implementation of the mouseover tooltip system using UI Toolkit has been successfully completed. The tooltip now correctly displays item information, positions itself accurately, and handles visibility and z-ordering across different UI contexts (Shop and Player Panel). The system is robust and adheres to the project's UI Toolkit conventions.