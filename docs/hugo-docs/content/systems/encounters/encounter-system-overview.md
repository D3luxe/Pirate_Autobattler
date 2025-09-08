---
title: "Encounter System Overview"
linkTitle: "Encounters"
weight: 10
system: ["core", "data", "ui"]
types: ["system-overview"]
status: "approved"
type: "docs"
cascade:
  type: "docs"
---

## 1. System Overview

The encounter system is a core part of the game that defines and manages various in-game events the player can face on the map. It's designed to be modular and data-driven, allowing for flexible content creation and diverse gameplay experiences.

## 2. Core Components

*   **`EncounterSO` (`Assets/Scripts/Data/EncounterSO.cs`):**
    *   A `ScriptableObject` that serves as the blueprint for all encounters.
    *   It defines common properties applicable to all encounter types, such as `id`, `weight` (for random selection), `isElite`, `eliteRewardDepthBonus`, `iconPath`, and `tooltipText`.
    *   It also contains type-specific fields, which are detailed in the "Encounter Types" section below.
    *   For `Event` type encounters, it includes `minFloor` and `maxFloor` to control when the event can appear, and a list of `EventChoice` objects.

*   **`EventChoice` (Defined within `EncounterSO`):**
    *   A serializable class representing a single choice a player can make within an `Event` encounter.
    *   It contains `choiceText`, `outcomeText`, and a `List<EventChoiceAction>`.

*   **`EventChoiceAction` (`Assets/Scripts/Data/EventChoiceAction.cs`):**
    *   An abstract `ScriptableObject` that serves as the base for all modular event actions.
    *   Concrete implementations (e.g., `GainResourceAction`, `ModifyStatAction`, `GiveItemAction`, `GiveShipAction`, `LoadEncounterAction`) define specific behaviors.
    *   Each `EventChoiceAction` has an `Execute(PlayerContext context)` method that performs its defined action.

*   **`PlayerContext` (`Assets/Scripts/Core/PlayerContext.cs`):**
    *   A class that encapsulates necessary game state and services (e.g., `IEconomyService`, `IInventoryService`, `IGameSessionService`, `IRunManagerService`) that `EventChoiceAction`s operate on. This provides a clean, interface-based way for actions to interact with the game state without direct dependencies on monolithic systems.

*   **`EventController` (`Assets/Scripts/UI/EventController.cs`):**
    *   This UI controller is responsible for handling the display and logic of `Event` encounters.
    *   When a player selects an `EventChoice`, the `EventController` instantiates a `PlayerContext` and then iterates through the `List<EventChoiceAction>` associated with that choice, calling `action.Execute(playerContext)` for each.
    *   **Note:** For development and testing, the `EventController` can load specific debug encounters via `GameSession.DebugEncounterToLoad`.

## 3. Encounter Types

The `EncounterSO` defines various types of encounters, each with specific properties and gameplay implications.

*   **`Battle`:**
    *   **Purpose:** A combat encounter where the player fights one or more enemies.
    *   **Relevant `EncounterSO` Fields:** `enemies` (List of `EnemySO`s to fight).

*   **`Shop`:**
    *   **Purpose:** Allows the player to purchase items and potentially a new ship.
    *   **Relevant `EncounterSO` Fields:** `shopItemCount` (Number of items available for purchase).

*   **`Port`:**
    *   **Purpose:** A safe haven where the player can heal their ship.
    *   **Relevant `EncounterSO` Fields:** `portHealPercent` (Percentage of max health healed).

*   **`Event`:**
    *   **Purpose:** Narrative-driven encounters with choices that lead to various outcomes.
    *   **Relevant `EncounterSO` Fields:**
        *   `eventTitle` (Title displayed in the event UI).
        *   `eventDescription` (Main text of the event).
        *   `eventUxml` (VisualTreeAsset for custom UI layout).
        *   `eventUss` (StyleSheet for custom UI styling).
        *   `minFloor`, `maxFloor` (Integer range defining the floors where this event can appear).
        *   `eventChoices` (List of `EventChoice` objects, each containing a list of `EventChoiceAction`s).

*   **`Treasure`:**
    *   **Purpose:** An encounter that directly grants rewards (e.g., gold, items) without player choice or combat.
    *   **Relevant `EncounterSO` Fields:** (Typically configured via `eliteRewardDepthBonus` and general reward generation rules, not direct fields on `EncounterSO` itself).

*   **`Boss`:**
    *   **Purpose:** A special, challenging battle encounter typically found at the end of a map or act.
    *   **Relevant `EncounterSO` Fields:** Similar to `Battle`, but often with unique `EnemySO`s and potentially specific rewards.

*   **`Unknown`:**
    *   **Purpose:** A placeholder or default type for encounters that are not yet defined or are used for error handling.

## 4. Modular Event System

The game's event encounters have been refactored to use a modular, data-driven system, allowing designers to define complex event outcomes without requiring code changes. This system leverages `ScriptableObject`s to encapsulate specific actions.

### 4.1. Core Components

*   **`EventChoiceAction` (`Assets/Scripts/Data/EventChoiceAction.cs`):**
    *   An abstract `ScriptableObject` that serves as the base for all modular event actions. Concrete implementations (e.g., `GainResourceAction`, `ModifyStatAction`, `GiveItemAction`, `GiveShipAction`, `LoadEncounterAction`) define specific behaviors.
    *   Each `EventChoiceAction` has an `Execute(PlayerContext context)` method that performs its defined action.

*   **`PlayerContext` (`Assets/Scripts/Core/PlayerContext.cs`):**
    *   A class that encapsulates necessary game state and services (e.g., `IEconomyService`, `IInventoryService`, `IGameSessionService`, `IRunManagerService`) that `EventChoiceAction`s operate on. This provides a clean, interface-based way for actions to interact with the game state without direct dependencies on monolithic systems.

*   **`EventController` (`Assets/Scripts/UI/EventController.cs`):**
    *   When a player selects an `EventChoice`, the `EventController` instantiates a `PlayerContext` and then iterates through the `List<EventChoiceAction>` associated with that choice.
    *   For each action, it calls `action.Execute(playerContext)`, triggering the defined outcome.

### 4.2. How it Works

1.  **Designer Defines Actions:** Designers create `EventChoiceAction` ScriptableObjects (e.g., "Gain 50 Gold Action", "Heal 10 HP Action") and configure their specific parameters in the Unity Editor.
2.  **Actions Assigned to Choices:** In the `EncounterEditorWindow`, designers assign a list of these `EventChoiceAction`s to an `EventChoice` within an `EncounterSO`.
    *   **Editor Integration:** The `EncounterEditorWindow` dynamically generates UI fields for the specific properties of each `EventChoiceAction` type (e.g., `resourceType` and `amount` for `GainResourceAction`). This powerful feature reinforces the data-driven nature of the system.
    *   **Sub-Asset Management:** `EventChoiceAction` instances are created as *sub-assets* of the `EncounterSO` directly within the editor. This ensures proper asset organization and persistence.
3.  **Player Selects Choice:** During gameplay, when a player selects an `EventChoice` in an event encounter.
4.  **`EventController` Executes Actions:** The `EventController` retrieves the list of `EventChoiceAction`s for the selected choice and executes each one sequentially, passing a `PlayerContext` that provides access to relevant game services.

### 4.3. Benefits

*   **Data-Driven Outcomes:** Event outcomes are defined as data assets, eliminating the need for code changes for new event behaviors.
*   **Modularity and Reusability:** Individual actions are reusable across multiple event choices and encounters.
*   **Decoupling:** Event outcomes are decoupled from the `EventChoice` data structure, making the system more flexible and maintainable.
*   **Improved Designer Workflow:** The `EncounterEditorWindow` provides a dedicated and intuitive interface for managing these modular actions.

```plantuml
@startuml
' --- STYLING ---
skinparam style strictuml
skinparam shadowing true
skinparam defaultFontName "Segoe UI"
skinparam defaultFontSize 16
skinparam linetype ortho
skinparam ArrowFontName Impact
skinparam ArrowThickness 1
skinparam ArrowColor #000000
skinparam backgroundColor #b4b4b42c

skinparam class {
    BackgroundColor WhiteSmoke
    BorderColor #666666
    ArrowColor #1d1d1dff
    !option handWritten true
}
skinparam package {
    BorderColor #555555
    FontColor #333333
    StereotypeFontColor #333333
}

title Modular Event System Flow

actor Designer
participant "EncounterEditorWindow" as Editor
box "Game Data" #LightYellow
    participant "EncounterSO" as EncounterSO
    participant "EventChoice" as EventChoice
    participant "EventChoiceAction" as EventChoiceAction
end box
participant "EventController" as Controller
box "Game Services" #LightBlue
    participant "PlayerContext" as PlayerContext
    participant "EconomyService" as Economy
    participant "InventoryService" as Inventory
    participant "GameSessionService" as GameSession
    participant "RunManagerService" as RunManager
end box

Designer -> Editor : Configures Event
Editor -> EncounterSO : Saves EventChoiceAction references
EncounterSO --> EventChoice : Contains EventChoices
EventChoice --> EventChoiceAction : Contains EventChoiceActions

activate Controller
Controller -> PlayerContext : Instantiates with services
PlayerContext --> Economy : Provides IEconomyService
PlayerContext --> Inventory : Provides IInventoryService
PlayerContext --> GameSession : Provides IGameSessionService
PlayerContext --> RunManager : Provides IRunManagerService

Controller -> EventChoice : Player selects choice
loop For each EventChoiceAction in selected choice
    Controller -> EventChoiceAction : Execute(PlayerContext)
    activate EventChoiceAction
    EventChoiceAction -> PlayerContext : Requests service (e.g., Economy.AddGold)
    PlayerContext -> Economy : Calls service method
    deactivate EventChoiceAction
end loop
deactivate Controller

@enduml