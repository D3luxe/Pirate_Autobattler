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

*   **`PirateRoguelike.Core.GameSession` (`Assets/Scripts/Core/GameSession.cs`):**
    *   **Responsibility:** The central static repository for the current game run's state (player ship, inventory, economy, map progress). It orchestrates starting new runs, loading saves, and ending battles.
    *   **Interaction:** Provides data to all other systems and dispatches events when core game state changes.

*   **`PirateRoguelike.Core.RunManager` (`Assets/Scripts/Core/RunManager.cs`):**
    *   **Responsibility:** A persistent singleton that manages the overall game flow within a "run." It instantiates core game managers (like `Pirate.MapGen.MapManager`), handles scene transitions (e.g., to Battle, Shop), and reacts to player progress on the map. **It also centralizes the management of debug and save hotkey input actions.**
    *   **Interaction:** Orchestrates the initialization of `Pirate.MapGen.MapManager` and `PirateRoguelike.UI.UIManager`. Reacts to `PirateRoguelike.Core.GameSession` events (e.g., `OnPlayerNodeChanged`).

*   **`PirateRoguelike.UI.UIManager` (`Assets/Scripts/UI/UIManager.cs`):**
    *   **Responsibility:** A persistent singleton dedicated to instantiating and managing all global UI elements (player panel, map view, tooltips). It acts as the central point for UI initialization and display.
    *   **Interaction:** Receives explicit initialization calls from `PirateRoguelike.Core.GameInitializer` and `PirateRoguelike.Core.RunManager`. Provides references to UI controllers (e.g., `PirateRoguelike.UI.PlayerPanelController`) to other systems.

*   **`PirateRoguelike.Services.UIStateService` (`Assets/Scripts/Services/UIStateService.cs`):**
    *   **Responsibility:** A static class providing global UI state flags, such as `IsConsoleOpen`, to coordinate behavior across different UI components.
    *   **Interaction:** Used by UI components (e.g., `DebugConsoleController`, `SlotManipulator`) to query or set global UI states, preventing conflicts and ensuring proper interaction flow.

*   **`Pirate.MapGen.MapManager` (`Assets/Scripts/Map/MapManager.cs`):**
    *   **Responsibility:** Generates and manages the procedural map data for each run. Provides map structure (nodes, edges) and handles map-related events.
    *   **Interaction:** Created by `PirateRoguelike.Core.RunManager`. Provides map data to `PirateRoguelike.UI.MapView`. Dispatches `Pirate.MapGen.MapManager.OnMapDataUpdated` events.

*   **`PirateRoguelike.Combat.CombatController` (`Assets/Scripts/Combat/CombatController.cs`):**
    *   **Responsibility:** Manages a single battle simulation. Processes combat ticks, applies effects, and determines battle outcomes.
    *   **Interaction:** Driven by `PirateRoguelike.Core.TickService`. Interacts with `PirateRoguelike.Core.ShipState` and `PirateRoguelike.Core.AbilityManager`. Reports battle results to `PirateRoguelike.Core.GameSession`.

*   **Data Systems (`Assets/Scripts/Data/`):**
    *   **Responsibility:** Defines and loads all static game content (items, ships, enemies, abilities) using `ScriptableObject`s. Manages runtime representations of these data.
    *   **Interaction:** `PirateRoguelike.Core.GameDataRegistry` loads data at startup. Runtime data objects (`PirateRoguelike.Data.ItemInstance`, `PirateRoguelike.Core.ShipState`) are used by `PirateRoguelike.Core.GameSession`, `PirateRoguelike.Services.Inventory`, and combat systems.

*   **UI Systems (`Assets/Scripts/UI/`):**
    *   **Responsibility:** Manages all visual user interface elements. Utilizes UI Toolkit for component creation and data binding.
    *   **Interaction:** `PirateRoguelike.UI.PlayerPanelController` and `PirateRoguelike.UI.EnemyPanelController` act as adapters between game state and UI views. Components like `ShipDisplayElement` bind directly to view models.

## 4. Namespace Conventions

To maintain clarity and consistency throughout the documentation, the following namespace conventions are observed:

*   **Primary Root Namespace:** `PirateRoguelike`
    *   Most core game systems, UI components, data structures, and services reside under this root.
*   **Map Generation Namespace:** `Pirate.MapGen`
    *   This is a specific exception for all map generation and map data-related logic.
*   **Referencing Classes:**
    *   When a class is first mentioned within a major section (e.g., under a new heading), its full namespace will be provided (e.g., `PirateRoguelike.Core.GameSession`).
    *   For subsequent mentions within the same section, only the short class name will be used (e.g., `GameSession`), as the namespace context will have been established.
    *   In cases of potential ambiguity (e.g., two classes with the same short name but different namespaces), the full namespace will always be used for clarity.

This convention ensures that readers can quickly identify the origin of a class while keeping the overall text readable.

## 4. System Interaction and Data Flow

The systems interact primarily through a combination of:

*   **Explicit Initialization Calls:** The `PirateRoguelike.Core.GameInitializer` orchestrates the initial setup, explicitly calling `Initialize()` methods on `PirateRoguelike.Core.RunManager` and `PirateRoguelike.UI.UIManager` in a guaranteed sequence.
*   **Singleton Access:** Managers (e.g., `PirateRoguelike.Core.GameSession.Instance`, `Pirate.MapGen.MapManager.Instance`, `PirateRoguelike.UI.UIManager.Instance`) provide global access points for other systems to retrieve data or trigger actions.
*   **Event-Driven Communication (`PirateRoguelike.Core.EventBus`):** The `PirateRoguelike.Core.EventBus` is used for decoupled, one-to-many communication. Systems dispatch events (e.g., `OnPlayerNodeChanged`, `OnBattleStart`, `OnDamageReceived`), and other interested systems subscribe to react without direct dependencies.
*   **Data Binding (UI):** UI components often bind to ViewModels (`PlayerPanelDataViewModel`, `EnemyShipViewData`) which, in turn, observe changes in the `PirateRoguelike.Core.GameSession` or other game state. This ensures the UI automatically updates when underlying data changes.
*   **Direct Method Calls:** For tightly coupled operations (e.g., `PirateRoguelike.Combat.CombatController` calling `PirateRoguelike.Core.ShipState` methods), direct method calls are used.

### Example Flow: Starting a New Run

1.  **`PirateRoguelike.UI.MainMenuController`** triggers `SceneManager.LoadScene("Boot")`.
2.  **`PirateRoguelike.Core.GameInitializer.Start()`** in the `Boot` scene:
    *   Initializes `PirateRoguelike.Core.GameSession` (creating `PlayerShip`, `PirateRoguelike.Services.Inventory`, etc.).
    *   Instantiates `PirateRoguelike.Core.RunManager` and calls `PirateRoguelike.Core.RunManager.Instance.Initialize()`.
    *   Instantiates `PirateRoguelike.UI.UIManager` and calls `PirateRoguelike.UI.UIManager.Instance.Initialize()`.
    *   Loads `Run.unity`.
3.  **`PirateRoguelike.Core.RunManager.OnRunSceneLoaded()`** (triggered by `Run.unity` loading):
    *   Ensures `Pirate.MapGen.MapManager` generates map data.
    *   Calls `PirateRoguelike.UI.UIManager.Instance.InitializeRunUI()`.
4.  **`PirateRoguelike.UI.UIManager.InitializeRunUI()`**:
    *   Initializes `PirateRoguelike.UI.PlayerPanelController` (which sets up its ViewModel and View).
    *   Calls `PirateRoguelike.UI.MapView.Show()` to display the map.
5.  **`PlayerPanelDataViewModel`** (initialized by `PirateRoguelike.UI.PlayerPanelController`):
    *   Subscribes to `PirateRoguelike.Core.GameSession` events (e.g., `OnPlayerShipInitialized`).
    *   Manually updates its properties from `PirateRoguelike.Core.GameSession` (e.g., `ShipName`, `CurrentHp`, `Gold`).
    *   The `PlayerPanelView` (and its `ShipDisplayElement`) automatically update via data binding.

## 5. Conclusion

## Related Documents

*   [Core Systems Overview]({{< myrelref "systems/core/_index.md" >}})
*   [Combat Systems Overview]({{< myrelref "systems/combat/_index.md" >}})
*   [Data Systems Overview]({{< myrelref "systems/data/_index.md" >}})
*   [UI Systems Overview]({{< myrelref "systems/ui/_index.md" >}})
*   [Map Systems Overview]({{< myrelref "systems/map/_index.md" >}})
*   [Shop System Overview]({{< myrelref "systems/crosscutting/shop-system-overview.md" >}})
*   [Crosscutting Systems Overview]({{< myrelref "systems/crosscutting/_index.md" >}})

The Pirate Autobattler's system architecture prioritizes clear responsibilities and flexible communication. While managers provide central control, the extensive use of events and data binding ensures that components remain decoupled, promoting maintainability and scalability. This foundation allows for complex gameplay interactions to be built upon a robust and understandable framework.
