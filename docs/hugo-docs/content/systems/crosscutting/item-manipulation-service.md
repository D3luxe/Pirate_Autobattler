---
title: "Item Manipulation Service"
weight: 10
system: ["crosscutting", "core", "ui"]
types: ["system-overview"]
---

## Overview

The Item Manipulation Service (`ItemManipulationService.cs`) is a central singleton responsible for handling all item-related operations within the game. It acts as a crucial intermediary, decoupling UI interactions from direct modifications to the core game state. This service ensures that item swaps, purchases, and other manipulations are performed consistently and adhere to game rules.

## Design

The service is designed as a singleton to provide a single, authoritative point of control for item operations. Its methods are now primarily invoked by command objects (e.g., `PurchaseItemCommand`, `SwapItemCommand`) after those commands have performed necessary validation.

*   **Singleton Pattern:** Implemented as a static singleton (`ItemManipulationService.Instance`) to ensure a single instance manages all item manipulations globally.
*   **Dependencies:** It is initialized with an `IGameSession` instance, through which it accesses core game components like `Economy`, `Inventory`, and `PlayerShip`.
*   **Decoupling:** The service operates by modifying the underlying game state (e.g., adding/removing items from inventory). It does not directly manipulate UI elements; instead, changes to the game state trigger UI updates through an event-driven architecture (e.g., `ItemManipulationEvents`). It also dispatches `OnRewardItemClaimed` when a reward item is successfully claimed. Validation logic previously residing here has been moved to the command objects that invoke this service.

## Implementation Details

### Core Components

*   **`ItemManipulationService` (`Assets/Scripts/Core/ItemManipulationService.cs`):**
    *   **`Initialize(IGameSession gameSession)`:** Sets up the service's dependency on the `IGameSession` instance, providing access to core game data and services.
    *   **`PerformSwap(SlotId fromSlot, SlotId toSlot)`:**
        *   Receives `SlotId` objects representing the source and destination slots.
        *   Manages the low-level logic for swapping `ItemInstance` objects between any two specified slots. This involves retrieving items from their current locations, removing them, and then placing them into their new target slots.
        *   This method is now public and is called by `SwapItemCommand.Execute()`.
    *   **`PerformPurchase(ItemSO itemToPurchase, SlotId targetFinalSlot, SlotId shopSlot)`:**
        *   Receives the `ItemSO` to purchase, the final destination `SlotId`, and the source `shopSlot`.
        *   Adds the newly purchased `ItemInstance` to the `targetFinalSlot` within either `_gameSession.Inventory` or `_gameSession.PlayerShip`.
        *   Upon successful purchase, the item is removed from the shop's available items via `ShopManager.Instance.RemoveShopItem(shopSlot.Index)`.
        *   This method is called by `PurchaseItemCommand.Execute()` after all validation (gold, slot availability) has passed.
    *   **`PerformClaimReward(ItemSO itemToClaim, SlotId targetFinalSlot, SlotId sourceSlot)`:**
        *   Receives the `ItemSO` to claim, the final destination `SlotId`, and the source `sourceSlot`.
        *   Adds the `ItemInstance` to the `targetFinalSlot` within `_gameSession.Inventory` or `_gameSession.PlayerShip`.
        *   Upon successful claim, calls `RewardService.RemoveClaimedItem(itemToClaim)` to update the reward state by removing the specific `ItemSO` instance.
        *   This method is called by `ClaimRewardItemCommand.Execute()` after all validation (slot availability) has passed.
*   **`SlotId` (`PirateRoguelike.Services.SlotId`):**
    *   A lightweight `struct` used throughout the item manipulation system to uniquely identify an item slot. It combines an integer `Index` (representing the slot's position within its container) and a `SlotContainerType` (specifying the type of container, e.g., Inventory, Equipment, Shop).
*   **`SlotContainerType` (`PirateRoguelike.Services.SlotContainerType`):**
    *   An `enum` that categorizes different types of item containers within the game, such as `Inventory`, `Equipment`, `Shop`, and `Crafting`. This allows the `ItemManipulationService` to apply context-specific rules and logic.

## Related Documents

*   [UI Systems Overview]({{< myrelref "../ui/ui-systems.md" >}})
*   [Core Systems Overview]({{< myrelref "../core/_index.md" >}})
*   [Shop System Overview]({{< myrelref "../crosscutting/shop-system-overview.md" >}})

## Process Flowchart

This diagram outlines the typical process flows for item manipulation requests handled by the `ItemManipulationService`.

```plantuml
@startuml
' --- STYLING ---
skinparam style strictuml
skinparam shadowing falsetrue
skinparam defaultFontName "Segoe UI"
skinparam defaultFontSize 24
skinparam backgroundColor #b4b4b42c
!pragma usecasesquare
skinparam class {
    BorderColor #A9A9A9
    BorderThickness 1.5
    ArrowColor #555555
    ArrowThickness 1.5
}
skinparam note {
    BackgroundColor #FFFFE0
    BorderColor #B4B4B4
}

' Define stereotype colors
skinparam class<<Service>> {
    BackgroundColor #ADD8E6
}
skinparam class<<Interface>> {
    BackgroundColor #FFFF99
}
skinparam class<<Data>> {
    BackgroundColor #D3D3D3
}
skinparam class<<Manager>> {
    BackgroundColor #DDA0DD
}
skinparam class<<Command>> {
    BackgroundColor #FFC0CB ' Pink
}

' --- CLASSES ---
class ItemManipulationService <<Service>> {
    + {static} Instance : ItemManipulationService
    - IGameSession _gameSession
    + Initialize(gameSession: IGameSession)
    + PerformSwap(slotA: SlotId, slotB: SlotId)
    + PerformPurchase(itemToPurchase: ItemSO, targetFinalSlot: SlotId, shopSlot: SlotId)
    + PerformClaimReward(itemToClaim: ItemSO, targetFinalSlot: SlotId, sourceSlot: SlotId)
}

interface IGameSession <<Interface>> {
    + Economy : EconomyService
    + Inventory : Inventory
    + PlayerShip : ShipState
}

class EconomyService <<Service>> {
    + CanAfford(amount: int) : bool
    + SpendGold(amount: int)
    + AddGold(amount: int)
    + Gold : int
}

class Inventory <<Data>> {
    + AddItemAt(item: ItemInstance, index: int) : bool
    + RemoveItemAt(index: int) : ItemInstance
    + GetItemAt(index: int) : ItemInstance
    + IsSlotOccupied(index: int) : bool
    + GetFirstEmptySlot() : int
}

class ShipState <<Data>> {
    + SetEquipment(index: int, item: ItemInstance) : bool
    + RemoveEquippedAt(index: int) : ItemInstance
    + GetEquippedItem(index: int) : ItemInstance
    + IsEquipmentSlotOccupied(index: int) : bool
    + GetFirstEmptyEquipmentSlot() : int
}

class ShopManager <<Manager>> {
    + {static} Instance : ShopManager
    + GetShopItem(index: int) : ItemSO
    + RemoveShopItem(index: int)
    + DisplayMessage(message: string)
}

class UIInteractionService <<Service>> {
    + {static} CanManipulateItem(containerType: SlotContainerType) : bool
}

struct SlotId <<Data>> {
    + Index : int
    + ContainerType : SlotContainerType
}

enum SlotContainerType <<Data>> {
    Inventory
    Equipment
    Shop
    Crafting
}

class ItemSO <<Data>> {
    + Cost : int
    + displayName : string
}

class ItemInstance <<Data>> {
    ' Represents a runtime instance of an ItemSO
}

interface ICommand <<Command>> {
    + CanExecute(): bool
    + Execute(): void
}

class PurchaseItemCommand <<Command>> {
    - _shopSlot: SlotId
    - _playerTargetSlot: SlotId
    - _itemToPurchase: ItemSO
    + CanExecute(): bool
    + Execute(): void
}

class SwapItemCommand <<Command>> {
    - _fromSlot: SlotId
    - _toSlot: SlotId
    + CanExecute(): bool
    + Execute(): void
}

class ClaimRewardItemCommand <<Command>> {
    - _sourceSlot: SlotId
    - _destinationSlot: SlotId
    - _itemToClaim: ItemSO
    + CanExecute(): bool
    + Execute(): void
}

class UICommandProcessor <<Service>> {
    + {static} Instance: UICommandProcessor
    + ProcessCommand(command: ICommand)
}

' --- RELATIONSHIPS ---
ItemManipulationService "1" -- "1" IGameSession : uses >

PurchaseItemCommand --> ItemManipulationService : calls PerformPurchase >
PurchaseItemCommand --> UIInteractionService : checks permission >
PurchaseItemCommand --> EconomyService : checks/spends gold >
PurchaseItemCommand --> ShopManager : interacts with >

SwapItemCommand --> ItemManipulationService : calls PerformSwap >
SwapItemCommand --> UIInteractionService : checks permission >

ClaimRewardItemCommand --> ItemManipulationService : calls PerformClaimReward >
ClaimRewardItemCommand --> UIInteractionService : checks permission >

UICommandProcessor --> ICommand : processes >
UICommandProcessor --> UIInteractionService : checks permission >

ICommand <|-- PurchaseItemCommand
ICommand <|-- SwapItemCommand
ICommand <|-- ClaimRewardItemCommand

ItemManipulationService ..> SlotId : uses
ItemManipulationService ..> SlotContainerType : uses

ItemInstance "1" -- "1" ItemSO : wraps >

Inventory "1" -- "*" ItemInstance : contains
ShipState "1" -- "*" ItemInstance : equips

@enduml
```