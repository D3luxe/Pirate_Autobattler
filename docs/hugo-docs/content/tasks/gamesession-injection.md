---
title: "GameSession Injection Task"
weight: 10
system: ["core", "ui"]
types: ["task", "plan", "refactoring", "dependency-injection"]
tags: ["GameSession", "PlayerPanelDataViewModel", "IGameSession", "GameSessionWrapper", "PlayerPanelController"]
stage: ["Blocked"] # e.g., "Planned", "InProgress", "Completed", "Blocked"
---

## Plan: GameSession Injection (Medium Effort Approach - Corrected)

This plan outlines the steps to implement the `GameSession` injection into `PlayerPanelDataViewModel` using the medium effort approach. This approach focuses on improving the testability and modularity of the `PlayerPanelDataViewModel` by introducing an `IGameSession` interface, without immediately undertaking the larger task of refactoring `GameSession` itself from a static class.

### Trace and Verify (Corrected Summary):

*   **`PlayerPanelDataViewModel` Location:** `Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs` (nested class, starts on line 12).
*   **`PlayerPanelDataViewModel`'s `GameSession` usage (within `PlayerPanelController.cs`):
    *   `_gameSession.Gold` (line 28)
    *   `_gameSession.Lives` (line 29)
    *   `_gameSession.CurrentDepth` (line 30)
    *   `_gameSession.PlayerShip.Def.displayName` (line 34)
    *   `_gameSession.PlayerShip.Def.art` (line 35)
    *   `_gameSession.PlayerShip.Def.baseMaxHealth` (line 50)
    *   `_gameSession.PlayerShip.OnEquipmentChanged` (line 114)
    *   `_gameSession.Inventory.OnInventoryChanged` (line 115)
*   **`PlayerPanelDataViewModel` instantiation in `PlayerPanelController.cs`:**
    *   `private PlayerPanelDataViewModel _viewModel = new PlayerPanelDataViewModel(new PirateRoguelike.Core.GameSessionWrapper());` (line 228)
*   **Note on `PlayerPanelController`'s direct `GameSession` dependencies:** This plan *does not* address the direct static `GameSession` dependencies that remain within `PlayerPanelController` itself (e.g., event subscriptions, item manipulation logic). This plan is scoped to injecting `IGameSession` into `PlayerPanelDataViewModel` only.

### Step-by-Step Implementation Plan:

1.  **Create `IGameSession.cs`:**
    *   **Action:** Create a new C# interface file `Assets/Scripts/Core/IGameSession.cs`.
    *   **Content:** Define the `IGameSession` interface, including `IPlayerShip` and `IInventory` interfaces, with properties and events currently accessed by `PlayerPanelDataViewModel`.
        ```csharp
        // IGameSession.cs
        using System;
        using PirateRoguelike.Data; // Required for ShipSO and ItemInstance

        public interface IGameSession
        {
            IPlayerShip PlayerShip { get; }
            IInventory Inventory { get; }
            int Gold { get; }
            int Lives { get; }
            int CurrentDepth { get; }
        }

        public interface IPlayerShip
        {
            ShipSO Def { get; }
            event Action OnEquipmentChanged;
            // Add other properties/methods as needed if PlayerPanelDataViewModel uses them
        }

        public interface IInventory
        {
            event Action OnInventoryChanged;
            ItemInstance[] Items { get; } // PlayerPanelDataViewModel uses Inventory.Items
            // Add other properties/methods as needed if PlayerPanelDataViewModel uses them
        }
        ```
    *   **Rationale:** To provide an abstraction for `GameSession` that the ViewModel can depend on.

2.  **Create `GameSessionWrapper.cs`:**
    *   **Action:** Create a new C# class file `Assets/Scripts/Core/GameSessionWrapper.cs`.
    *   **Content:** Implement `IGameSession` in `GameSessionWrapper` and have its properties and methods access the static `GameSession` members. This wrapper will act as an adapter to bridge the static `GameSession` to the `IGameSession` interface.
    *   **Rationale:** To allow the static `GameSession` to be "wrapped" and passed as an `IGameSession` instance.

3.  **Modify `PlayerPanelController.cs` (specifically the nested `PlayerPanelDataViewModel` class):
    *   **Action:** Open `Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs`.
    *   **Changes (within the `PlayerPanelDataViewModel` class, starting line 12):
        *   Ensure the constructor `public PlayerPanelDataViewModel(IGameSession gameSession)` exists and correctly assigns `_gameSession`.
        *   Verify all references to `GameSession.StaticProperty` (e.g., `GameSession.Gold`, `GameSession.PlayerShip.Def.displayName`) are updated to use the injected `_gameSession.Property`.
        *   Verify event subscriptions (e.g., `GameSession.PlayerShip.OnEquipmentChanged`, `GameSession.Inventory.OnInventoryChanged`) are updated to use the injected `_gameSession.PlayerShip.OnEquipmentChanged` and `_gameSession.Inventory.OnInventoryChanged`.
    *   **Rationale:** To decouple the ViewModel from the static `GameSession` and enable dependency injection.

4.  **Modify `PlayerPanelController.cs`:**
    *   **Action:** Open `Assets/Scripts/UI/PlayerPanel/PlayerPanelController.cs`.
    *   **Changes:**
        *   Modify the instantiation of `PlayerPanelDataViewModel` at line `228` to pass a new instance of `GameSessionWrapper` to its constructor: `private PlayerPanelDataViewModel _viewModel = new PlayerPanelDataViewModel(new PirateRoguelike.Core.GameSessionWrapper());`
    *   **Rationale:** To provide the `IGameSession` implementation to the ViewModel.
