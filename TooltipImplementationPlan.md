# Item Tooltip Implementation Plan

This document outlines the plan to implement a mouseover tooltip for items within the game's UI.

## 1. Objective

Create a tooltip system that displays detailed information about an item when the user hovers their mouse over it. The implementation will be exclusively using **UI Toolkit**. All legacy UGUI elements, such as `InventorySlotUI`, will be ignored.

## 2. Scope

The initial implementation will focus on adding tooltip functionality to the `ShopItemViewUI` element, which is confirmed to be a UI Toolkit asset.

## 3. Core Components

-   **`TooltipPanel.uxml`**: The existing UXML file that defines the visual structure of the tooltip.
-   **`ShopItemViewUI.cs`**: The target UI Toolkit component where the tooltip will be triggered.
-   **`ItemSO.cs`**: The ScriptableObject containing the data to be displayed in the tooltip.

## 4. Implementation Steps

### Step 1: Create `TooltipController.cs`

A new `MonoBehaviour` script will be created to manage the tooltip's state and content.

-   **Responsibilities:**
    -   Hold a reference to the tooltip's `UIDocument`.
    -   Expose a public `Show(ItemSO item, VisualElement targetElement)` method to display and position the tooltip.
    -   Expose a public `Hide()` method to conceal the tooltip.
    -   Dynamically populate the tooltip's UXML fields with data from the provided `ItemSO`.
    -   Manage the fade-in/out animation by toggling USS classes.
    -   Position the tooltip adjacent to the `targetElement`.

### Step 2: Create `EffectDisplay.cs`

A small, reusable controller class will be created to manage the individual effect descriptions within the tooltip.

-   **Responsibilities:**
    -   Take an `AbilitySO` as input.
    -   Populate the icon and description label of an `#ActiveEffect` or `#PassiveEffect` element.

### Step 3: Modify `ShopItemViewUI.cs`

The existing `ShopItemViewUI.cs` script will be updated to trigger the tooltip.

-   **Changes:**
    -   Register `PointerEnterEvent` and `PointerLeaveEvent` callbacks on the root visual element.
    -   The `PointerEnterEvent` callback will invoke `TooltipController.Show()`, passing the item's data and a reference to itself for positioning.
    -   The `PointerLeaveEvent` callback will invoke `TooltipController.Hide()`.

### Step 4: Create the Tooltip Prefab

A new prefab will be created to ensure the tooltip is available in the scene.

-   **Contents:**
    -   A `GameObject` containing a `UIDocument` component linked to `TooltipPanel.uxml`.
    -   The `TooltipController.cs` script will be attached to this `GameObject`.

### Step 5: Create/Update Stylesheet (`.uss`)

A USS file will be used to control the tooltip's animation.

-   **Styles:**
    -   A `.tooltip--hidden` class with `opacity: 0;`.
    -   A `.tooltip--visible` class with `opacity: 1;`.
    -   A `transition` property on the tooltip's root element to enable a smooth fade between the hidden and visible states.
