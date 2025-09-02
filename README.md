# Pirate Autobattler Project

This is a Unity project scaffold for a 2D pirate-themed roguelike autobattler. It has been structured for clean architecture, data-driven content, and extensibility.

## Project Overview

*   **Unity Version:** 6000.2.0f1
*   **Critical Packages:**
    *   `com.unity.inputsystem`
    *   `com.unity.ui.builder`
    *   `com.unity.uitoolkit`
*   **Main Scenes:** `MainMenu`, `Boot`, `Run`, `Battle`, `Summary`
*   **ASMDEFs:** None in `Assets/Scripts` (ensuring a flat assembly structure for now)
*   **Scripting Define Symbols:** `UNITY_EDITOR` (used for editor-specific code paths)

## Getting Started

The project is set up but requires initial content and scenes to be generated. Follow these steps in the Unity Editor:

1.  **Generate Scenes:**
    *   Go to the menu `Game > Tools > Create Scene Structure`.
    *   This will create all the necessary empty scenes (`MainMenu`, `Run`, `Battle`, etc.) in the `Assets/Scenes` folder.

2.  **Generate Game Data:**
    *   Go to the menu `Game > Tools > Create Starter Content`.
    *   This will create the starter set of `ScriptableObject` assets in the `Assets/GameData` folder. This includes:
        *   `RunConfiguration.asset`: The central configuration for game balance.
        *   Starter items (Wooden Cannon, Deckhand, etc.).
        *   Starter ships (Brigantine, Ironclad, Corsair) and one event ship.
        *   An example event encounter.

3.  **Open the Main Scene:**
    *   Open the `Assets/Scenes/Run.unity` scene. This is intended to be the main scene for gameplay where the map and player HUD will be.

## Project Structure Overview

*   **/Assets/Art, /Audio, /Prefabs, /Scenes, /Resources**: Standard Unity folders for assets.
*   **/Assets/GameData**: Contains all the `ScriptableObject` assets that define the game's content (Items, Ships, Encounters, Configs).
*   **/Assets/Scripts**: Contains all C# code, organized by feature.
    *   `/Core`: Core systems like `GameSession`, `RunManager`, `MapManager`, `GameInitializer`, `TickService`, `GameDataRegistry`, and `EventBus`.
    *   `/Combat`: Combat logic, including `CombatController`, `ShipState`, and `AbilityManager`.
    *   `/Data`: C# definitions for all `ScriptableObject` types (e.g., `ItemSO`, `ShipSO`, `EncounterSO`).
    *   `/Runtime`: Dynamic runtime representations of items, abilities, and actions (`ItemInstance`, `RuntimeItem`, `RuntimeAbility`, `RuntimeAction`).
    *   `/Saving`: The `SaveManager` and `RunState` definition for persistence.
    *   `/UI`: UI-related scripts, including `TooltipController`, `EffectDisplay`, `EnemyPanelController`, and various UI Toolkit components.
*   **/Assets/Editor**: Contains all editor-specific scripts, including `ContentTools` for setup and custom inspectors.
*   **/Assets/UI**: Contains UI Toolkit assets (UXML, USS).

## Key Systems & Concepts

*   **Core Systems:** Manages game lifecycle, session state, and core services. Includes `GameSession` (static, central state), `RunManager` (main game flow), `MapManager` (map generation), `GameInitializer` (startup), `TickService` (fixed updates), `GameDataRegistry` (loads ScriptableObjects), and `EventBus` (global event dispatcher).
*   **Combat System:** Handles turn-based player vs. enemy battles. Orchestrated by `CombatController`, driven by `TickService`. Manages `ShipState` and utilizes `ItemInstance` with `Runtime` objects for dynamic item behaviors.
*   **Data Systems:** Static game data is defined by `ScriptableObject` assets and loaded at startup by `GameDataRegistry`. Dynamic runtime data is managed by C# classes like `ItemInstance`, `RuntimeItem`, `RuntimeAbility`, and `RuntimeAction`. Game state persistence is handled by a JSON-based save/load system (`SaveManager`, `GameSession`, `RunState`).
*   **UI Systems:** Manages UI using UI Toolkit, including `TooltipController`, `PlayerPanelController`, `MapView`, and `BattleUIController`. Features a universal item manipulation system.
*   **Event-Driven Architecture:** The `EventBus` is a central component for decoupling systems, broadcasting events like `OnBattleStart`, `OnDamageReceived`, and `OnHeal`.
*   **Initialization Order:** A precise system initialization order ensures data readiness and proper inter-system communication.

## Documentation Structure

The project's documentation is organized into the `docs` directory, with subdirectories for different types of documents. This documentation is built using Hugo.

*   **`/docs/analysis`**: Contains detailed analyses, root cause investigations, and summaries of specific systems or problems.
*   **`/docs/tasks`**: Contains plans and specific task-related documentation for features, refactors, or bug fixes.
*   **`/docs/systems`**: Contains overviews, design documents, and explanations of core game functionality and systems.

This structure aims to provide a clear and consistent way to locate relevant information within the project.

## How to Add New Content (Data-Driven)

This project is designed to be data-driven. To add new content, you should primarily be creating new assets, not writing new code.

*   **To Add a New Item:**
    1.  In the Project window, navigate to `Assets/GameData/Items`.
    2.  Right-click and choose `Create > Pirate > Data > Item`.
    3.  Fill out the properties in the Inspector.
    4.  The game's systems will automatically pick up the new item where appropriate (e.g., in shops).

*   **To Add a New Ship:**
    1.  Navigate to `Assets/GameData/Ships`.
    2.  Right-click and choose `Create > Pirate > Data > Ship`.
    3.  Fill out the properties.

## List of Key Tunables

The primary balancing and configuration values can be found in one place:

*   **`Assets/GameData/RunConfiguration.asset`**

Select this asset in the Project window to modify the following values in the Inspector:
*   Encounter count per run.
*   Shop frequency.
*   Gold rewards.
*   Reroll costs.
*   Item and ship costs.
*   Port healing percentage.
*   Inventory size.

This allows for rapid iteration on game balance without needing to change any code.

## Known Issues & Recommendations

*   **`GameSession` Static State:** The static global state of `GameSession` hinders testability and complicates save/load. Refactoring it into a non-static class/MonoBehaviour is recommended for improved testability and explicit data flow.
*   **UI Coupling:** Direct UI updates (e.g., `CombatController`, `PlayerPanelController` subscribing to `ShipState`) tightly couple game logic and UI. Introducing a data-binding/view-model layer is recommended to decouple UI from game logic.
*   **Unused Assets/Events:** `StreamingAssets/items.json` is present but unused, and several `EventBus` events are defined but never dispatched. These create codebase clutter and confusion and should be removed.
*   **Runtime Action State Not Saved:** The current save system does not persist changes made to the mutable properties within `RuntimeAction` instances, leading to loss of dynamic item modifications upon loading. A detailed proposed solution is available in the documentation.

## Roadmap (High-Level)

*   **Quick Wins:** Remove unused `items.json` and `EventBus` events. Optimize `ProcessActiveEffects` to reduce per-tick allocations.
*   **Near Term:** Implement a view-model layer for UI.
*   **Mid Term:** Refactor `GameSession` to be a non-static class.

## Further Documentation

For more in-depth information on specific systems, design decisions, and detailed analyses, please refer to the `docs` directory. Key documents include:

*   **High-Level Diagram:** A Mermaid diagram illustrating the overall application flow.
*   **System Initialization Order:** Detailed sequence of system, view model, and UI component initialization.
*   **Combat Tick Walkthrough:** Step-by-step explanation of the combat simulation.
*   **Event Catalog:** Comprehensive list of `EventBus` events, their publishers, and subscribers.
*   **Data Model:** Overview of `ScriptableObject` data structures.
*   **Glossary & File Index:** Definitions of key terms and a quick reference for important files.