-   **Snapshot:**
    -   **Unity Version:** 6000.2.0f1
    -   **Critical Packages:** `com.unity.inputsystem`, `com.unity.ui.builder`, `com.unity.uitoolkit`
    -   **Scenes in Build:** `MainMenu`, `Boot`, `Run`, `Battle`, `Summary`
    -   **ASMDEFs:** None in `Assets/Scripts`
    -   **Scripting Define Symbols:** `UNITY_EDITOR` (in `MainMenuController.cs`)

-   **High-Level Diagram (Mermaid):**

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

-   **Module Overviews:**
    -   **Core:** Manages game lifecycle, session state, and core services.
        -   **`GameSession`**: Static class, central repository for current run's state (`RunState`).
        -   **`RunManager`**: Persistent singleton, manages main game flow, scene transitions, and UI.
        -   **`MapManager`**: Persistent singleton, manages map generation and provides map node data. Its state is reset at the start of each new run.
        -   **`GameInitializer`**: Runs in `Boot` scene, initializes `GameSession` and `RunManager`.
        -   **`TickService`**: `MonoBehaviour` providing a fixed 100ms update tick for combat.
        -   **`GameDataRegistry`**: Static class, loads all `ScriptableObject` data from `Resources` at startup.
        -   **`EventBus`**: Static global event dispatcher, decouples systems.
    -   **Combat:** Handles turn-based player vs. enemy battles.
        -   **`CombatController`**: Orchestrates battle (effects, cooldowns, end conditions), driven by `TickService`.
        -   **`ShipState`**: Holds runtime ship state (health, items, effects).
        -   **`AbilityManager`**: Subscribes to `EventBus` events to trigger `AbilitySO` actions.
    -   **Data:** `ScriptableObject` definitions for game content (`ItemSO`, `ShipSO`, `EnemySO`, etc.).
    -   **UI:** Manages UI using UI Toolkit (`MainMenuController`, `PlayerPanelController`, `MapView`, `BattleUIController`).

    -   **UI Systems:** Manages various UI elements and their interactions using UI Toolkit.
        -   **`TooltipController`**: A singleton `MonoBehaviour` responsible for managing the lifecycle, content population, positioning, and visibility of the item tooltip. It dynamically instantiates tooltip elements from UXML assets and attaches them to the main UI `rootVisualElement` (Player Panel's `UIDocument`'s root) to ensure correct z-ordering.
            -   **Visibility Management**: Employs a `IsTooltipVisible` flag to track its state, preventing redundant show/hide calls.
            -   **Smooth Transitions**: Utilizes coroutines (`_currentTooltipCoroutine`) to manage smooth fade-in/fade-out animations, ensuring only one animation is active at a time.
            -   **CSS Integration**: Works in conjunction with `TooltipPanelStyle.uss` for opacity transitions, with `visibility` controlled directly in C# to ensure proper animation sequencing.
        -   **`EffectDisplay`**: A helper class used by `TooltipController` to dynamically display individual ability effects within the tooltip.
        -   **`EnemyPanelController`**: Manages the enemy's UI panel, including dynamic equipment slot generation and tooltip integration.

-   **System Initialization Order:**
    This section details the precise order in which core game systems, view models, and UI components are initialized, ensuring data readiness and proper inter-system communication.

    ```mermaid
    graph TD
        A[GameInitializer.Start()] --> B{AbilityManager.Initialize()};
        A --> C{GameSession.StartNewRun() / LoadRun()};
        C --> D[GameSession.OnEconomyInitialized];
        C --> E[GameSession.OnPlayerShipInitialized];
        C --> F[GameSession.OnInventoryInitialized];
        A --> G[Instantiate RunManager];
        G --> H[RunManager.Awake()];
        H --> I{Instantiate MapManager};
        I --> J[MapManager.GenerateMapIfNeeded()];
        J --> K[MapManager.OnMapDataUpdated];
        H --> L{Instantiate MapView};
        L --> M[MapView.Awake()];
        M --> N[MapView subscribes to MapManager.OnMapDataUpdated];
        M --> O[MapView subscribes to GameSession.OnPlayerNodeChanged];
        H --> P{Instantiate PlayerPanelController};
        H --> Q{Instantiate GlobalUIOverlay};
        H --> R{Instantiate TooltipController};
        R --> S[TooltipController.Awake()];
        S --> T[TooltipController.Initialize(GlobalUIOverlay.rootVisualElement)];
        A --> U[SceneManager.LoadScene("Run")];
        U --> V[RunManager.Start()];
        V --> W[PlayerPanelController.Initialize()];
        W --> X[PlayerPanelDataViewModel.Initialize()];
        X --> Y[PlayerPanelDataViewModel subscribes to GameSession.On...Initialized events];
        X --> Z[PlayerPanelDataViewModel subscribes to GameSession.On...Changed events];
        W --> AA[PlayerPanelView.BindInitialData()];

        K --> N;
        D --> Y;
        E --> Y;
        F --> Y;
        GameSession.PlayerShip.OnEquipmentChanged --> Z;
        GameSession.Inventory.OnInventoryChanged --> Z;
        GameSession.OnPlayerNodeChanged --> O;
    ```

    **Detailed Initialization Flow:**

    1.  **`GameInitializer.Start()`**:
        *   Initializes `AbilityManager` by subscribing it to `EventBus` events.
        *   Calls `MapManager.Instance.ResetMap()` to clear any previous map state.
        *   Initializes `GameSession` (either starting a new run or loading a saved one). This step sets up core game data (`CurrentRunState`, `EconomyService`, `Inventory`, `PlayerShip`) and dispatches `GameSession.OnEconomyInitialized`, `GameSession.OnPlayerShipInitialized`, and `GameSession.OnInventoryInitialized` events.
        *   Instantiates the `RunManager` prefab, which becomes a persistent singleton.

    2.  **`RunManager.Awake()`**: (Executes immediately after `RunManager` is instantiated)
        *   Instantiates `MapManager`. `MapManager` then generates the game map data and assigns it to `GameSession.CurrentRunState.mapGraphData`, subsequently invoking `MapManager.OnMapDataUpdated`.
        *   Instantiates `MapView` (UI component). `MapView.Awake()` runs, setting up its UI elements and subscribing to `MapManager.OnMapDataUpdated` and `GameSession.OnPlayerNodeChanged` to react to data changes.
        *   Instantiates `PlayerPanelController` (UI component).
        *   Instantiates `GlobalUIOverlay` (a root UI `UIDocument` for global UI elements).
        *   Instantiates `TooltipController` (UI component). `TooltipController.Awake()` runs.
        *   Calls `TooltipController.Initialize()`, passing the `rootVisualElement` of the `GlobalUIOverlay`. This integrates the tooltip UI into the main UI hierarchy.

    3.  **`SceneManager.LoadScene("Run")`**: (Called by `GameInitializer.Start()`)
        *   Loads the "Run" scene, which contains the main game environment.

    4.  **`RunManager.Start()`**: (Executes after all `Awake()` methods in the scene have completed, and after the "Run" scene is loaded)
        *   Calls `PlayerPanelController.Initialize()`.
        *   `PlayerPanelController.Initialize()` instantiates `PlayerPanelDataViewModel` (the view model for the player panel) and `PlayerPanelView` (the view component).
        *   Crucially, `PlayerPanelDataViewModel.Initialize()` is called, which subscribes to `GameSession.OnPlayerShipInitialized`, `GameSession.OnInventoryInitialized`, and `GameSession.OnEconomyInitialized` events (for initial data binding) as well as `GameSession.PlayerShip.OnEquipmentChanged` and `GameSession.Inventory.OnInventoryChanged` (for ongoing updates).
        *   `PlayerPanelView.BindInitialData()` is called to perform the initial data binding from the view model to the UI.

    **Event-Driven Data Flow and UI Updates:**
    *   When `GameSession` invokes its initialization events (`OnPlayerShipInitialized`, `OnInventoryInitialized`, `OnEconomyInitialized`), the `PlayerPanelDataViewModel` reacts by updating its properties. This, in turn, notifies the `PlayerPanelView` to update the UI elements that display player ship, inventory, and economy data.
    *   When `MapManager` invokes `OnMapDataUpdated`, the `MapView` reacts by calling its `HandleMapDataUpdated()` method, which triggers the layout and rendering of the map nodes and edges.
    *   Ongoing changes to `GameSession.PlayerShip` (equipment) and `GameSession.Inventory` trigger events that `PlayerPanelDataViewModel` is subscribed to, ensuring the UI remains synchronized with game state.

    This structured initialization sequence, combined with an event-driven data flow, ensures that UI components and view models only attempt to access game data after it has been properly initialized, minimizing the risk of errors and promoting a decoupled architecture.

-   **Combat Tick Walkthrough (`CombatController.HandleTick`):**
    1.  **Sudden Death Check**: Initiates if battle exceeds 30 seconds; ships take increasing damage. (`CombatController.cs:71-86`)
    2.  **Process Active Effects**: Iterates sorted active effects for both player/enemy, calls `Tick()`, removes expired. (`CombatController.cs:111-132`)
    3.  **Reduce Stun Duration**: Calls `Player.ReduceStun()` and `Enemy.ReduceStun()`. (`CombatController.cs:94-95`)
    4.  **Dispatch Tick Event**: `EventBus.DispatchTick()` for system reactions (e.g., `AbilityManager`). (`CombatController.cs:99`)
    5.  **Check Battle End Conditions**: Ends battle if either ship's health is zero or below. (`CombatController.cs:135-168`)

-   **Event Catalog:**

| Event Name | Publisher(s) (Class:Line) | Subscriber(s) (Class:Line) | Notes |
| :--- | :--- | :--- | :--- |
| `OnTick` (TickService) | `TickService:38` | `CombatController:29` | Main driver for combat. |
| `OnBattleStart` | `CombatController:48` | `AbilityManager:48` | Signals battle start. |
| `OnSuddenDeathStarted` | `CombatController:74` | `BattleUIController:41` | Fired when sudden death begins. |
| `OnDamageReceived` | `ShipState:224` | `ShipView:18`, `AbilityManager:50` | Fired when ship takes damage. |
| `OnHeal` | `ShipState:239` | `ShipView:19`, `AbilityManager:51` | Fired when ship is healed. |
| `OnTick` (EventBus) | `CombatController:99` | `AbilityManager:52` | General combat tick update. |
| `OnHealthChanged` | `ShipState:102, 223, 238` | `CombatController:38, 39`, `ShipView:17`, `PlayerPanelController:87` | Direct C# event on `ShipState` for UI. |
| `OnEncounterEnd` | *None found* | *None found* | Unused in `EventBus`. |
| `OnItemReady` | `EventBus` | `AbilityManager` | Triggers item abilities. |
| `OnAllyActivate` | `EventBus` | *None found* | Unused in `EventBus`. |
| `OnDamageDealt` | `EventBus` | `AbilityManager:49` | Triggers abilities on damage dealt. |
| `OnShieldGained` | *None found* | *None found* | Unused in `EventBus`. |
| `OnDebuffApplied` | *None found* | *None found* | Unused in `EventBus`. |
| `OnBuffApplied` | *None found* | *None found* | Unused in `EventBus`. |

-   **Data Model:**

| SO Type | Path | Key Fields | Loading Mechanism |
| :--- | :--- | :--- | :--- |
| `ItemSO` | `Assets/Resources/GameData/Items` | `id`, `displayName`, `sprite`, `abilities` | `Resources.LoadAll` (`GameDataRegistry`) |
| `ShipSO` | `Assets/Resources/GameData/Ships` | `id`, `displayName`, `shipSprite`, `baseHealth` | `Resources.LoadAll` (`GameDataRegistry`) |
| `EncounterSO` | `Assets/Resources/GameData/Encounters` | `id`, `encounterType`, `enemy` | `Resources.LoadAll` (`GameDataRegistry`) |
| `RunConfigSO` | `Assets/Resources/GameData` | `startingLives`, `startingGold`, `inventorySize` | `Resources.Load` (`GameDataRegistry`) |
| `AbilitySO` | `Assets/Resources/GameData/Abilities` | `displayName`, `Trigger`, `Actions` | Via other SOs (e.g., `ItemSO`) |
| `ActionSO` | `Assets/Resources/GameData/Actions` | (Abstract) | Via `AbilitySO` |
| `EffectSO` | `Assets/Resources/GameData/Effects` | `duration`, `tickAction` | Via `ActionSO` |
| `EnemySO` | `Assets/Resources/GameData/Enemies` | `id`, `shipId`, `itemLoadout` | Via `EncounterSO` |
| `PlayerUIThemeSO` | `Assets/UI/PlayerPanel` | `primaryColor`, `secondaryColor` | Assigned in Inspector |

-   **Content Health:**
    -   **`StreamingAssets/items.json`**: Present but unused; no code references. `ItemSO` ScriptableObjects are the authoritative source.
    -   **Unused Events**: Several `EventBus` events (`OnEncounterEnd`, `OnShieldGained`, `OnDebuffApplied`, `OnBuffApplied`, `OnAllyActivate`) are defined but never dispatched, indicating potential incomplete features.

-   **Performance Notes:**
    -   **Combat Tick Allocations**: `CombatController.ProcessActiveEffects` creates new lists (`effectsToRemove`, `sortedActiveEffects`) every tick. Could be a concern with many effects; consider pooling or clearing lists.
    -   **LINQ in Hot Path**: `OrderBy` in `sortedActiveEffects` on every tick causes allocations. Optimize or pre-sort if performance-critical.

-   **Risks & Recommendations:**

| Severity | Effort | Rationale | Recommendation |
| :--- | :--- | :--- | :--- |
| **High** | **M** | `GameSession`'s static global state hinders testability and complicates save/load. | Refactor `GameSession` into a non-static class/MonoBehaviour for improved testability and explicit data flow. |
| **Medium** | **S** | Direct UI updates (e.g., `CombatController`, `PlayerPanelController` subscribing to `ShipState`) tightly couple game logic and UI. *Partially mitigated by the new event-driven item manipulation system.* | Introduce a data-binding/view-model layer to decouple UI from game logic. |
| **Low** | **S** | Unused `items.json` and `EventBus` events create codebase clutter and confusion. | Remove unused `items.json` and `EventBus` events to clean up the project. |

-   **Roadmap:**
    -   **Quick Wins:**
        -   [x] Implement a mouseover tooltip system for items using UI Toolkit.
        -   [x] Implement a universal item manipulation system.
        -   [ ] Remove `StreamingAssets/items.json`.
        -   [ ] Remove unused events from `EventBus`.
        -   [ ] Optimize `ProcessActiveEffects` to reduce per-tick allocations.
    -   **Near Term:**
        -   [ ] Implement a view-model layer for UI.
    -   **Mid Term:**
        -   [ ] Refactor `GameSession` to be a non-static class.

-   **Glossary & File Index:**
    -   **`GameSession`**: (`Assets/Scripts/Core/GameSession.cs`) Static class holding current run's state.
    -   **`RunManager`**: (`Assets/Scripts/Core/RunManager.cs`) Persistent singleton managing game loop.
    -   **`CombatController`**: (`Assets/Scripts/Combat/CombatController.cs`) Manages a single battle.
    -   **`GameDataRegistry`**: (`Assets/Scripts/Core/GameDataRegistry.cs`) Loads/provides `ScriptableObject` data.
    -   **`EventBus`**: (`Assets/Scripts/Core/EventBus.cs`) Global static event dispatcher.

-   **Open Questions:**
    -   Intended use of `OnAllyActivate`?
    -   Plans for Addressables system?
    -   Strategy for different screen resolutions/aspect ratios?