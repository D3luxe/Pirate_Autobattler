---
title: "UI ViewModel Refactor Task"
weight: 10
system: ["ui", "core"]
types: ["task", "plan", "refactoring", "performance", "bug-fix"]
tags: ["ViewModel", "ObservableList", "INotifyPropertyChanged", "PlayerPanelDataViewModel", "PlayerPanelView", "EnemyPanelController", "ShipDisplayElement", "SlotElement", "Tooltip"]
---

### UI Viewmodel Refactor Plan

This plan outlines the steps to transition the UI to a more robust "bind" model, improve performance, and fix identified bugs.

#### Phase 1: Foundation - Implement Observable Collections & Fix Component Bugs

1.  **Implement `ObservableList<T>`:**
    *   Create a generic `ObservableList<T>` class that implements `IList<T>` and raises `CollectionChanged` events (e.g., `NotifyCollectionChangedAction.Add`, `Remove`, `Replace`, `Move`, `Reset`) when its contents change. This class should also implement `INotifyPropertyChanged` for the `Count` and `Item[]` properties.
    *   Ensure `ObservableList<T>` is compatible with `System.ComponentModel.INotifyPropertyChanged` for individual item changes within the list.
2.  **Update ViewModels to Use `ObservableList`:**
    *   Modify `PlayerPanelDataViewModel` to use `ObservableList<ISlotViewData>` for its `EquipmentSlots` and `InventorySlots` properties.
    *   Ensure that when the underlying `GameSession.PlayerShip.Equipped` or `GameSession.Inventory.Items` collections change, these `ObservableList` instances are correctly updated (e.g., by adding/removing/replacing items in the `ObservableList` rather than creating a new `List`).
3.  **Fix Missing Element Queries in Custom Components:**
    *   In `ShipDisplayElement.cs`, add the query for `_shipHpLabel` in the constructor's `QueryElements` method.
    *   In `SlotElement.cs`, add the queries for `_cooldownBar` and `_disabledOverlay` in the constructor's `QueryElements` method.

#### Phase 2: Refactor UI to Leverage Observable Collections

1.  **Refactor `PlayerPanelView.PopulateSlots`:**
    *   Modify `PlayerPanelView.PopulateSlots` (and potentially rename it or create a new method) to no longer clear and re-create all `SlotElement`s.
    *   Instead, subscribe to the `CollectionChanged` event of the `ObservableList<ISlotViewData>` passed to it.
    *   Implement logic to add, remove, or update individual `SlotElement`s in the UI based on the `CollectionChanged` event arguments (e.g., `NotifyCollectionChangedAction.Add` adds a new `SlotElement`, `Remove` removes one, `Replace` updates an existing one).
2.  **Refactor `EnemyPanelController.PopulateEquipmentSlots`:**
    *   Apply similar refactoring as in Step 2.1 to `EnemyPanelController.PopulateEquipmentSlots` to leverage the `ObservableList` for efficient updates.
3.  **Remove Explicit Collection Updates from Controllers:**
    *   In `PlayerPanelController.cs`, remove the explicit calls to `_panelView.UpdateEquipment()` and `_panelView.UpdatePlayerInventory()` from `HandleEquipmentChanged()` and `HandleInventoryChanged()`. The UI updates will now be driven by the `ObservableList` events.
    *   In `EnemyPanelController.cs`, remove the explicit call to `PopulateEquipmentSlots()` from `HandleEnemyEquipmentChanged()`.

#### Phase 3: General UI Refinements

1.  **Refactor Duplicated Tooltip Logic:**
    *   Create a reusable utility class or a base method that encapsulates the `PointerEnterEvent` and `PointerLeaveEvent` registration for tooltip display, reducing code duplication in `PlayerPanelView.PopulateSlots` and `EnemyPanelController.PopulateEquipmentSlots`.
