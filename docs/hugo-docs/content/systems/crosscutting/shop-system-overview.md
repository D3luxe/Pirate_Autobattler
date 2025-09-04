---
title: "Shop System"
weight: 25
system: ["ui", "core", "crosscutting"]
types: ["system-overview"]
---

## Overview

The Shop system provides an interface for the player to spend gold to acquire new items and occasionally purchase a new ship. It is designed to be a standard encounter type on the map.

## Design

The shop's architecture follows a clean Model-View-Controller (MVC) pattern and emphasizes component reuse.

*   **Backend (`ShopManager.cs`):** A `MonoBehaviour` that acts as the data provider. It is responsible for generating the inventory of items and the ship for sale based on the player's current progress in the run.
*   **Controller (`ShopController.cs`):** A `MonoBehaviour` that manages the shop's UI. It listens for events from the `ShopManager` and is responsible for creating and binding the UI elements.
*   **Component Reuse:** The shop **does not** use custom UI components for its item slots. Instead, it reuses the universal `SlotElement` and `ItemElement` components that are also used for the player's inventory and equipment panels.
*   **Contextual Styling (CSS):** To display shop-specific information (like price), the system uses contextual CSS styling. The `ShopController` adds a `.shop-container` class to the parent element holding the item slots. A special rule in `ItemElement.uss` makes the price label visible only when an `ItemElement` is inside a `.shop-container`.
*   **Data Bridge (ViewModel):** A custom `ShopSlotViewModel` is used. It implements the standard `ISlotViewData` interface required by `SlotElement`, but extends it with shop-specific properties like `Price`.

## Implementation Details

### Core Components

*   **`ShopManager` (`Assets/Scripts/Encounters/ShopManager.cs`):**
    *   Generates a list of `ItemSO`s and a `ShipSO` in its `Start()` method.
    *   After all data is prepared, it fires a single `OnShopDataUpdated` event.

*   **`ShopController` (`Assets/Scripts/UI/ShopController.cs`):**
    *   Subscribes to `OnShopDataUpdated`.
    *   When the event is received, it clears the item container and loops through the data from the `ShopManager`.
    *   For each item, it creates a `ShopSlotViewModel`, then creates a generic `SlotElement` and binds it to the view model.

*   **`ShopSlotViewModel` (`Assets/Scripts/UI/Shop/ShopSlotViewModel.cs`):**
    *   The data object that bridges the gap between the shop's data needs and the generic UI components.
    *   Provides `Price` and `IsPurchasable` properties.

*   **`ItemElement` (`Assets/Scripts/UI/Components/ItemElement.cs`):**
    *   The generic UI component for displaying an item icon.
    *   It now contains a `price-label` element in its UXML.
    *   Its `Bind()` method checks if the provided `ISlotViewData` is a `ShopSlotViewModel`. If it is, it displays the price; otherwise, the price remains blank.

*   **`SlotManipulator` (`Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs`):**
    *   This universal manipulator handles item interaction logic, including purchases from the shop. It differentiates between a quick click and a drag-and-drop gesture.
    *   **Click-to-Buy:** If a user clicks (mouse down and up without significant movement) on a shop item, the `SlotManipulator` directly requests a purchase to an available inventory slot via `ItemManipulationService`.
    *   **Drag-and-Drop Purchase:** If a user drags a shop item, the drag operation is initiated only after the mouse moves beyond a small threshold. On mouse release, if the item is dropped onto a valid inventory or equipment slot, a purchase is requested. If dropped elsewhere, the purchase is cancelled, and the item visually returns to the shop.
    *   It detects when a drag originates from a slot within a container of type `Shop` and correctly interprets the action as a purchase request to the `ItemManipulationService`.

## Related Documents

*   [UI Systems Overview]({{< myrelref "../ui/ui-systems.md" >}})
*   [Core Systems Overview]({{< myrelref "../core/_index.md" >}})

## Process Flowchart

This diagram outlines the entire process flow for the shop system, from encounter initialization to item purchase.

```plantuml
@startuml
' --- STYLING (Activity Beta syntaxing) ---
skinparam style strictuml
skinparam shadowing true
skinparam defaultFontName "Segoe UI"
skinparam defaultFontSize 16
skinparam backgroundColor #b4b4b42c
!option handWritten true
skinparam activity {
    BorderColor #A9A9A9
    BorderThickness 1.5
    ArrowColor #555555
    ArrowThickness 1.5
}
skinparam note {
    BackgroundColor #FFFFE0
    BorderColor #B4B4B4
}

' --- DIAGRAM LOGIC (Using Partition) ---
start

partition "Game Progression" #E3F2FD {
  :Player clicks Shop node on Map;
  if (Node type is Shop?) then (Yes)
    :Load EncounterSO;
    :GameSession.CurrentRunState.NextShopItemCount = EncounterSO.shopItemCount;
    :SceneManager.LoadScene("Shop");
  else
    :end;
    stop
  endif
}

' This partition represents the backend process that starts after the scene loads
partition "Backend - Data Generation" #E8F5E9 {
    :Shop Scene Loads;
    :ShopManager.Start;
    :Reads NextShopItemCount from GameSession;
    :GenerateShopItems();
    :Get all items from GameDataRegistry;
    :Selects items based on rarity;
    :Fires OnShopDataUpdated event;
}
note right: This event triggers the Frontend UI update

' This partition represents the frontend work, which logically follows the backend event
partition "Frontend - UI Rendering" #FFF3E0 {
    :ShopController receives event and updates UI;
    repeat
        :new ShopSlotViewModel;
        :new SlotElement;
        :slotElement.Bind(viewModel);
        :container.Add(slotElement);
    repeat while (more items?) is (yes)
    -> UI is rendered and ready for interaction;
}

' The final stage of the user journey
partition "Purchase Flow" #F3E5F5 {
  :User interacts with ItemElement from Shop;
  if (Mouse moved beyond threshold?) then (Yes)
    :Start Drag;
    :Ghost icon follows mouse;
    :User releases mouse;
    if (Dropped over Inventory/Equipment?) then (Yes)
      :Call ItemManipulationService.RequestPurchase (from Shop to Target Slot);
    else (No)
      :Cancel Purchase;
      :Item returns to Shop;
    endif
  else (No - it's a click)
    :Call ItemManipulationService.RequestPurchase (from Shop to any Inventory Slot);
  endif
  :ItemManipulationService processes request;
  if (Purchase successful?) then (Yes)
    :GameSession.Inventory.AddItem;
    :GameSession.Economy.SpendGold;
  else (No)
    :Display "Not enough gold!" or similar message;
  endif
}

stop
@enduml
```