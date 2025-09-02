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
            A[MainMenu.unity] --> B{PirateRoguelike.UI.MainMenuController};
        end

        subgraph Game Initialization
            B -- OnStartGameClicked --> C[SceneManager.LoadScene("Boot")];
            C --> D[Boot.unity];
            D --> E{PirateRoguelike.Core.GameInitializer};
            E --> F[PirateRoguelike.Core.GameSession.StartNewRun / LoadRun];
            E -- Instantiates --> G((PirateRoguelike.Core.RunManager));
            G -- Instantiates --> H((Pirate.MapGen.MapManager));
            E --> I[SceneManager.LoadScene("Run")];
        end

        subgraph Main Game Loop
            I --> J[Run.unity];
            J --> K{PirateRoguelike.Core.RunManager};
            K -- Manages --> L{PirateRoguelike.UI.MapView};
            K -- Manages --> M{PirateRoguelike.UI.PlayerPanelController};
            L -- OnNodeClicked --> N[Start Encounter];
        end

        subgraph Combat
            N -- Loads --> O[Battle.unity];
            O --> P{PirateRoguelike.Combat.BattleManager};
            P --> Q{PirateRoguelike.Combat.CombatController};
            R((PirateRoguelike.Core.TickService)) -- OnTick (100ms) --> Q;
            Q -- Updates --> S{PirateRoguelike.Core.ShipState};
            Q -- Updates --> T{PirateRoguelike.UI.BattleUIController};
        end

        subgraph Global Systems
            U[PirateRoguelike.Core.GameDataRegistry] -- Provides --> AllSystems[All Systems];
            V[PirateRoguelike.Core.EventBus] -- Mediates --> AllSystems;
            W[PirateRoguelike.Core.GameSession] -- Holds --> X[PirateRoguelike.Saving.RunState];
        end
    ```

-   **Module Overviews:**
    -   **Core:** Manages game lifecycle, session state, and core services.
        -   **`PirateRoguelike.Core.GameSession`**: Static class, central repository for current run's state (`PirateRoguelike.Saving.RunState`).
        -   **`PirateRoguelike.Core.RunManager`**: Persistent singleton, manages main game flow, scene transitions, and UI.
        -   **`Pirate.MapGen.MapManager`**: Persistent singleton, manages map generation and provides map node data. Its state is reset at the start of each new run.
        -   **`PirateRoguelike.Core.GameInitializer`**: Runs in `Boot` scene, initializes `PirateRoguelike.Core.GameSession` and `PirateRoguelike.Core.RunManager`.
        -   **`PirateRoguelike.Core.TickService`**: `MonoBehaviour` providing a fixed 100ms update tick for combat.
        -   **`PirateRoguelike.Core.GameDataRegistry`**: Static class, loads all `ScriptableObject` data from `Resources` at startup.
        -   **`PirateRoguelike.Core.EventBus`**: Static global event dispatcher, decouples systems.
    -   **Combat:** Handles turn-based player vs. enemy battles.
        -   **`PirateRoguelike.Combat.CombatController`**: Orchestrates battle (effects, cooldowns, end conditions), driven by `PirateRoguelike.Core.TickService`.
        -   **`PirateRoguelike.Core.ShipState`**: Holds runtime ship state (health, items, effects).
        -   **`PirateRoguelike.Core.AbilityManager`**: Subscribes to `PirateRoguelike.Core.EventBus` events to trigger `AbilitySO` actions.
    -   **Data:** `ScriptableObject` definitions for game content.
        -   **`PirateRoguelike.Data.ItemSO`**
        -   **`PirateRoguelike.Data.ShipSO`**
        -   **`PirateRoguelike.Data.EnemySO`**
    -   **UI:** Manages UI using UI Toolkit.
        -   **`PirateRoguelike.UI.MainMenuController`**
        -   **`PirateRoguelike.UI.PlayerPanelController`**
        -   **`PirateRoguelike.UI.MapView`**
        -   **`PirateRoguelike.UI.BattleUIController`**

    -   **UI Systems:** Manages various UI elements and their interactions using UI Toolkit.
        -   **`PirateRoguelike.UI.PlayerPanelController`**: Orchestrates the player's main UI. It is responsible for creating the `PlayerPanelView` (the view) and the `PlayerPanelDataViewModel` (the view model), and injecting the `IGameSession` dependency into the view model. This ensures the UI is decoupled from the static `GameSession` state.
        -   **`PirateRoguelike.UI.TooltipController`**: A singleton `MonoBehaviour` responsible for managing the lifecycle, content population, positioning, and visibility of the item tooltip. It dynamically instantiates tooltip elements from UXML assets and attaches them to the main UI `rootVisualElement` (Player Panel's `UIDocument`'s root) to ensure correct z-ordering.
            -   **Visibility Management**: Employs a `IsTooltipVisible` flag to track its state, preventing redundant show/hide calls.
            -   **Smooth Transitions**: Utilizes coroutines (`_currentTooltipCoroutine`) to manage smooth fade-in/fade-out animations, ensuring only one animation is active at a time.
            -   **CSS Integration**: Works in conjunction with `TooltipPanelStyle.uss` for opacity transitions, with `visibility` controlled directly in C# to ensure proper animation sequencing.
        -   **`PirateRoguelike.UI.EffectDisplay`**: A helper class used by `PirateRoguelike.UI.TooltipController` to dynamically display individual ability effects within the tooltip.
        -   **`PirateRoguelike.UI.EnemyPanelController`**: Manages the enemy's UI panel, including dynamic equipment slot generation and tooltip integration.

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

    1.  **`PirateRoguelike.Core.GameInitializer.Start()`**:
        *   Initializes `PirateRoguelike.Core.AbilityManager` by subscribing it to `PirateRoguelike.Core.EventBus` events.
        *   Calls `Pirate.MapGen.MapManager.Instance.ResetMap()` to clear any previous map state.
        *   Initializes `PirateRoguelike.Core.GameSession` (either starting a new run or loading a saved one). This step sets up core game data (`PirateRoguelike.Saving.RunState`, `PirateRoguelike.Services.EconomyService`, `PirateRoguelike.Services.Inventory`, `PirateRoguelike.Core.ShipState`) and dispatches `PirateRoguelike.Core.GameSession.OnEconomyInitialized`, `PirateRoguelike.Core.GameSession.OnPlayerShipInitialized`, and `PirateRoguelike.Core.GameSession.OnInventoryInitialized` events.
        *   Instantiates the `PirateRoguelike.Core.RunManager` prefab, which becomes a persistent singleton, and explicitly calls `PirateRoguelike.Core.RunManager.Instance.Initialize()` to set up core game managers (like `Pirate.MapGen.MapManager`).
        *   Instantiates the `PirateRoguelike.UI.UIManager` prefab, which becomes a persistent singleton, and explicitly calls `PirateRoguelike.UI.UIManager.Instance.Initialize()` to set up global UI elements.

    2.  **`PirateRoguelike.Core.RunManager.Awake()`**: (Executes immediately after `PirateRoguelike.Core.RunManager` is instantiated)
        *   Sets up the `PirateRoguelike.Core.RunManager` singleton and subscribes to `SceneManager.sceneLoaded` and `PirateRoguelike.Core.GameSession.OnPlayerNodeChanged`.

    3.  **`PirateRoguelike.Core.RunManager.Initialize()`**: (Called explicitly by `PirateRoguelike.Core.GameInitializer.Start()`)
        *   Instantiates `Pirate.MapGen.MapManager`. `Pirate.MapGen.MapManager` then generates the game map data and assigns it to `PirateRoguelike.Core.GameSession.CurrentRunState.mapGraphData`, subsequently invoking `Pirate.MapGen.MapManager.OnMapDataUpdated`.
        *   Instantiates `RewardUI`.

    4.  **`PirateRoguelike.UI.UIManager.Awake()`**: (Executes immediately after `PirateRoguelike.UI.UIManager` is instantiated)
        *   Sets up the `PirateRoguelike.UI.UIManager` singleton.

    5.  **`PirateRoguelike.UI.UIManager.Initialize()`**: (Called explicitly by `PirateRoguelike.Core.GameInitializer.Start()`)
        *   Instantiates `GlobalUIOverlay`, `PirateRoguelike.UI.PlayerPanelController`, `PirateRoguelike.UI.MapView`, and `PirateRoguelike.UI.TooltipController`.
        *   Performs initial hookups between UI components (e.g., `PirateRoguelike.UI.PlayerPanelController.SetMapPanel`).
        *   Initializes `PirateRoguelike.UI.TooltipController`.

    6.  **`SceneManager.LoadScene("Run")`**: (Called by `PirateRoguelike.Core.GameInitializer.Start()`)
        *   Loads the "Run" scene, which contains the main game environment.

    7.  **`PirateRoguelike.Core.RunManager.OnSceneLoaded()`**: (Executes when the "Run" scene is loaded)
        *   Calls `OnRunSceneLoaded()`.

    8.  **`PirateRoguelike.Core.RunManager.OnRunSceneLoaded()`**: (Executes after the "Run" scene is loaded and `Pirate.MapGen.MapManager` has generated data)
        *   Checks for battle rewards.
        *   Ensures `Pirate.MapGen.MapManager` has generated map data (if not, it generates it).
        *   Ensures `PirateRoguelike.Core.GameSession.Economy` is initialized.
        *   **Crucially, calls `PirateRoguelike.UI.UIManager.Instance.InitializeRunUI()` to initialize and display the UI for the "Run" scene.**

    9.  **`PirateRoguelike.UI.UIManager.InitializeRunUI()`**: (Called explicitly by `PirateRoguelike.Core.RunManager.OnRunSceneLoaded()`)
        *   Initializes the `PirateRoguelike.UI.PlayerPanelController` with the current `PirateRoguelike.Core.GameSession` data.
        *   Calls `PirateRoguelike.UI.MapView.Show()` to display the map.

    **Event-Driven Data Flow and UI Updates:**
    *   When `PirateRoguelike.Core.GameSession` invokes its initialization events (`OnPlayerShipInitialized`, `OnInventoryInitialized`, `OnEconomyInitialized`), the `PlayerPanelDataViewModel` reacts by updating its properties. This, in turn, notifies the `PlayerPanelView` to update the UI elements that display player ship, inventory, and economy data.
    *   When `Pirate.MapGen.MapManager` invokes `OnMapDataUpdated`, the `PirateRoguelike.UI.MapView` reacts by calling its `HandleMapDataUpdated()` method, which triggers the layout and rendering of the map nodes and edges.
    *   Ongoing changes to `PirateRoguelike.Core.GameSession.PlayerShip` (equipment) and `PirateRoguelike.Services.Inventory` trigger events that `PlayerPanelDataViewModel` is subscribed to, ensuring the UI remains synchronized with game state.

    This structured initialization sequence, combined with an event-driven data flow, ensures that UI components and view models only attempt to access game data after it has been properly initialized, minimizing the risk of errors and promoting a decoupled architecture.

-   **Combat Tick Walkthrough (`PirateRoguelike.Combat.CombatController.HandleTick`):**
    1.  **Sudden Death Check**: Initiates if battle exceeds 30 seconds; ships take increasing damage. (`PirateRoguelike.Combat.CombatController.cs:71-86`)
    2.  **Process Active Effects**: Iterates sorted active effects for both player/enemy, calls `Tick()`, removes expired. (`PirateRoguelike.Combat.CombatController.cs:111-132`)
    3.  **Reduce Stun Duration**: Calls `Player.ReduceStun()` and `Enemy.ReduceStun()`. (`PirateRoguelike.Combat.CombatController.cs:94-95`)
    4.  **Dispatch Tick Event**: `PirateRoguelike.Core.EventBus.DispatchTick()` for system reactions (e.g., `PirateRoguelike.Core.AbilityManager`). (`PirateRoguelike.Combat.CombatController.cs:99`)
    5.  **Check Battle End Conditions**: Ends battle if either ship's health is zero or below. (`PirateRoguelike.Combat.CombatController.cs:135-168`)

-   **Event Catalog:**

| Event Name | Publisher(s) (Class:Line) | Subscriber(s) (Class:Line) | Notes |
| :--- | :--- | :--- | :--- |
| `OnTick` (PirateRoguelike.Core.TickService) | `PirateRoguelike.Core.TickService:38` | `PirateRoguelike.Combat.CombatController:29` | Main driver for combat. |
| `OnBattleStart` | `PirateRoguelike.Combat.CombatController:48` | `PirateRoguelike.Core.AbilityManager:48` | Signals battle start. |
| `OnSuddenDeathStarted` | `PirateRoguelike.Combat.CombatController:74` | `PirateRoguelike.UI.BattleUIController:41` | Fired when sudden death begins. |
| `OnDamageReceived` | `PirateRoguelike.Core.ShipState:224` | `ShipView:18`, `PirateRoguelike.Core.AbilityManager:50` | Fired when ship takes damage. |
| `OnHeal` | `PirateRoguelike.Core.ShipState:239` | `ShipView:19`, `PirateRoguelike.Core.AbilityManager:51` | Fired when ship is healed. |
| `OnTick` (PirateRoguelike.Core.EventBus) | `PirateRoguelike.Combat.CombatController:99` | `PirateRoguelike.Core.AbilityManager:52` | General combat tick update. |
| `OnHealthChanged` | `PirateRoguelike.Core.ShipState:102, 223, 238` | `PirateRoguelike.Combat.CombatController:38, 39`, `ShipView:17`, `PlayerPanelController:87` | Direct C# event on `ShipState` for UI. |
| `OnEncounterEnd` | *None found* | *None found* | Unused in `EventBus`. |
| `OnItemReady` | `PirateRoguelike.Core.EventBus` | `PirateRoguelike.Core.AbilityManager` | Triggers item abilities. |
| `OnAllyActivate` | `PirateRoguelike.Core.EventBus` | *None found* | Unused in `EventBus`. |
| `OnDamageDealt` | `PirateRoguelike.Core.EventBus` | `PirateRoguelike.Core.AbilityManager:49` | Triggers abilities on damage dealt. |
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
| **Medium** | **S** | Custom UI components that don't refresh on binding can miss initial state. | Ensure custom UI components (like `ShipDisplayElement`) perform a full refresh in their `Bind()` method to prevent timing issues where initial data is missed. |
| **Low** | **L** | Current synchronous initialization chain can become brittle with complex loading. | Consider refactoring the initialization sequence to use an asynchronous, callback-based pattern (e.g., `IEnumerator` coroutines or `async/await Task`) to ensure robust, sequential loading of interdependent systems. |
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
    -   **`PirateRoguelike.Core.GameSession`**: (`Assets/Scripts/Core/GameSession.cs`) Static class holding current run's state.
    -   **`PirateRoguelike.Core.RunManager`**: (`Assets/Scripts/Core/RunManager.cs`) Persistent singleton managing game loop.
    -   **`PirateRoguelike.Combat.CombatController`**: (`Assets/Scripts/Combat/CombatController.cs`) Manages a single battle.
    -   **`PirateRoguelike.Core.GameDataRegistry`**: (`Assets/Scripts/Core/GameDataRegistry.cs`) Loads/provides `ScriptableObject` data.
    -   **`PirateRoguelike.Core.EventBus`**: (`Assets/Scripts/Core/EventBus.cs`) Global static event dispatcher.

-   **Open Questions:**
    -   Intended use of `OnAllyActivate`?
    -   Plans for Addressables system?
    -   Strategy for different screen resolutions/aspect ratios?