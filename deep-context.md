# Project Deep Context (v2)

- **Snapshot:**
  - **Unity Version:** 2022.3.20f1 (Assumed from project files, verification needed)
  - **Critical Packages:**
    - `com.unity.inputsystem`
    - `com.unity.ui.builder`
    - `com.unity.uitoolkit`
  - **Scenes in Build:** `MainMenu`, `Boot`, `Run`, `Battle`, `Summary` (Inferred from code)
  - **ASMDEFs:** None found in `Assets/Scripts`
  - **Scripting Define Symbols:** `UNITY_EDITOR` (in `MainMenuController.cs`)

- **High-Level Diagram (Mermaid):**

```mermaid
graph TD
    subgraph App Start
        A[MainMenu.unity] --> B{MainMenuController};
    end

    subgraph Game Initialization
        B -- OnStartGameClicked --> C[SceneManager.LoadScene("Boot")];
        C --> D[Boot.unity];
        D --> E{GameInitializer};
        E --> F[GameSession.StartNewRun / LoadRun];
        E -- Instantiates --> G((RunManager));
        G -- Instantiates --> H((MapManager));
        E --> I[SceneManager.LoadScene("Run")];
    end

    subgraph Main Game Loop
        I --> J[Run.unity];
        J --> K{RunManager};
        K -- Manages --> L{MapView};
        K -- Manages --> M{PlayerPanelController};
        L -- OnNodeClicked --> N[Start Encounter];
    end

    subgraph Combat
        N -- Loads --> O[Battle.unity];
        O --> P{BattleManager};
        P --> Q{CombatController};
        R((TickService)) -- OnTick (100ms) --> Q;
        Q -- Updates --> S{ShipState};
        Q -- Updates --> T{BattleUIController};
    end

    subgraph Global Systems
        U[GameDataRegistry] -- Provides --> AllSystems[All Systems];
        V[EventBus] -- Mediates --> AllSystems;
        W[GameSession] -- Holds --> X[RunState];
    end
```

- **Module Overviews:**
  - **Core:** Manages the game's lifecycle, session state, and core services.
    - **`GameSession`**: Static class holding the entire state of the current run (`RunState`). Acts as a central repository for game state.
    - **`RunManager`**: A persistent singleton (`DontDestroyOnLoad`) that manages the main game flow, including scene transitions and UI management.
    - **`GameInitializer`**: Runs in the `Boot` scene to initialize the `GameSession` and `RunManager`.
    - **`TickService`**: A `MonoBehaviour` that provides a fixed-time update tick (100ms) for the combat system.
    - **`GameDataRegistry`**: Static class responsible for loading all `ScriptableObject` data from `Resources` at startup.
    - **`EventBus`**: Static class that acts as a global event dispatcher, decoupling various systems.
  - **Combat:** Handles the turn-based battle between the player and an enemy.
    - **`CombatController`**: Orchestrates the battle, driven by the `TickService`. It processes effects, cooldowns, and checks for end conditions.
    - **`ShipState`**: Holds the runtime state of a ship, including health, items, and effects.
    - **`AbilityManager`**: Subscribes to various `EventBus` events to trigger `AbilitySO` actions.
  - **Data:** Contains the `ScriptableObject` definitions that form the game's data model.
    - **`ItemSO`, `ShipSO`, `EnemySO`, etc.**: `ScriptableObject`s defining game content.
  - **UI:** Manages the user interface using UI Toolkit.
    - **`MainMenuController`**: Handles the main menu.
    - **`PlayerPanelController`**: Manages the main player UI, including inventory and ship stats.
    - **`MapView`**: Displays the game's map.
    - **`BattleUIController`**: Manages the UI during combat.

- **Combat Tick Walkthrough:**
  A single tick in `CombatController.HandleTick` executes the following steps in order:
  1.  **Sudden Death Check**: If the battle has exceeded 30 seconds, sudden death is initiated, and both ships take increasing damage each tick. (`CombatController.cs:71-86`)
  2.  **Process Active Effects**: For both player and enemy, it iterates through all active effects.
      - Effects are sorted by their `ActionType` priority. (`CombatController.cs:111-114`)
      - Each effect's `Tick()` method is called, which may trigger an action. (`CombatController.cs:118-122`)
      - Expired effects are removed. (`CombatController.cs:124-132`)
  3.  **Reduce Stun Duration**: `Player.ReduceStun()` and `Enemy.ReduceStun()` are called. (`CombatController.cs:94-95`)
  4.  **Dispatch Tick Event**: `EventBus.DispatchTick()` is called to allow other systems (like `AbilityManager`) to react to the tick. (`CombatController.cs:99`)
  5.  **Check Battle End Conditions**: Checks if either ship's health is at or below zero and ends the battle if necessary. (`CombatController.cs:135-168`)

- **Event Catalog:**

| Event Name | Publisher(s) (Class:Line) | Subscriber(s) (Class:Line) | Notes |
|---|---|---|---|
| `OnTick` (TickService) | `TickService:38` | `CombatController:29` | The main driver for the combat loop. |
| `OnBattleStart` | `CombatController:48` | `AbilityManager:48` | Signals the beginning of a battle. |
| `OnSuddenDeathStarted` | `CombatController:74` | `BattleUIController:41` | Fired when the battle timer reaches the sudden death threshold. |
| `OnDamageReceived` | `ShipState:224` | `ShipView:18`, `AbilityManager:50` | Fired when a ship takes damage. |
| `OnHeal` | `ShipState:239` | `ShipView:19`, `AbilityManager:51` | Fired when a ship is healed. |
| `OnTick` (EventBus) | `CombatController:99` | `AbilityManager:52` | Fired every combat tick for general-purpose updates. |
| `OnHealthChanged` | `ShipState:102, 223, 238` | `CombatController:38`, `CombatController:39`, `ShipView:17`, `PlayerPanelController:87` | A direct C# event on `ShipState` for UI updates. |
| `OnEncounterEnd` | *None found* | *None found* | Defined in `EventBus` but appears to be unused. |
| `OnItemReady` | `EventBus` (Generic Dispatch) | `AbilityManager` (via `TriggerType` check) | Used to trigger item abilities. |
| `OnAllyActivate` | `EventBus` (Generic Dispatch) | *None found* | Defined in `EventBus` but appears to be unused. |
| `OnDamageDealt` | `EventBus` (Generic Dispatch) | `AbilityManager:49` | Used to trigger abilities on dealing damage. |
| `OnShieldGained` | *None found* | *None found* | Defined in `EventBus` but appears to be unused. |
| `OnDebuffApplied` | *None found* | *None found* | Defined in `EventBus` but appears to be unused. |
| `OnBuffApplied` | *None found* | *None found* | Defined in `EventBus` but appears to be unused. |

- **Data Model:**

| SO Type | Path | Key Fields | Loading Mechanism |
|---|---|---|---|
| `ItemSO` | `Assets/Resources/GameData/Items` | `id`, `displayName`, `sprite`, `abilities` | `Resources.LoadAll` in `GameDataRegistry` |
| `ShipSO` | `Assets/Resources/GameData/Ships` | `id`, `displayName`, `shipSprite`, `baseHealth` | `Resources.LoadAll` in `GameDataRegistry` |
| `EncounterSO` | `Assets/Resources/GameData/Encounters` | `id`, `encounterType`, `enemy` | `Resources.LoadAll` in `GameDataRegistry` |
| `RunConfigSO` | `Assets/Resources/GameData` | `startingLives`, `startingGold`, `inventorySize` | `Resources.Load` in `GameDataRegistry` |
| `AbilitySO` | `Assets/Resources/GameData/Abilities` | `displayName`, `Trigger`, `Actions` | Loaded as part of other SOs (e.g., `ItemSO`) |
| `ActionSO` | `Assets/Resources/GameData/Actions` | (Abstract) | Loaded as part of `AbilitySO` |
| `EffectSO` | `Assets/Resources/GameData/Effects` | `duration`, `tickAction` | Loaded as part of `ActionSO` |
| `EnemySO` | `Assets/Resources/GameData/Enemies` | `id`, `shipId`, `itemLoadout` | Loaded as part of `EncounterSO` |
| `PlayerUIThemeSO` | `Assets/UI/PlayerPanel` | `primaryColor`, `secondaryColor` | Assigned in Inspector |

- **Content Health:**
  - **`StreamingAssets/items.json`**: This file is present in the project but there are no references to it in the C# code. It appears to be a remnant of a previous data loading system and is not currently used. The authoritative source for item data is the `ItemSO` ScriptableObjects in `Assets/Resources/GameData/Items`.
  - **Unused Events**: The `EventBus` defines several events that are never dispatched (`OnEncounterEnd`, `OnShieldGained`, `OnDebuffApplied`, `OnBuffApplied`). This may indicate incomplete features.

- **Performance Notes:**
  - **Combat Tick Allocations**: In `CombatController.ProcessActiveEffects`, a new list is created for `effectsToRemove` and `sortedActiveEffects` every tick. For a small number of effects, this is negligible, but could become a performance concern with many effects. Consider using a pooled list or clearing the list instead of creating a new one.
  - **LINQ in Hot Path**: `sortedActiveEffects` uses `OrderBy` on every tick. While convenient, this can cause allocations. If performance becomes an issue, consider a more optimized sorting method or pre-sorting if the order is static.

- **Risks & Recommendations:**

| Severity | Effort | Rationale | Recommendation |
|---|---|---|---|
| **High** | **M** | **`GameSession` is a static class with global state.** This makes the game difficult to test in isolation and can lead to bugs that are hard to reproduce. It also complicates the save/load system, as the entire state is coupled to this one class. | Refactor `GameSession` into a `MonoBehaviour` or a plain C# class that is passed explicitly to the systems that need it. This will improve testability and make the data flow more explicit. |
| **Medium** | **S** | **Direct UI updates from game state.** Classes like `CombatController` and `PlayerPanelController` directly subscribe to events on `ShipState`. This creates a tight coupling between the game logic and the UI. | Introduce a data-binding layer or a view-model that sits between the game state and the UI. The UI should only be updated from the view-model, which in turn is updated by the game logic. This will decouple the UI from the game logic and make it easier to change one without affecting the other. |
| **Low** | **S** | **Unused events and data files.** The project contains unused assets and code (`items.json`, some `EventBus` events), which can cause confusion for new developers. | Remove the unused `items.json` file and the unused events from `EventBus`. This will clean up the codebase and make it easier to understand. |

- **Roadmap:**
  - **Quick Wins (1-2 days):**
    - [ ] Remove `StreamingAssets/items.json`.
    - [ ] Remove unused events from `EventBus`.
    - [ ] Optimize `ProcessActiveEffects` to reduce per-tick allocations.
  - **Near Term (1-2 weeks):**
    - [ ] Implement a view-model layer for the UI to decouple it from the game logic.
  - **Mid Term (1-2 sprints):**
    - [ ] Refactor `GameSession` to be a non-static class.

- **Glossary & File Index:**
  - **`GameSession`**: (`Assets/Scripts/Core/GameSession.cs`) Static class holding the current run's state.
  - **`RunManager`**: (`Assets/Scripts/Core/RunManager.cs`) Persistent singleton for managing the game loop.
  - **`CombatController`**: (`Assets/Scripts/Combat/CombatController.cs`) Manages a single battle.
  - **`GameDataRegistry`**: (`Assets/Scripts/Core/GameDataRegistry.cs`) Loads and provides access to all `ScriptableObject` data.
  - **`EventBus`**: (`Assets/Scripts/Core/EventBus.cs`) Global static event dispatcher.

- **Open Questions:**
  - What is the intended use of the `OnAllyActivate` event?
  - Are there plans to use the Addressables system in the future?
  - What is the strategy for handling different screen resolutions and aspect ratios?
