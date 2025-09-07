---
title: "Command Pattern Refactor Plan for UI Interactions"
date: 2025-09-07T18:14:00Z
description: "A detailed plan for refactoring UI interactions using the Command Pattern to improve modularity and testability."
draft: false
tags: ["Architecture", "UI", "Refactoring", "Command Pattern"]
system: ["ui", "core"]
types: ["analysis", "plan"]
---

## 1. Analysis

The current `SlotManipulator` directly orchestrates complex UI interactions, including differentiating between clicks and drags, performing validation checks (e.g., `UIStateService.IsConsoleOpen`, `UIInteractionService.CanManipulateItem`), and directly invoking methods on `ItemManipulationService` based on the interaction type and target. This tightly couples the UI component to the game's business logic and validation rules.

The `ItemManipulationService` currently duplicates some validation logic (e.g., `UIInteractionService.CanManipulateItem` checks, gold checks, slot availability checks) and contains complex slot-finding algorithms.

The `UIInteractionService` provides a simple, global check for item manipulation permissions.

## 2. Plan

The refactoring will introduce a Command Pattern to formalize user interaction flow, centralizing validation and business logic within command objects and a `UICommandProcessor`.

### New Components:

*   **`ICommand` (Interface):** Defines `CanExecute()` and `Execute()` methods.
*   **`UICommandProcessor` (Singleton Service):** Receives `ICommand` objects, validates them via `CanExecute()`, and executes them via `Execute()`.
*   **Specific Command Classes:**
    *   `PurchaseItemCommand`: Handles purchasing items from the shop.
    *   `SwapItemCommand`: Handles swapping items between inventory/equipment.
    *   `ClaimRewardItemCommand`: Handles claiming reward items.

### Modified Components:

*   **`SlotManipulator`:** Will be simplified to primarily handle UI input. It will create the appropriate command object based on user interaction and pass it to the `UICommandProcessor`. All validation and business logic will be removed from this class.
*   **`ItemManipulationService`:** Its `Request...` methods will be removed. Its core `Perform...` methods (e.g., `PerformSwap`, `PerformPurchase`, `PerformClaimReward`) will be made public and will contain only the actual state-changing logic, assuming all validation has been performed by the command's `CanExecute()` method.
*   **`UIInteractionService`:** Remains largely unchanged, but its `CanManipulateItem` method will now be primarily consumed by the `CanExecute()` methods of the command objects.
*   **`EconomyService`:** A new `CanAfford` method will be added to separate the gold check from the spending action.

## 3. Detailed Implementation Steps

### Phase 1: Define Command Infrastructure

1.  **Create `ICommand` interface:**
    *   **File:** `Assets/Scripts/Commands/ICommand.cs`
    *   **Content:**
        ```csharp
        namespace PirateRoguelike.Commands
        {
            public interface ICommand
            {
                bool CanExecute();
                void Execute();
            }
        }
        ```

2.  **Create `UICommandProcessor`:**
    *   **File:** `Assets/Scripts/Commands/UICommandProcessor.cs`
    *   **Content:**
        ```csharp
        using UnityEngine;
        using PirateRoguelike.Services; // For SlotId, SlotContainerType if needed for logging/context

        namespace PirateRoguelike.Commands
        {
            public class UICommandProcessor
            {
                private static UICommandProcessor _instance;
                public static UICommandProcessor Instance
                {
                    get
                    {
                        if (_instance == null)
                        {
                            _instance = new UICommandProcessor();
                        }
                        return _instance;
                    }
                }

                private UICommandProcessor() { } // Private constructor for singleton

                public void ProcessCommand(ICommand command)
                {
                    if (command == null)
                    {
                        Debug.LogError("Attempted to process a null command.");
                        return;
                    }

                    if (command.CanExecute())
                    {
                        command.Execute();
                    }
                    else
                    {
                        Debug.LogWarning($"Command {command.GetType().Name} cannot be executed. Validation failed.");
                        // TODO: Potentially dispatch a UI event for user feedback (e.g., "Invalid action!")
                    }
                }
            }
        }
        ```

### Phase 2: Implement Specific Commands

1.  **Create `PurchaseItemCommand`:**
    *   **File:** `Assets/Scripts/Commands/PurchaseItemCommand.cs`
    *   **Content:**
        ```csharp
        using UnityEngine;
        using PirateRoguelike.Services;
        using PirateRoguelike.Core;
        using PirateRoguelike.Data;
        using PirateRoguelike.UI;
        using PirateRoguelike.Encounters; // For ShopManager

        namespace PirateRoguelike.Commands
        {
            public class PurchaseItemCommand : ICommand
            {
                private readonly SlotId _shopSlot;
                private readonly SlotId _playerTargetSlot; // Can be -1 for auto-find
                private readonly ItemSO _itemToPurchase;
                private SlotId _finalDestinationSlot; // Determined during CanExecute

                public PurchaseItemCommand(SlotId shopSlot, SlotId playerTargetSlot, ItemSO itemToPurchase)
                {
                    _shopSlot = shopSlot;
                    _playerTargetSlot = playerTargetSlot;
                    _itemToPurchase = itemToPurchase;
                }

                public bool CanExecute()
                {
                    if (UIStateService.IsConsoleOpen) return false;
                    if (!UIInteractionService.CanManipulateItem(SlotContainerType.Shop)) return false;
                    if (_itemToPurchase == null)
                    {
                        Debug.LogError("Item to purchase is null.");
                        ShopManager.Instance?.DisplayMessage("Item not available!");
                        return false;
                    }
                    if (!GameSession.Economy.CanAfford(_itemToPurchase.Cost))
                    {
                        Debug.LogWarning($"Not enough gold to purchase {_itemToPurchase.displayName}. Cost: {_itemToPurchase.Cost}, Gold: {GameSession.Economy.Gold}");
                        ShopManager.Instance?.DisplayMessage("Not enough gold!");
                        return false;
                    }

                    // Slot finding logic (moved from ItemManipulationService.RequestPurchase)
                    _finalDestinationSlot = _playerTargetSlot;
                    if (_playerTargetSlot.Index == -1 || (_playerTargetSlot.ContainerType == SlotContainerType.Inventory && GameSession.Inventory.IsSlotOccupied(_playerTargetSlot.Index)) || (_playerTargetSlot.ContainerType == SlotContainerType.Equipment && GameSession.PlayerShip.IsEquipmentSlotOccupied(_playerTargetSlot.Index)))
                    {
                        int availableInventorySlot = GameSession.Inventory.GetFirstEmptySlot();
                        if (availableInventorySlot != -1)
                        {
                            _finalDestinationSlot = new SlotId(availableInventorySlot, SlotContainerType.Inventory);
                        }
                        else
                        {
                            int availableEquipmentSlot = GameSession.PlayerShip.GetFirstEmptyEquipmentSlot();
                            if (availableEquipmentSlot != -1)
                            {
                                _finalDestinationSlot = new SlotId(availableEquipmentSlot, SlotContainerType.Equipment);
                            }
                            else
                            {
                                Debug.LogWarning($"No available slots for {_itemToPurchase.displayName}.");
                                ShopManager.Instance?.DisplayMessage("Inventory full!");
                                return false;
                            }
                        }
                    }
                    return true;
                }

                public void Execute()
                {
                    GameSession.Economy.SpendGold(_itemToPurchase.Cost);

                    bool purchaseSuccessful = ItemManipulationService.Instance.PerformPurchase(_itemToPurchase, _finalDestinationSlot, _shopSlot);

                    if (purchaseSuccessful)
                    {
                        Debug.Log($"Successfully purchased {_itemToPurchase.displayName} for {_itemToPurchase.Cost} gold and placed in {_finalDestinationSlot.ContainerType} slot {_finalDestinationSlot.Index}.");
                        ShopManager.Instance?.DisplayMessage($"Purchased {_itemToPurchase.displayName}!");
                    }
                    else
                    {
                        GameSession.Economy.AddGold(_itemToPurchase.Cost); // Refund gold
                        Debug.LogError($"Failed to add {_itemToPurchase.displayName} to slot {_finalDestinationSlot.Index}. Gold refunded.");
                        ShopManager.Instance?.DisplayMessage("Purchase failed!");
                    }
                }
            }
        }
        ```

2.  **Create `SwapItemCommand`:**
    *   **File:** `Assets/Scripts/Commands/SwapItemCommand.cs`
    *   **Content:**
        ```csharp
        using UnityEngine;
        using PirateRoguelike.Services;
        using PirateRoguelike.Core;
        using PirateRoguelike.UI;

        namespace PirateRoguelike.Commands
        {
            public class SwapItemCommand : ICommand
            {
                private readonly SlotId _fromSlot;
                private readonly SlotId _toSlot;

                public SwapItemCommand(SlotId fromSlot, SlotId toSlot)
                {
                    _fromSlot = fromSlot;
                    _toSlot = toSlot;
                }

                public bool CanExecute()
                {
                    if (UIStateService.IsConsoleOpen) return false;
                    if (!UIInteractionService.CanManipulateItem(_fromSlot.ContainerType) || !UIInteractionService.CanManipulateItem(_toSlot.ContainerType))
                    {
                        Debug.LogWarning($"Cannot swap items. Manipulation not allowed for container types: {_fromSlot.ContainerType}, {_toSlot.ContainerType}");
                        return false;
                    }
                    // Add any other specific swap validation here if needed
                    return true;
                }

                public void Execute()
                {
                    ItemManipulationService.Instance.PerformSwap(_fromSlot, _toSlot);
                    Debug.Log($"Successfully swapped item from {_fromSlot.ContainerType} slot {_fromSlot.Index} to {_toSlot.ContainerType} slot {_toSlot.Index}.");
                }
            }
        }
        ```

3.  **Create `ClaimRewardItemCommand`:**
    *   **File:** `Assets/Scripts/Commands/ClaimRewardItemCommand.cs`
    *   **Content:**
        ```csharp
        using UnityEngine;
        using PirateRoguelike.Services;
        using PirateRoguelike.Core;
        using PirateRoguelike.Data;
        using PirateRoguelike.UI;

        namespace PirateRoguelike.Commands
        {
            public class ClaimRewardItemCommand : ICommand
            {
                private readonly SlotId _sourceSlot;
                private readonly SlotId _destinationSlot; // Can be -1 for auto-find
                private readonly ItemSO _itemToClaim;
                private SlotId _finalDestinationSlot; // Determined during CanExecute

                public ClaimRewardItemCommand(SlotId sourceSlot, SlotId destinationSlot, ItemSO itemToClaim)
                {
                    _sourceSlot = sourceSlot;
                    _destinationSlot = destinationSlot;
                    _itemToClaim = itemToClaim;
                }

                public bool CanExecute()
                {
                    if (UIStateService.IsConsoleOpen) return false;
                    if (!UIInteractionService.CanManipulateItem(SlotContainerType.Reward)) return false;
                    if (_itemToClaim == null)
                    {
                        Debug.LogError("Item to claim is null.");
                        return false;
                    }

                    // Slot finding logic (moved from ItemManipulationService.RequestClaimReward)
                    _finalDestinationSlot = _destinationSlot;
                    if (_destinationSlot.Index == -1 || (_destinationSlot.ContainerType == SlotContainerType.Inventory && GameSession.Inventory.IsSlotOccupied(_destinationSlot.Index)) || (_destinationSlot.ContainerType == SlotContainerType.Equipment && GameSession.PlayerShip.IsEquipmentSlotOccupied(_destinationSlot.Index)))
                    {
                        int availableInventorySlot = GameSession.Inventory.GetFirstEmptySlot();
                        if (availableInventorySlot != -1)
                        {
                            _finalDestinationSlot = new SlotId(availableInventorySlot, SlotContainerType.Inventory);
                        }
                        else
                        {
                            int availableEquipmentSlot = GameSession.PlayerShip.GetFirstEmptyEquipmentSlot();
                            if (availableEquipmentSlot != -1)
                            {
                                _finalDestinationSlot = new SlotId(availableEquipmentSlot, SlotContainerType.Equipment);
                            }
                            else
                            {
                                Debug.LogWarning($"No available slots for {_itemToClaim.displayName}.");
                                return false;
                            }
                        }
                    }
                    return true;
                }

                public void Execute()
                {
                    bool claimSuccessful = ItemManipulationService.Instance.PerformClaimReward(_itemToClaim, _finalDestinationSlot, _sourceSlot);

                    if (claimSuccessful)
                    {
                        Debug.Log($"Successfully claimed {_itemToClaim.displayName} and placed in {_finalDestinationSlot.ContainerType} slot {_finalDestinationSlot.Index}.");
                    }
                    else
                    {
                        Debug.LogError($"Failed to add {_itemToClaim.displayName} to slot {_finalDestinationSlot.Index}.");
                    }
                }
            }
        }
        ```

### Phase 3: Refactor `ItemManipulationService` and `EconomyService`

1.  **Modify `EconomyService.cs`:**
    *   **File:** `Assets/Scripts/Core/EconomyService.cs`
    *   **Changes:**
        ```csharp
        using System;
        using UnityEngine;
        using PirateRoguelike.Data;
        using PirateRoguelike.Saving;

        namespace PirateRoguelike.Services
        {
            public class EconomyService
            {
                public static event Action<int> OnGoldChanged;
                public static event Action<int> OnLivesChanged;

                public int Gold { get; private set; }
                public int Lives { get; private set; }

                private readonly RunConfigSO _config;
                private int _rerollsThisShop;
                private bool _freeRerollAvailable = false;

                public EconomyService(RunConfigSO config, RunState runState = null)
                {
                    _config = config;
                    if (runState != null)
                    {
                        Gold = runState.gold;
                        Lives = runState.playerLives;
                        _rerollsThisShop = runState.rerollsThisShop;
                    }
                    else
                    {
                        Gold = config.startingGold;
                        Lives = config.startingLives;
                    }
                }

                public void SaveToRunState(RunState runState)
                {
                    runState.gold = Gold;
                    runState.playerLives = Lives;
                    runState.rerollsThisShop = _rerollsThisShop;
                }

                public void MarkFreeRerollAvailable()
                {
                    _freeRerollAvailable = true;
                }

                public void AddGold(int amount)
                {
                    Gold = Mathf.Min(Gold + amount, 999);
                    OnGoldChanged?.Invoke(Gold);
                }

                public bool TrySpendGold(int amount)
                {
                    if (Gold >= amount)
                    {
                        Gold -= amount;
                        OnGoldChanged?.Invoke(Gold);
                        return true;
                    }
                    return false;
                }

                // NEW: CanAfford method
                public bool CanAfford(int amount)
                {
                    return Gold >= amount;
                }

                public void AddLives(int amount)
                {
                    Lives += amount;
                    OnLivesChanged?.Invoke(Lives);
                }

                public void LoseLife()
                {
                    Lives--;
                    OnLivesChanged?.Invoke(Lives);
                }

                public int GetCurrentRerollCost()
                {
                    if (_freeRerollAvailable && _rerollsThisShop == 0)
                    {
                        return 0;
                    }
                    return (int)Mathf.Round(_config.rerollBaseCost * Mathf.Pow(_config.rerollGrowth, _rerollsThisShop));
                }

                public void IncrementRerollCount()
                {
                    _rerollsThisShop++;
                    _freeRerollAvailable = false; // Free reroll used
                }

                public void ResetRerollCount()
                {
                    _rerollsThisShop = 0;
                    _freeRerollAvailable = false; // Reset for next shop
                }
            }
        }
        ```

2.  **Modify `ItemManipulationService.cs`:**
    *   **File:** `Assets/Scripts/Core/ItemManipulationService.cs`
    *   **Changes:**
        ```csharp
        using System;
        using UnityEngine;
        using PirateRoguelike.Data;
        using PirateRoguelike.Core; // Assuming GameSession is in Core
        using System.Linq; // For LINQ operations like FirstOrDefault, Any
        using PirateRoguelike.Events; // Added

        namespace PirateRoguelike.Services
        {
            public class ItemManipulationService
            {
                private static ItemManipulationService _instance;
                public static ItemManipulationService Instance
                {
                    get
                    {
                        if (_instance == null)
                        {
                            _instance = new ItemManipulationService();
                        }
                        return _instance;
                    }
                }

                private IGameSession _gameSession; // Dependency

                private ItemManipulationService() { } // Private constructor for singleton

                public void Initialize(IGameSession gameSession)
                {
                    _gameSession = gameSession;
                }

                // Renamed and made public for use by SwapItemCommand
                public void PerformSwap(SlotId slotA, SlotId slotB)
                {
                    ItemInstance itemA = null;
                    ItemInstance itemB = null;

                    // Get itemA from slotA and remove it
                    if (slotA.ContainerType == SlotContainerType.Inventory)
                    {
                        itemA = _gameSession.Inventory.GetItemAt(slotA.Index);
                        _gameSession.Inventory.RemoveItemAt(slotA.Index);
                    }
                    else if (slotA.ContainerType == SlotContainerType.Equipment)
                    {
                        itemA = _gameSession.PlayerShip.GetEquippedItem(slotA.Index);
                        _gameSession.PlayerShip.RemoveEquippedAt(slotA.Index);
                    }

                    // Get itemB from slotB and remove it
                    if (slotB.ContainerType == SlotContainerType.Inventory)
                    {
                        itemB = _gameSession.Inventory.GetItemAt(slotB.Index);
                        _gameSession.Inventory.RemoveItemAt(slotB.Index);
                    }
                    else if (slotB.ContainerType == SlotContainerType.Equipment)
                    {
                        itemB = _gameSession.PlayerShip.GetEquippedItem(slotB.Index);
                        _gameSession.PlayerShip.RemoveEquippedAt(slotB.Index);
                    }

                    // Place itemB into slotA
                    if (itemB != null)
                    {
                        if (slotA.ContainerType == SlotContainerType.Inventory)
                        {
                            _gameSession.Inventory.AddItemAt(itemB, slotA.Index);
                        }
                        else if (slotA.ContainerType == SlotContainerType.Equipment)
                        {
                            _gameSession.PlayerShip.SetEquipment(slotA.Index, itemB);
                        }
                    }

                    // Place itemA into slotB
                    if (itemA != null)
                    {
                        if (slotB.ContainerType == SlotContainerType.Inventory)
                        {
                            _gameSession.Inventory.AddItemAt(itemA, slotB.Index);
                        }
                        else if (slotB.ContainerType == SlotContainerType.Equipment)
                        {
                            _gameSession.PlayerShip.SetEquipment(slotB.Index, itemA);
                        }
                    }
                }

                // New method for PurchaseItemCommand to call
                public bool PerformPurchase(ItemSO itemToPurchase, SlotId targetFinalSlot, SlotId shopSlot)
                {
                    bool purchaseSuccessful = false;
                    if (targetFinalSlot.ContainerType == SlotContainerType.Inventory)
                    {
                        purchaseSuccessful = _gameSession.Inventory.AddItemAt(new ItemInstance(itemToPurchase), targetFinalSlot.Index);
                    }
                    else if (targetFinalSlot.ContainerType == SlotContainerType.Equipment)
                    {
                        purchaseSuccessful = _gameSession.PlayerShip.SetEquipment(targetFinalSlot.Index, new ItemInstance(itemToPurchase));
                    }

                    if (purchaseSuccessful)
                    {
                        PirateRoguelike.Encounters.ShopManager.Instance?.RemoveShopItem(shopSlot.Index);
                        // Debug.Log and DisplayMessage will be handled by the command
                    }
                    return purchaseSuccessful;
                }

                // New method for ClaimRewardItemCommand to call
                public bool PerformClaimReward(ItemSO itemToClaim, SlotId targetFinalSlot, SlotId sourceSlot)
                {
                    bool claimSuccessful = false;
                    if (targetFinalSlot.ContainerType == SlotContainerType.Inventory)
                    {
                        claimSuccessful = _gameSession.Inventory.AddItemAt(new ItemInstance(itemToClaim), targetFinalSlot.Index);
                    }
                    else if (targetFinalSlot.ContainerType == SlotContainerType.Equipment)
                    {
                        claimSuccessful = _gameSession.PlayerShip.SetEquipment(targetFinalSlot.Index, new ItemInstance(itemToClaim));
                    }

                    if (claimSuccessful)
                    {
                        RewardService.RemoveClaimedItem(itemToClaim);
                        ItemManipulationEvents.DispatchRewardItemClaimed(sourceSlot.Index);
                    }
                    return claimSuccessful;
                }
            }
        }
        ```

### Phase 4: Refactor `SlotManipulator`

1.  **Modify `SlotManipulator.cs`:**
    *   **File:** `Assets/Scripts/UI/PlayerPanel/SlotManipulator.cs`
    *   **Changes:**
        ```csharp
        using UnityEngine;
        using UnityEngine.UIElements;
        using System;
        using System.Linq;
        using PirateRoguelike.Services;
        using PirateRoguelike.UI.Components;
        using PirateRoguelike.Data;
        using PirateRoguelike.Commands; // New using statement

        namespace PirateRoguelike.UI
        {
            public class SlotManipulator : PointerManipulator
            {
                private VisualElement _ghostIcon;
                private Vector2 _startPosition;
                public bool IsDragging { get; private set; }
                private ISlotViewData _sourceSlotData;
                private global::PirateRoguelike.Services.SlotContainerType _fromContainer;
                private VisualElement _lastHoveredSlot;
                private float _dragThreshold = 5f;
                private Vector2 _pointerDownPosition;
                private bool _isPointerDown = false;
                private PointerDownEvent _initialPointerDownEvent;

                public SlotManipulator(VisualElement targetElement, ISlotViewData sourceSlotData)
                {
                    target = targetElement;
                    _sourceSlotData = sourceSlotData;
                    IsDragging = false;
                    Debug.Log($"SlotManipulator: Constructor called for {targetElement.name} ({targetElement.GetType().Name}).");
                }

                protected override void RegisterCallbacksOnTarget()
                {
                    target.RegisterCallback<PointerDownEvent>(OnPointerDown);
                    target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                    target.RegisterCallback<PointerUpEvent>(OnPointerUp);
                    target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
                }

                protected override void UnregisterCallbacksFromTarget()
                {
                    target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                    target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                    target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                    target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
                    Debug.Log($"SlotManipulator: UnregisterCallbacksFromTarget called for {target.name} ({target.GetType().Name}).");
                }

                private void OnPointerDown(PointerDownEvent evt)
                {
                    _fromContainer = _sourceSlotData.ContainerType;

                    if (UIStateService.IsConsoleOpen) { return; }

                    if (!UIInteractionService.CanManipulateItem(_fromContainer))
                    {
                        return;
                    }

                    if (_sourceSlotData == null || _sourceSlotData.IsEmpty)
                    {
                        return;
                    }

                    TooltipController.Instance.Hide();

                    _pointerDownPosition = evt.position;
                    _isPointerDown = true;
                    target.CapturePointer(evt.pointerId);
                    _initialPointerDownEvent = evt;
                }

                private void OnPointerMove(PointerMoveEvent evt)
                {
                    if (!_isPointerDown) return; // Only proceed if mouse button is down

                    float distance = Vector2.Distance(_pointerDownPosition, evt.position); // Declare distance once here

                    if (!IsDragging) // If dragging has not yet started
                    {
                        if (distance > _dragThreshold)
                        {
                            IsDragging = true;
                            target.style.visibility = Visibility.Hidden;

                            PlayerPanelEvents.OnSlotBeginDrag?.Invoke(_sourceSlotData.SlotId, _initialPointerDownEvent);

                            _ghostIcon = new Image();
                            float ghostWidth = target.resolvedStyle.width * 0.8f;
                            float ghostHeight = target.resolvedStyle.height * 0.8f;
                            _ghostIcon.style.width = ghostWidth;
                            _ghostIcon.style.height = ghostHeight;
                            ((Image)_ghostIcon).sprite = _sourceSlotData.Icon;
                            _ghostIcon.style.position = Position.Absolute;
                            _ghostIcon.pickingMode = PickingMode.Ignore;
                            _ghostIcon.style.opacity = 0.8f;
                            target.panel.visualTree.Add(_ghostIcon);

                            _ghostIcon.style.left = evt.position.x - (ghostWidth / 2);
                            _ghostIcon.style.top = evt.position.y - (ghostHeight / 2);

                            UpdateGhostPosition(evt.position); // Initial position update for the ghost icon
                        }
                    }

                    if (IsDragging) // If dragging is active (either just initiated or ongoing)
                    {
                        UpdateGhostPosition(evt.position);

                        (VisualElement newHoveredSlotElement, ISlotViewData newHoveredSlotData, global::PirateRoguelike.Services.SlotContainerType newHoveredContainerType) = FindHoveredSlot(evt.position);

                        if (newHoveredSlotElement != _lastHoveredSlot)
                        {
                            _lastHoveredSlot?.RemoveFromClassList("slot--hover");
                            newHoveredSlotElement?.AddToClassList("slot--hover");
                            _lastHoveredSlot = newHoveredSlotElement;
                        }
                    }
                }

                private void OnPointerUp(PointerUpEvent evt)
                {
                    _isPointerDown = false;

                    if (!target.HasPointerCapture(evt.pointerId)) return;

                    _lastHoveredSlot?.RemoveFromClassList("slot--hover");

                    (VisualElement dropTargetElement, ISlotViewData dropSlotData, global::PirateRoguelike.Services.SlotContainerType toContainer) = FindHoveredSlot(evt.position);

                    Debug.Log($"OnPointerUp: Drop Target Element: {dropTargetElement?.name ?? "NULL"} (Type: {dropTargetElement?.GetType().Name ?? "NULL"})");
                    Debug.Log($"OnPointerUp: Drop Slot Data: {dropSlotData?.SlotId.ToString() ?? "NULL"} (IsEmpty: {dropSlotData?.IsEmpty.ToString() ?? "NULL"})");
                    Debug.Log($"OnPointerUp: To Container: {toContainer}");

                    target.style.visibility = Visibility.Visible;

                    global::PirateRoguelike.Services.SlotId fromSlotId = new global::PirateRoguelike.Services.SlotId(_sourceSlotData.SlotId, _fromContainer);
                    global::PirateRoguelike.Services.SlotId toSlotId = (dropSlotData != null) ? new global::PirateRoguelike.Services.SlotId(dropSlotData.SlotId, toContainer) : new global::PirateRoguelike.Services.SlotId(-1, global::PirateRoguelike.Services.SlotContainerType.Inventory); // Default to inventory if no specific target

                    ICommand command = null;

                    if (!IsDragging && _fromContainer == global::PirateRoguelike.Services.SlotContainerType.Shop)
                    {
                        // Click on shop item
                        ItemSO itemToPurchase = _sourceSlotData.CurrentItemInstance.Def; // Assuming ISlotViewData provides CurrentItemInstance.Def
                        command = new PurchaseItemCommand(fromSlotId, toSlotId, itemToPurchase);
                    }
                    else if (_fromContainer == global::PirateRoguelike.Services.SlotContainerType.Shop)
                    {
                        // Drag from shop
                        ItemSO itemToPurchase = _sourceSlotData.CurrentItemInstance.Def; // Assuming ISlotViewData provides CurrentItemInstance.Def
                        command = new PurchaseItemCommand(fromSlotId, toSlotId, itemToPurchase);
                    }
                    else if (_fromContainer == global::PirateRoguelike.Services.SlotContainerType.Inventory || _fromContainer == global::PirateRoguelike.Services.SlotContainerType.Equipment)
                    {
                        // Drag from inventory/equipment
                        if (dropTargetElement != null && dropSlotData != null)
                        {
                            command = new SwapItemCommand(fromSlotId, toSlotId);
                        }
                    }
                    else if (_fromContainer == global::PirateRoguelike.Services.SlotContainerType.Reward)
                    {
                        ItemSO itemToClaim = _sourceSlotData.CurrentItemInstance.Def; // Assuming ISlotViewData provides CurrentItemInstance.Def
                        if (dropTargetElement != null && dropSlotData != null && (toContainer == global::PirateRoguelike.Services.SlotContainerType.Inventory || toContainer == global::PirateRoguelike.Services.SlotContainerType.Equipment))
                        {
                            // Drag from reward to inventory/equipment
                            command = new ClaimRewardItemCommand(fromSlotId, toSlotId, itemToClaim);
                        }
                        else if (!IsDragging) // Click to claim
                        {
                            // Click on reward item
                            command = new ClaimRewardItemCommand(fromSlotId, toSlotId, itemToClaim);
                        }
                        else
                        {
                            Debug.LogWarning("Reward item dropped outside valid target or not a click.");
                        }
                    }

                    if (command != null)
                    {
                        UICommandProcessor.Instance.ProcessCommand(command);
                    }

                    CleanUp();
                    target.ReleasePointer(evt.pointerId);
                }

                protected override void UnregisterCallbacksFromTarget()
                {
                    target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
                    target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
                    target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                    target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
                    Debug.Log($"SlotManipulator: UnregisterCallbacksFromTarget called for {target.name} ({target.GetType().Name}).");
                }

                private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
                {
                    if (!IsDragging) return;
                    CleanUp();
                }

                private void UpdateGhostPosition(Vector2 pointerPosition)
                {
                    _ghostIcon.style.left = pointerPosition.x - (_ghostIcon.resolvedStyle.width / 2);
                    _ghostIcon.style.top = pointerPosition.y - (_ghostIcon.resolvedStyle.height / 2);
                }

                private (VisualElement, ISlotViewData, global::PirateRoguelike.Services.SlotContainerType) FindHoveredSlot(Vector2 pointerPosition)
                {
                    PickingMode originalPickingMode = target.pickingMode;
                    target.pickingMode = PickingMode.Ignore;

                    VisualElement picked = target.panel.Pick(pointerPosition);

                    target.pickingMode = originalPickingMode;

                    if (picked == null)
                    {
                        return (null, null, default);
                    }

                    VisualElement current = picked;
                    while (current != null && !current.ClassListContains("slot"))
                    {
                        current = current.parent;
                    }

                    if (current != null)
                    {
                        SlotElement slotElement = current as SlotElement;
                        if (slotElement != null && slotElement.userData is ISlotViewData slotData)
                        {
                            return (current, slotData, slotData.ContainerType);
                        }
                    }
                    return (null, null, default);
                }

                private void CleanUp()
                {
                    IsDragging = false;
                    _isPointerDown = false;
                    _ghostIcon?.RemoveFromHierarchy();
                    _ghostIcon = null;
                    _lastHoveredSlot?.RemoveFromClassList("slot--hover");
                    _lastHoveredSlot = null;
                }

                public void Dispose()
                {
            UnregisterCallbacksFromTarget();
        }
    }
}
        ```
