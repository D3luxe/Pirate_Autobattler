# Gemini Code Assistant Context

## Project: Pirate Autobattler

This is a Unity project for a 2D pirate-themed roguelike autobattler. The project is built with a data-driven architecture, using Unity's `ScriptableObject` system to manage game content like items, ships, and encounters.

### Key Technologies

*   **Engine:** Unity 2022.3.20f1
*   **Language:** C#
*   **Core Libraries:** Unity Input System, Unity UI (UIToolkit)

---

## Architecture Overview

The project follows a data-driven and event-driven architecture. This allows for a clean separation of concerns and makes it easy to add new content and features.

### Data Flow

The game's content is primarily defined using `ScriptableObject`s. These are assets that can be created and edited in the Unity Editor, allowing designers to easily tweak game balance and add new content without writing any code.

```mermaid
graph TD
    A[ScriptableObjects <br> (ItemSO, ShipSO, etc.)] --> B{GameDataRegistry};
    B --> C[Game Logic <br> (CombatController, etc.)];
    D[ContentTools.cs <br> (Editor Script)] -- Creates --> A;
    E[items.json <br> (StreamingAssets)] -.-> F[Potentially Legacy or<br>Unused Data Source];
```

*   **`ScriptableObject`s (`ItemSO`, `ShipSO`, etc.):** The single source of truth for game data. They are stored in `Assets/Resources/GameData`.
*   **`ContentTools.cs`:** An editor script that provides a convenient way to create the initial set of `ScriptableObject`s.
*   **`GameDataRegistry`:** A static class (presumably) that loads all `ScriptableObject`s from the `Resources` folder and provides easy access to them for the rest of the game.
*   **`items.json`:** This file exists in `StreamingAssets` but does not appear to be used by the core game logic. It might be a remnant of a previous system or used for a different purpose.

### Combat Loop

The combat system is driven by the `TickService`, which runs at a fixed interval (100ms). The `CombatController` orchestrates the battle, processing actions and effects for both the player and the enemy.

```mermaid
graph TD
    A[TickService] -- OnTick (100ms) --> B[CombatController.HandleTick];
    B --> C{Process Effects};
    B --> D{Process Cooldowns};
    B --> E{Check End Conditions};
    C -- Modifies --> F[ShipState (Player & Enemy)];
    D -- Modifies --> F;
    G[ItemInstance] -- Has --> H[ItemSO];
    F -- Has --> G;
    I[AbilitySO] -- Triggered by EventBus --> C;
    H -- Has --> I;
```

*   **`TickService`:** A `MonoBehaviour` that invokes an `OnTick` event every 100ms.
*   **`CombatController`:** The central class for managing a battle. It listens to the `TickService`'s `OnTick` event to drive the combat simulation.
*   **`ShipState`:** A class that holds the runtime state of a ship, including its health, equipped items, and active effects.
*   **`ItemInstance`:** A wrapper around an `ItemSO` that holds the runtime state of an item, such as its current cooldown.
*   **`EventBus`:** A global event dispatcher that allows different systems to communicate without being directly coupled. Abilities (`AbilitySO`) are triggered by events from the `EventBus`.

### UI and Game State Synchronization

The UI is built with Unity's UI Toolkit and is kept in sync with the game state through an event-based system. The `PlayerPanelController` acts as the bridge between the game logic and the UI.

```mermaid
graph TD
    A[Game Events <br> (OnHealthChanged, OnGoldChanged, etc.)] --> B[PlayerPanelController];
    B --> C[PlayerPanelDataViewModel];
    C --> D[PlayerPanelView];
    D -- Renders --> E[UI (UIToolkit)];
    F[UI Events <br> (OnSlotDropped)] --> B;
    B -- Updates --> G[Game State <br> (GameSession)];
```

*   **`PlayerPanelController`:** A `MonoBehaviour` that listens to game events (e.g., `EconomyService.OnGoldChanged`, `PlayerShip.OnHealthChanged`) and updates the UI accordingly. It also listens to UI events (e.g., `PlayerPanelEvents.OnSlotDropped`) and updates the game state in response.
*   **`PlayerPanelDataViewModel`:** An adapter class that converts the game state from the `GameSession` into a format that is easy for the `PlayerPanelView` to consume.
*   **`PlayerPanelView`:** The view class that is responsible for rendering the UI using the UI Toolkit. It takes data from the `PlayerPanelDataViewModel` and updates the visual elements.
*   **`GameSession`:** A static class that holds the current state of the game, including the player's ship, inventory, and economy.

---

## How to Add New Content

This project is designed to be data-driven. To add new content, you should primarily be creating new assets, not writing new code.

*   **To Add a New Item:**
    1.  In the Project window, navigate to `Assets/Resources/GameData/Items`.
    2.  Right-click and choose `Create > Pirate > Data > Item`.
    3.  Fill out the properties in the Inspector.
    4.  The game's systems will automatically pick up the new item where appropriate (e.g., in shops).

*   **To Add a New Ship:**
    1.  Navigate to `Assets/Resources/GameData/Ships`.
    2.  Right-click and choose `Create > Pirate > Data > Ship`.
    3.  Fill out the properties.
