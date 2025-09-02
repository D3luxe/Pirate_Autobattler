---
title: "Game System Flow Overview"
linkTitle: "Game Systems"
weight: 5
system: ["core"]
types: ["system-overview"]
status: "approved"
discipline: ["engineering"]
stage: ["Completed"]
type: "docs"
layout: "section"
---

## 1. Introduction

This document provides a high-level overview of the Pirate Autobattler game's core systems and how they interact to create the overall gameplay experience. The architecture emphasizes modularity, clear separation of concerns, and event-driven communication.

## 2. High-Level Game Loop

The game follows a distinct loop, transitioning between key scenes and states:

1.  **Main Menu (`MainMenu.unity`):** The entry point, allowing players to start a new game or load a saved one.
2.  **Boot Sequence (`Boot.unity`):** A hidden scene responsible for initializing all core game managers and systems.
3.  **Run Phase (`Run.unity`):** The primary gameplay loop where players navigate the map, encounter events, and manage their ship.
4.  **Battle Phase (`Battle.unity`):** Entered when the player encounters an enemy, simulating real-time combat.
5.  **Summary/Game Over (`Summary.unity`):** Displayed upon game completion or player defeat.

## 3. Core Systems and Responsibilities

The game's functionality is distributed among several key systems:

*   **`GameSession` (`Assets/Scripts/Core/GameSession.cs`):**
    *   **Responsibility:** The central static repository for the current game run's state (player ship, inventory, economy, map progress). It orchestrates starting new runs, loading saves, and ending battles.
    *   **Interaction:** Provides data to all other systems and dispatches events when core game state changes.

*   **`RunManager` (`Assets/Scripts/Core/RunManager.cs`):**
    *   **Responsibility:** A persistent singleton that manages the overall game flow within a "run." It instantiates core game managers (like `MapManager`), handles scene transitions (e.g., to Battle, Shop), and reacts to player progress on the map.
    *   **Interaction:** Orchestrates the initialization of `MapManager` and `UIManager`. Reacts to `GameSession` events (e.g., `OnPlayerNodeChanged`).

*   **`UIManager` (`Assets/Scripts/UI/UIManager.cs`):**
    *   **Responsibility:** A persistent singleton dedicated to instantiating and managing all global UI elements (player panel, map view, tooltips). It acts as the central point for UI initialization and display.
    *   **Interaction:** Receives explicit initialization calls from `GameInitializer` and `RunManager`. Provides references to UI controllers (e.g., `PlayerPanelController`) to other systems.

*   **`MapManager` (`Assets/Scripts/Map/MapManager.cs`):**
    *   **Responsibility:** Generates and manages the procedural map data for each run. Provides map structure (nodes, edges) and handles map-related events.
    *   **Interaction:** Created by `RunManager`. Provides map data to `MapView`. Dispatches `OnMapDataUpdated` events.

*   **`CombatController` (`Assets/Scripts/Combat/CombatController.cs`):**
    *   **Responsibility:** Manages a single battle simulation. Processes combat ticks, applies effects, and determines battle outcomes.
    *   **Interaction:** Driven by `TickService`. Interacts with `ShipState` and `AbilityManager`. Reports battle results to `GameSession`.

*   **Data Systems (`Assets/Scripts/Data/`):**
    *   **Responsibility:** Defines and loads all static game content (items, ships, enemies, abilities) using `ScriptableObject`s. Manages runtime representations of these data.
    *   **Interaction:** `GameDataRegistry` loads data at startup. Runtime data objects (`ItemInstance`, `ShipState`) are used by `GameSession`, `Inventory`, and combat systems.

*   **UI Systems (`Assets/Scripts/UI/`):**
    *   **Responsibility:** Manages all visual user interface elements. Utilizes UI Toolkit for component creation and data binding.
    *   **Interaction:** `PlayerPanelController` and `EnemyPanelController` act as adapters between game state and UI views. Components like `ShipDisplayElement` bind directly to view models.

## 4. System Interaction and Data Flow

The systems interact primarily through a combination of:

*   **Explicit Initialization Calls:** The `GameInitializer` orchestrates the initial setup, explicitly calling `Initialize()` methods on `RunManager` and `UIManager` in a guaranteed sequence.
*   **Singleton Access:** Managers (e.g., `GameSession.Instance`, `MapManager.Instance`, `UIManager.Instance`) provide global access points for other systems to retrieve data or trigger actions.
*   **Event-Driven Communication (`EventBus`):** The `EventBus` is used for decoupled, one-to-many communication. Systems dispatch events (e.g., `OnPlayerNodeChanged`, `OnBattleStart`, `OnDamageReceived`), and other interested systems subscribe to react without direct dependencies.
*   **Data Binding (UI):** UI components often bind to ViewModels (`PlayerPanelDataViewModel`, `EnemyShipViewData`) which, in turn, observe changes in the `GameSession` or other game state. This ensures the UI automatically updates when underlying data changes.
*   **Direct Method Calls:** For tightly coupled operations (e.g., `CombatController` calling `ShipState` methods), direct method calls are used.

### Example Flow: Starting a New Run

1.  **`MainMenuController`** triggers `SceneManager.LoadScene("Boot")`.
2.  **`GameInitializer.Start()`** in the `Boot` scene:
    *   Initializes `GameSession` (creating `PlayerShip`, `Inventory`, etc.).
    *   Instantiates `RunManager` and calls `RunManager.Instance.Initialize()`.
    *   Instantiates `UIManager` and calls `UIManager.Instance.Initialize()`.
    *   Loads `Run.unity`.
3.  **`RunManager.OnRunSceneLoaded()`** (triggered by `Run.unity` loading):
    *   Ensures `MapManager` generates map data.
    *   Calls `UIManager.Instance.InitializeRunUI()`.
4.  **`UIManager.InitializeRunUI()`**:
    *   Initializes `PlayerPanelController` (which sets up its ViewModel and View).
    *   Calls `MapView.Show()` to display the map.
5.  **`PlayerPanelDataViewModel`** (initialized by `PlayerPanelController`):
    *   Subscribes to `GameSession` events (e.g., `OnPlayerShipInitialized`).
    *   Manually updates its properties from `GameSession` (e.g., `ShipName`, `CurrentHp`, `Gold`).
    *   The `PlayerPanelView` (and its `ShipDisplayElement`) automatically update via data binding.

## 5. Conclusion

The Pirate Autobattler's system architecture prioritizes clear responsibilities and flexible communication. While managers provide central control, the extensive use of events and data binding ensures that components remain decoupled, promoting maintainability and scalability. This foundation allows for complex gameplay interactions to be built upon a robust and understandable framework.
