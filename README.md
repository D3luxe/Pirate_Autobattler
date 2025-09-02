# Pirate Autobattler Project

This is a Unity project scaffold for a 2D pirate-themed roguelike autobattler. It has been structured for clean architecture, data-driven content, and extensibility.

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
    *   `/Core`: Core systems like the `TickService` and `EconomyService`.
    *   `/Combat`: Combat logic, including `CombatController`, `ShipState`, and `IEffect` implementations.
    *   `/Data`: The C# definitions for all `ScriptableObject` types.
    *   `/Items`, `/Ships`, `/Encounters`, `/Map`, `/UI`: Feature-specific logic.
    *   `/Saving`: The `SaveManager` and `RunState` definition for persistence.
*   **/Assets/Editor**: Contains all editor-specific scripts, including the `ContentTools` script used for setup.

## Documentation Structure

The project's documentation is organized into the `docs` directory, with subdirectories for different types of documents:

*   **`/docs/analysis`**: Contains detailed analyses, root cause investigations, and summaries of specific systems or problems. Files are prefixed with `analysis-`.
*   **`/docs/tasks`**: Contains plans and specific task-related documentation for features, refactors, or bug fixes. Files are prefixed with `task-`.
*   **`/docs/systems`**: Contains overviews, design documents, and explanations of core game functionality and systems. Files are prefixed with `system-`.

This structure aims to provide a clear and consistent way to locate relevant information within the project.

## How to Add New Content

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
