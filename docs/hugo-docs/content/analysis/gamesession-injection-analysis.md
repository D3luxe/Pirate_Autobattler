---
title: "GameSession Injection Analysis"
weight: 10
system: ["core", "ui"]
types: ["analysis", "refactoring","system-overview"]
status: "archived"
discipline: ["engineering"]
stage: ["production"]
---

# GameSession Injection Analysis: Medium Effort Approach

## Problem/Topic

Improve the testability of `PlayerPanelDataViewModel` by introducing an interface for `GameSession`, without immediately undertaking the larger task of refactoring `GameSession` itself from a static class.

## Analysis

### Core Idea

Make `PlayerPanelDataViewModel` depend on an **abstraction** (`IGameSession`) rather than a **concrete implementation** (`GameSession`).

### Steps Involved:

### Step 1: Define an `IGameSession` Interface

Create a new C# interface that declares the properties and methods of `GameSession` that `PlayerPanelDataViewModel` currently uses.

**File:** `Assets/Scripts/Core/IGameSession.cs` (or a suitable `Interfaces` folder)

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

**Effort for Step 1:** Low. This involves creating a new file and copying/pasting the relevant signatures from `GameSession`. You'll also need to create interfaces for `PlayerShip` and `Inventory` if they are complex objects with their own events/properties that the ViewModel directly interacts with.

### Step 2: Make `GameSession` Implement `IGameSession`

Modify the existing `GameSession` class to implement the `IGameSession` interface. Since `GameSession` is currently static, this step is a bit unusual. You can't directly make a static class implement an interface in the traditional sense.

**Option A (Still Static, but with an Interface-like Accessor - Recommended for Medium Effort):**
Create a non-static wrapper class that implements `IGameSession` and accesses the static `GameSession` members.

```csharp
// GameSessionWrapper.cs (New class to implement IGameSession)
public class GameSessionWrapper : IGameSession
{
    public IPlayerShip PlayerShip => GameSession.PlayerShip; // Access static GameSession
    public IInventory Inventory => GameSession.Inventory;   // Access static GameSession

    public int Gold => GameSession.Gold;
    public int Lives => GameSession.Lives;
    public int CurrentDepth => GameSession.CurrentDepth;
}
```
This approach allows you to pass `new GameSessionWrapper()` where an `IGameSession` is expected.

**Effort for Step 2:** Medium. This involves creating a new wrapper class and mapping the static `GameSession` members to the interface members.

### Step 3: Modify `PlayerPanelDataViewModel` to Depend on `IGameSession`

This is where the dependency injection happens.

```csharp
// PlayerPanelDataViewModel.cs
public class PlayerPanelDataViewModel : IPlayerPanelData, IShipViewData, IHudViewData, System.ComponentModel.INotifyPropertyChanged
{
    private readonly IGameSession _gameSession; // Depend on the interface

    public PlayerPanelDataViewModel(IGameSession gameSession) // Constructor injection
    {
        _gameSession = gameSession;
    }

    // Update all property accessors and event subscriptions
    public string ShipName => _gameSession.PlayerShip.Def.displayName;
    public int Gold => _gameSession.Gold;
    // ... other properties

    public void Initialize()
    {
        _gameSession.PlayerShip.OnEquipmentChanged += UpdateEquipmentSlots;
        _gameSession.Inventory.OnInventoryChanged += UpdateInventorySlots;
        // ...
    }
}
```

**Effort for Step 3:** Low to Medium. This involves changing the constructor and updating all references within the `PlayerPanelDataViewModel` class. The complexity depends on how many references there are.

### Step 4: Update `PlayerPanelController` to Pass `IGameSession`

Finally, `PlayerPanelController` needs to provide the `IGameSession` instance when it creates `PlayerPanelDataViewModel`.

```csharp
// PlayerPanelController.cs
public class PlayerPanelController : MonoBehaviour
{
    private PlayerPanelDataViewModel _viewModel;
    private PlayerPanelView _panelView;

    private void Awake()
    {
        // Using the GameSessionWrapper from Step 2, Option A:
        _viewModel = new PlayerPanelDataViewModel(new GameSessionWrapper());

        _panelView = new PlayerPanelView(GetComponent<UIDocument>().rootVisualElement);
        _viewModel.Initialize();
        _panelView.BindInitialData(_viewModel);
        // ...
    }
    // ...
}
```

**Effort for Step 4:** Low. This is a single line change in `PlayerPanelController`.

### Benefits of this Medium Effort Approach:

*   **Improved Testability of `PlayerPanelDataViewModel`:** You can now easily create mock or stub implementations of `IGameSession` for unit testing `PlayerPanelDataViewModel`. This means you can test the ViewModel's logic without needing a running Unity environment or the actual `GameSession` data.
*   **Clearer Dependencies:** The dependency on `GameSession` (via `IGameSession`) is now explicit in the ViewModel's constructor.
*   **Preparation for Future Refactoring:** This sets the stage for the "High Effort" refactoring of `GameSession` itself. When `GameSession` eventually becomes a non-static class, the `PlayerPanelDataViewModel` will already be expecting an `IGameSession` instance, minimizing further changes to the ViewModel.

## Conclusion/Recommendations

This approach provides a significant step towards better architecture and testability for the UI layer, even with the current static `GameSession`.