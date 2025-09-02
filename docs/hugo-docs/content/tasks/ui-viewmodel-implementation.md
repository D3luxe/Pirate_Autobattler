---
title: "UI ViewModel Implementation Task"
weight: 10
system: ["ui", "core"]
types: ["task", "plan", "implementation", "refactoring", "architecture"]
tags: ["ViewModel", "INotifyPropertyChanged", "ObservableList", "UI Toolkit", "PlayerPanelDataViewModel", "EnemyShipViewData", "SlotDataViewModel", "BindingUtility", "UXML"]
stage: ["Completed"] # e.g., "Planned", "InProgress", "Completed", "Blocked"
---

**Revised Plan for UI ViewModel Implementation**

This plan outlines a phased approach to implementing a robust viewmodel layer for the UI, incorporating insights from the complete analysis regarding abstraction levels, ViewModel differences, and standardization. The goal is to move from a "push" model to a "bind" model where UI elements automatically update when viewmodel properties change.

**Phase 1: Minimal Abstraction - Making Existing UI Toolkit ViewModels Observable**

*   **Goal:** Implement `INotifyPropertyChanged` on existing viewmodel interfaces and concrete classes to enable basic observability.
*   **Steps:**
    1.  **Define `INotifyPropertyChanged` Interface:** (Already completed) Ensure `Assets/Scripts/Shared/Interfaces/INotifyPropertyChanged.cs` exists with the standard `System.ComponentModel.INotifyPropertyChanged` interface.
    2.  **Modify ViewModel Interfaces:** Update `IShipViewData`, `IHudViewData`, and `ISlotViewData` to inherit from `INotifyPropertyChanged`.
    3.  **Implement `INotifyPropertyChanged` in Concrete ViewModels:**
        *   `PlayerPanelDataViewModel`
        *   `EnemyShipViewData`
        *   `SlotDataViewModel`
        *   For each property in these viewmodels, add a backing field and implement the `set` accessor to raise `PropertyChanged` events when the value changes.
    4.  **Refactor Controllers to Update ViewModels:** Modify `PlayerPanelController` and `EnemyPanelController` to update the viewmodel properties directly in their event handlers, rather than calling explicit `UpdateX` methods on the `_panelView`.

**Phase 2: Intermediate Abstraction - Implementing Custom Binding Utilities and Reusable UI Elements (As Needed)**

*   **Goal:** Develop helper methods or a simple binding system to reduce boilerplate in views, and create reusable UI Toolkit Visual Elements for common components like ship displays and item slots.
*   **Steps:** (These steps will be detailed and planned *after* Phase 1 is complete and verified, and if the need for further abstraction is confirmed.)
    1.  **Develop a `BindingUtility` class:** Create static methods for common binding patterns (e.g., `BindLabelText`, `BindImageSprite`, `BindVisibility`).
    2.  **Implement `ObservableList` or `ObservableCollection`:** Create a custom observable collection that notifies when items are added, removed, or changed, to facilitate dynamic list rendering.
    3.  **Create Reusable UI Toolkit Visual Elements:** Design and implement generic `ShipDisplayElement` (UXML and C#) that binds to `IShipViewData`, and `SlotElement` (UXML and C#) that binds to `ISlotViewData` and handles common interactions.
    4.  **Refactor Views to Use Binding Utilities and Reusable Elements:** Update `PlayerPanelView`, `EnemyPanelView`, and potentially `MapView` and `ShopUI` to use these utilities and reusable elements for binding.

**Phase 3: Migrate Legacy Unity UI to UI Toolkit (As Needed)**

*   **Goal:** Convert `ShopItemView` (and potentially `ShipView`) from legacy Unity UI to UI Toolkit to unify the UI framework.
*   **Steps:** (These steps will be detailed and planned *after* Phase 1 and 2 are complete and verified, and if the need for unification is confirmed.)
    1.  **Recreate UXML:** Design `ShopItemView.uxml` and `ShipView.uxml` using UI Toolkit.
    2.  **Rewrite C# Views:** Create new C# scripts that load the UI Toolkit UXML and bind to viewmodels.
