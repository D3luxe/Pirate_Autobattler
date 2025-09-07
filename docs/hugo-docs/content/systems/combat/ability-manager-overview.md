---
title: "Ability Manager Overview"
weight: 15
system: ["combat", "core"]
types: ["system-overview"]
---

## Overview

The `AbilityManager` (`AbilityManager.cs`) is a static, centralized system responsible for managing the registration and execution of abilities within the combat system. It acts as a dispatcher, listening for various combat events and triggering associated abilities from equipped items and other sources.

## Design

The `AbilityManager` is designed as a static class, making it globally accessible without requiring instantiation. Its core design principles revolve around an event-driven architecture and efficient ability lookup.

*   **Static Singleton:** As a static class, it provides a single, global point of access for ability management.
*   **Event-Driven:** It subscribes to key events from the `EventBus` to react to combat occurrences (e.g., battle start, damage dealt, healing, tick updates).
*   **Ability Registry (`_activeAbilities`):** It maintains a dictionary (`Dictionary<TriggerType, List<AbilitySO>> _activeAbilities`) that maps specific `TriggerType`s to a list of `AbilitySO`s. This allows for quick lookup and execution of relevant abilities when a trigger event occurs.
*   **Dynamic Registration:** Abilities are dynamically registered at the start of a battle based on the equipped items of both the player and enemy ships.

## Implementation Details

### Core Components

*   **`AbilityManager` (`Assets/Scripts/Core/AbilityManager.cs`):**
    *   **`Initialize()`:** Called once at game startup (e.g., by `GameInitializer`). It subscribes the `AbilityManager`'s event handlers to relevant `EventBus` events.
    *   **`Shutdown()`:** Unsubscribes from `EventBus` events and clears the `_activeAbilities` dictionary, typically called when a battle ends or the game shuts down.
    *   **`RegisterAbilities(IEnumerable<AbilitySO> abilities)`:** A private helper method that populates the `_activeAbilities` dictionary. It iterates through a collection of `AbilitySO`s and adds them to the list associated with their respective `TriggerType`.
    *   **Event Handlers (`HandleBattleStart`, `HandleDamageDealt`, `HandleDamageReceived`, `HandleHeal`, `HandleTick`):**
        *   These static methods are subscribed to `EventBus` events.
        *   `HandleBattleStart(CombatContext ctx)`: Clears previously registered abilities, then iterates through the equipped items of both the `Caster` (player) and `Target` (enemy) `ShipState`s in the provided `CombatContext`. It calls `RegisterAbilities` for all abilities found on these items. Finally, it executes any abilities with the `OnBattleStart` trigger.
        *   `HandleTick(ShipState playerShip, ShipState enemyShip, float deltaTime)`: This is a crucial handler for time-based abilities. It iterates through the equipped items of both the player and enemy ships. For each active item, it manages its `CooldownRemaining`. If an item's cooldown reaches zero, it executes all abilities with the `OnItemReady` trigger associated with that item, and then resets the item's cooldown.

            **Note:** This process is driven by the global `OnTick` event. The AbilityManager does not use a separate `OnItemReady` event, but rather checks for the `TriggerType.OnItemReady` on an item's abilities once its cooldown, which it tracks internally, expires.

        *   `HandleDamageDealt(ShipState caster, ShipState target, float amount)`: Creates a `CombatContext` and calls `CheckAndExecuteAbilities` for `TriggerType.OnDamageDealt`.
        *   `HandleDamageReceived(ShipState target, float amount)`: Creates a `CombatContext` and calls `CheckAndExecuteAbilities` for `TriggerType.OnDamageReceived`.
        *   `HandleHeal(ShipState target, float amount)`: Creates a `CombatContext` and calls `CheckAndExecuteAbilities` for `TriggerType.OnHeal`.
    *   **`CheckAndExecuteAbilities(TriggerType trigger, CombatContext ctx)`:** A private helper method that retrieves all `AbilitySO`s associated with the given `TriggerType` from the `_activeAbilities` dictionary. It then iterates through these abilities and executes each of their `ActionSO`s using the provided `CombatContext`.

### Interactions with Other Systems

*   **`EventBus`:** The primary mechanism for `AbilityManager` to receive combat events.
*   **`ShipState`:** Abilities often interact with `ShipState` (e.g., applying effects, dealing damage, healing). `AbilityManager` accesses equipped items from `ShipState` to register abilities.
*   **`CombatContext`:** Provides the necessary context (caster, target, damage/heal amounts) for abilities to execute their actions correctly.
*   **`AbilitySO` / `ActionSO`:** These ScriptableObjects define the abilities and their actions that `AbilityManager` registers and executes.
*   **`ItemInstance`:** `AbilityManager` processes abilities from `ItemInstance`s equipped on `ShipState`s, particularly for `OnItemReady` triggers and cooldown management.

## Process Flowchart

This diagram illustrates the lifecycle of an ability, from its registration to its execution based on a combat event.

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

partition "Ability Manager Lifecycle" #E3F2FD {
  :GameInitializer calls AbilityManager.Initialize();
  :AbilityManager subscribes to EventBus events;
}

partition "Ability Registration (OnBattleStart)" #E8F5E9 {
  :EventBus.OnBattleStart event received by HandleBattleStart;
  :Clear _activeAbilities;
  :For each equipped item on Player and Enemy ShipState;
  :RegisterAbilities(item.Def.abilities);
  :Execute OnBattleStart abilities;
}

partition "Ability Execution (General Flow)" #FFF3E0 {
  :EventBus event (e.g., OnDamageDealt, OnHeal, OnTick) received;
  :Corresponding HandleEvent method called;
  :Create CombatContext;
  :CheckAndExecuteAbilities(TriggerType, CombatContext);
  if (_activeAbilities contains TriggerType?) then (Yes)
    :For each AbilitySO for TriggerType;
    :For each ActionSO in AbilitySO;
    :ActionSO.Execute(CombatContext);
  else (No)
    :No abilities for this trigger;
  endif
}

partition "OnItemReady Abilities (HandleTick Specific)" #F3E5F5 {
  :EventBus.OnTick event received by HandleTick;
  :For each equipped item on Player and Enemy ShipState;
  if (Item is active and CooldownRemaining <= 0?) then (Yes)
    :Execute OnItemReady abilities for item;
    :Reset item cooldown;
  else (No)
    :Reduce item cooldown;
  endif
}

stop
@enduml
