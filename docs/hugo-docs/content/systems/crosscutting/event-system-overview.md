---
title: "Event System Overview"
weight: 30
system: ["crosscutting", "core"]
types: ["system-overview", "analysis"]
---

## Overview

The game employs a hybrid eventing architecture to manage communication between its various systems. This approach ensures that components can be both highly decoupled for flexibility and tightly coupled for performance and simplicity where appropriate. Three distinct event patterns are used:

1.  **Global Static Event Bus (`EventBus.cs`):** A centralized, static class for broadcasting major game logic events. It serves as the primary mechanism for decoupling high-level systems, especially within the combat loop. Systems can subscribe to events like `OnBattleStart` or `OnDamageReceived` without needing a direct reference to the event publisher.

2.  **Domain-Specific Static Event Bus (`ItemManipulationEvents.cs`):** A specialized static class dedicated exclusively to notifying the UI about changes in item state (inventory and equipment). This isolates UI-related communication, preventing the global `EventBus` from being cluttered with UI-specific concerns.

3.  **Direct C# Instance Events:** Standard `.NET` events (e.g., `public event Action OnTick`) are used for direct, one-to-one or one-to-few notifications where the subscriber has a direct reference to the publisher instance. This is the most performant method and is used for high-frequency events (`TickService.OnTick`) or direct state updates (`ShipState.OnHealthChanged`).

## Design

The design of the event architecture prioritizes a clear separation of concerns:

*   **`EventBus` for Game Logic:** This bus is intended for significant state changes and triggers that affect gameplay mechanics. Its global, static nature makes it ideal for systems that are instantiated or managed independently, such as the `CombatController` and the `AbilityManager`.
*   **`ItemManipulationEvents` for UI State:** This bus is a dedicated channel for the UI. When the underlying data for items changes in the `Inventory` or `ShipState`, this bus informs the UI layer (specifically ViewModels like `PlayerPanelDataViewModel`) to refresh itself. This strictly enforces a one-way data flow from the game state to the UI.
*   **C# Events for Performance and Direct Coupling:** For high-frequency notifications like the 100ms combat tick, a direct C# event is optimal to avoid the overhead of a more complex bus. It's also ideal for cases where a parent controller needs to wire its children to a specific data instance, such as the `CombatController` subscribing UI elements to a specific `ShipState`'s `OnHealthChanged` event.

## Implementation Details

### 1. Global Event Bus (`EventBus.cs`)

*   **File Path:** `Assets/Scripts/Core/EventBus.cs`
*   **Implementation:** A static class containing a series of `public static event Action<...>` delegates. Each event has a corresponding public `Dispatch...()` method to invoke it.

**Key Events, Publishers, and Subscribers:**

| Event Name | Publisher(s) (Class) | Subscriber(s) (Class) | Notes |
| :--- | :--- | :--- | :--- |
| `OnBattleStart` | `CombatController` | `AbilityManager` | Signals the beginning of a battle. |
| `OnSuddenDeathStarted` | `CombatController` | `BattleUIController` | Fired when sudden death begins. |
| `OnTick` | `CombatController` | `AbilityManager` | General combat tick for ability processing. |
| `OnDamageReceived` | `ShipState` | `AbilityManager`, `ShipStateView` | Fired when a ship takes damage. |
| `OnHeal` | `ShipState` | `AbilityManager`, `ShipStateView` | Fired when a ship is healed. |
| `OnDamageDealt` | *(Not currently dispatched)* | `AbilityManager` | Defined but unused. |

### 2. Item Manipulation Event Bus (`ItemManipulationEvents.cs`)

*   **File Path:** `Assets/Scripts/Core/ItemManipulationEvents.cs`
*   **Implementation:** A static class similar to `EventBus`, but with events specific to item slot changes.

**Key Events, Publishers, and Subscribers:**

| Event Name | Publisher(s) (Class) | Subscriber(s) (Class) | Notes |
| :--- | :--- | :--- | :--- |
| `OnItemAdded` | `Inventory`, `ShipState` | `PlayerPanelController`, `InventoryUI` | An item has been added to a slot. |
| `OnItemRemoved` | `Inventory`, `ShipState` | `PlayerPanelController`, `InventoryUI` | An item has been removed from a slot. |
| `OnItemMoved` | `Inventory`, `ShipState` | `PlayerPanelController`, `EnemyPanelController`, `InventoryUI` | An item has moved between slots. |
| `OnRewardItemClaimed` | `ItemManipulationService` | `RewardUIController` | A reward item has been successfully claimed. |

### 3. Direct C# Instance Events

*   **Implementation:** Standard C# `event Action` properties on `MonoBehaviour` or plain C# classes.

**Key Events, Publishers, and Subscribers:**

| Event Name | Publisher (Class) | Subscriber(s) (Class) | Notes |
| :--- | :--- | :--- | :--- |
| `OnTick` | `TickService` | `CombatController` | Drives the main combat loop at a fixed 100ms interval. |
| `OnHealthChanged` | `ShipState` | `CombatController` (wires up UI), `ShipStateView` | Notifies listeners that a specific ship's health has changed, for direct UI updates. |

## Related Documents
*   [Combat System Overview]({{< myrelref "../combat/combat-system-overview.md" >}})
*   [UI Systems Overview]({{< myrelref "../ui/ui-systems.md" >}})
*   [Core Systems Overview]({{< myrelref "../core/_index.md" >}})

## Process Flowchart

This diagram outlines the flow of a typical `EventBus` event, from its origin in a game system to its consumption by decoupled subscribers.

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

title EventBus Communication Flow Example

|Publisher|
start
:A game event occurs;
note
  Example: A battle starts
  in `CombatController`.
end note
:Call static `EventBus.Dispatch<EventName>(args)`;

|EventBus|
:Receive dispatch call;
:Invoke corresponding `static event Action`;
note
  `OnBattleStart?.Invoke(context)`
end note
:Notify all subscribers;

|Subscribers|
fork
    |Subscriber A|
    :Receive notification;
    :Execute handler method;
    note
      `AbilityManager.HandleBattleStart()`
      registers abilities for the fight.
    end note

    |Subscriber B|
    :Receive notification;
    :Execute handler method;
    note
      A UI controller plays a
      "Battle Start!" animation.
    end note
fork again
    |...|
    :Other subscribers execute
    their handlers in parallel;
end fork

stop
@enduml
```
