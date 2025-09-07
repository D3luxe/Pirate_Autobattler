---
title: "Combat System Overview"
linkTitle: "Combat Overview"
weight: 10
system: ["combat"]
types: ["system-overview"]
status: "approved"
---

## 1. System Overview

The combat system in Pirate Autobattler is a real-time, tick-based simulation orchestrated by the `CombatController`. It manages the interaction between the player's ship and an enemy ship, driven by a consistent game tick and a robust event system.

## 2. Core Components

*   **`CombatController` (`PirateRoguelike.Combat.CombatController`):**
    >   File Path: Assets/Scripts/Combat/CombatController.cs
    The central manager for a single battle. It handles battle initialization, processes each combat tick, applies effects, and determines battle end conditions.

*   **`TickService` (`PirateRoguelike.Core.TickService`):**
    >   File Path: Assets/Scripts/Core/TickService.cs
    A `MonoBehaviour` that provides a consistent, fixed-interval (100ms) tick to drive the combat simulation. The `CombatController` subscribes to its `OnTick` event.

*   **`ShipState` (`PirateRoguelike.Core.ShipState`):**
    >   File Path: Assets/Scripts/Core/ShipState.cs
    Represents the runtime state of a ship (player or enemy). It manages:
    *   `CurrentHealth`, `CurrentShield`
    *   `ActiveEffects` (a list of `ActiveCombatEffect` instances)
    *   `Equipped` items (`ItemInstance[]`)
    *   `_stunDuration` and `_activeStatModifiers`
    It also provides methods for `TakeDamage`, `Heal`, `ApplyEffect`, and dispatches `OnHealthChanged` for UI updates.

*   **`ItemInstance` (`PirateRoguelike.Data.ItemInstance`):**
    >   File Path: Assets/Scripts/Combat/ItemInstance.cs
    Represents a runtime instance of an item. It holds a reference to its `ItemSO` definition and, crucially, contains a `RuntimeItem` which handles the item's dynamic abilities and actions. It also tracks `CooldownRemaining` and `StunDuration`.

*   **`RuntimeItem` (`PirateRoguelike.Runtime.RuntimeItem`):**
    >   File Path: Assets/Scripts/Runtime/RuntimeItem.cs
*   **`RuntimeAbility` (`PirateRoguelike.Runtime.RuntimeAbility`):**
    >   File Path: Assets/Scripts/Runtime/RuntimeAbility.cs
*   **`RuntimeAction` (`PirateRoguelike.Runtime.RuntimeAction`):**
    >   File Path: Assets/Scripts/Runtime/RuntimeAction.cs
    These classes form the [dynamic item system]({{< myrelref "../core/runtime-data-systems.md" >}}).
    *   `RuntimeItem` wraps an `ItemSO` and contains `RuntimeAbility` instances.
    *   `RuntimeAbility` wraps an `AbilitySO` and contains `RuntimeAction` instances.
    *   `RuntimeAction` is an abstract base class for concrete actions (e.g., `RuntimeDamageAction`, `RuntimeHealAction`, `RuntimeApplyEffectAction`).
    This system allows for dynamic modification of item properties and behaviors at runtime, which is critical for combat effects and tooltips.

*   **`EventBus` (`PirateRoguelike.Core.EventBus`):**
    >   File Path: Assets/Scripts/Core/EventBus.cs
    A static class for global event dispatching. It facilitates communication between various combat-related systems by broadcasting events like `OnBattleStart`, `OnDamageReceived`, and `OnHeal`.

*   **`AbilityManager` (`PirateRoguelike.Core.AbilityManager`):**
    >   File Path: Assets/Scripts/Core/AbilityManager.cs
    A static class responsible for managing and executing abilities based on various combat triggers. It subscribes to `EventBus` events to activate abilities from equipped items and other sources. For a detailed overview, refer to the [Ability Manager Overview]({{< myrelref "ability-manager-overview.md" >}}).

*   **[UI Components]({{< myrelref "../ui/ui-systems.md" >}}) (BattleUIController, EnemyPanelController, ShipStateView):**
    These components manage the visual display of combat information, subscribing to `ShipState.OnHealthChanged` and other events to keep the UI synchronized with the game state.

## 3. Combat Flow

### 3.1. Initialization

1.  `CombatController.Init` is called, providing the player's `ShipState`, enemy data (`EnemySO`), the `ITickService`, and references to various UI controllers.
2.  The enemy's `ShipState` is created from the `EnemySO`, and its items are equipped.
3.  The `CombatController` subscribes to `_tickService.OnTick` to begin the combat loop.
4.  All necessary UI components (`BattleUIController`, `EnemyPanelController`, `ShipStateView`s) are initialized and subscribe to relevant `ShipState` events (e.g., `OnHealthChanged`).
5.  The `EventBus.DispatchBattleStart` event is invoked to signal the start of the battle to other systems (e.g., `AbilityManager`).
6.  `UIInteractionService.IsInCombat` is set to `true` to indicate the game is in a combat state.

### 3.3. Battle End

1.  If either ship's health is depleted, the `TickService` is stopped.
2.  The winner is determined (in case of a tie, the player wins).
3.  `GameSession.EndBattle` is called, passing the battle outcome to update the overall game state (e.g., awarding rewards, progressing the run).
4.  `UIInteractionService.IsInCombat` is set to `false` to indicate the game is no longer in a combat state, allowing UI interactions.

### 3.2. Tick Loop (`CombatController.HandleTick`)

The `HandleTick` method is the heart of the combat simulation, executed every 100ms by the `TickService`.

1.  **Sudden Death Check**: The battle duration is tracked. If it exceeds 30 seconds, "Sudden Death" is triggered, causing both ships to take increasing damage over time. `EventBus.DispatchSuddenDeathStarted` is broadcast.
2.  **Process Active Effects**: The `ProcessActiveEffects` method is called for both the player and enemy ships. This method:
    *   Iterates through each ship's `ActiveEffects`.
    *   Calls the `Tick()` method on each `ActiveCombatEffect`.
    *   If an effect has a `TickAction` defined, that action is executed (e.g., dealing damage over time, applying a buff).
    *   Expired effects are removed from the `ActiveEffects` list.
    *   Effects are processed in a sorted order based on their action type priority.
3.  **Reduce Stun**: The stun duration for both the player and enemy ships is reduced.
4.  **Dispatch General Tick Event**: `EventBus.DispatchTick` is broadcast, allowing other systems (like `AbilityManager` to process `OnItemReady` abilities and other time-based effects) to react to the general combat tick.
5.  **Check Battle End Conditions**: The system checks if either ship's `CurrentHealth` has dropped to zero or below.

### 3.3. Battle End

1.  If either ship's health is depleted, the `TickService` is stopped.
2.  The winner is determined (in case of a tie, the player wins).
3.  `GameSession.EndBattle` is called, passing the battle outcome to update the overall game state (e.g., awarding rewards, progressing the run).

## 4. Key EventBus Events (Combat-Related)

The `EventBus` plays a crucial role in decoupling combat systems.

| Event Name | Publisher(s) | Subscriber(s) (Known) | Notes |
| :--- | :--- | :--- | :--- |
| `OnBattleStart` | `CombatController:48` | `AbilityManager:48` | Signals the beginning of a battle. |
| `OnSuddenDeathStarted` | `CombatController:74` | `BattleUIController:41` | Fired when sudden death begins. |
| `OnTick` (EventBus) | `CombatController:99` | `AbilityManager:52` | General combat tick update for abilities and other systems. |
| `OnDamageReceived` | `ShipState:224` | `ShipView:18`, `AbilityManager:50` | Fired when a ship takes damage. |
| `OnHeal` | `ShipState:239` | `ShipView:19`, `AbilityManager:51` | Fired when a ship is healed. |
| `OnHealthChanged` (C# event) | `ShipState:102, 223, 238` | `CombatController:38, 39`, `ShipView:17`, `PlayerPanelController:87` | Direct C# event on `ShipState` for immediate UI updates. |

**Note on Unused Events:**
Several events are defined in `EventBus.cs` but are not currently dispatched by the `CombatController` or other core combat logic: `OnEncounterEnd`, `OnItemReady`, `OnAllyActivate`, `OnShieldGained`, `OnDebuffApplied`, `OnBuffApplied`. These may represent future features or incomplete implementations.

## 5. Important Data Structures

*   **`ShipState`**: Contains `Def` (ShipSO), `CurrentHealth`, `CurrentShield`, `ActiveEffects` (list of `ActiveCombatEffect`), `Equipped` items, `_stunDuration`, and `_activeStatModifiers`.
*   **`ItemInstance`**: Contains `Def` (ItemSO), `RuntimeItem`, `CooldownRemaining`, `StunDuration`.
*   **`ActiveCombatEffect`**: Represents an active effect on a ship, with a reference to its `EffectSO` definition, remaining duration, and stacks.
*   **`CombatContext`**: A simple struct used to pass `Caster` and `Target` `ShipState`s in combat events, providing context for abilities and actions.
*   **`StatModifier`**: Defines a modifier (flat or percentage) to a specific ship stat (e.g., Attack, Defense).

## 6. Interactions with Other Systems

*   **UI Integration**: The `CombatController` directly interacts with `BattleUIController` and `EnemyPanelController` for visual updates. `ShipState.OnHealthChanged` is a primary mechanism for UI synchronization, ensuring health bars and other indicators are always up-to-date.
*   **[Game Session Management]({{< myrelref "../core/save-load.md" >}})**: The `CombatController` calls `GameSession.EndBattle` to transition out of combat and update the overall game state (e.g., awarding rewards, progressing the run).
*   **[Data Loading]({{< myrelref "../data/data-systems-overview.md" >}})**: `GameDataRegistry` is used by `CombatController` and `ShipState` to retrieve `ShipSO` and `ItemSO` definitions when initializing ships and items.
*   **[Ability System]({{< myrelref "ability-manager-overview.md" >}})**: The `AbilityManager` subscribes to `EventBus` events (e.g., `OnBattleStart`, `OnTick`, `OnDamageReceived`, `OnHeal`) to trigger item abilities and other combat-related effects. For a detailed overview, refer to the [Ability Manager Overview]({{< myrelref "ability-manager-overview.md" >}}).

## 7. Performance Considerations

*   **Tick-based Allocations**: The `CombatController.ProcessActiveEffects` method currently creates new lists (`effectsToRemove`, `sortedActiveEffects`) and uses LINQ's `OrderBy` on every tick. While acceptable for a small number of effects, this can lead to increased memory allocations in a hot path, especially with many active effects. Future optimization could involve pooling lists or pre-sorting effects if performance becomes a bottleneck.

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

partition "Combat Initialization" #E3F2FD {
  :GameInitializer;
  :Load Battle Scene;
  :CombatController.Init(PlayerShip, EnemyData, TickService, UI Controllers);
  :Create Enemy ShipState;
  :CombatController subscribes to TickService.OnTick;
  :Initialize UI Components (BattleUIController, EnemyPanelController, ShipStateViews);
  :EventBus.DispatchBattleStart;
  :UIInteractionService.IsInCombat = true;
}

partition "Combat Tick Loop (CombatController.HandleTick)" #E8F5E9 {
  repeat
    :TickService.OnTick (every 100ms);
    :Sudden Death Check;
    :Process Active Effects (Player & Enemy);
    :Reduce Stun (Player & Enemy);
    :EventBus.DispatchTick;
    :AbilityManager processes OnItemReady abilities;
    :Check Battle End Conditions;
  repeat while (Battle not ended)

  :Battle Ends;
}

partition "Battle End" #FFF3E0 {
  :TickService.Stop;
  :Determine Winner;
  :GameSession.EndBattle(Outcome);
}

partition "Event-Driven Interactions" #F3E5F5 {
  :ShipState.OnHealthChanged --> BattleUIController;
  :ShipState.OnHealthChanged --> EnemyPanelController;
  :ShipState.OnHealthChanged --> PlayerPanelController;
  :ShipState.TakeDamage/Heal --> EventBus.DispatchDamageReceived/Heal;
  :EventBus.DispatchDamageReceived/Heal --> AbilityManager;
  :EventBus.DispatchBattleStart --> AbilityManager;
}

stop
@enduml

