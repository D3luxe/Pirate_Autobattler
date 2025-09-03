---
title: "Debug Mode Analysis"
weight: 10
system: ["core", "ui", "crosscutting"]
types: ["analysis", "plan", "recommendation"]
status: "draft"
stage: ["Planned"]
---

# Debug Mode Analysis

This document provides an analysis of how to add a debug mode to the Pirate Autobattler game for easier testing, along with a detailed plan for implementing a simple text-based console.

## Problem/Topic

The current testing process requires playing through significant portions of the game, which becomes increasingly time-consuming as the game develops. A debug mode is needed to allow direct interaction with game systems and encounters, streamlining the testing workflow.

## Analysis

The codebase utilizes `GameSession` as a central static class for managing game state (player ship, inventory, economy, current run state). Scene transitions are handled via `UnityEngine.SceneManagement.SceneManager.LoadScene()`, and `RunManager` orchestrates the main game flow and map node progression. Input is managed using `UnityEngine.InputSystem`, as evidenced by the `_saveHotkeyAction` in `RunManager`. Existing `debugStartingShip` fields in `GameInitializer` and `RunManager` suggest prior consideration for debug functionality.

**Key Findings:**

1.  **`GameSession` (Core/GameSession.cs):** This static class is central to the game's state. It holds `CurrentRunState`, `PlayerShip`, `Inventory`, `Economy`, and manages scene loading (`SceneManager.LoadScene`). This is a prime candidate for direct manipulation in a debug mode.
    *   `GameSession.StartNewRun(runConfig, debugStartingShip)`: This method in `GameSession` is called by `GameInitializer` and takes a `debugStartingShip`. This is a direct hook for starting a new game with a specific ship, which is a good start for debug.
    *   `GameSession.EndBattle`, `GameSession.EndRun`: These methods control game flow and could be triggered for testing.
    *   `GameSession.Economy.AddGold`, `GameSession.Inventory.AddItem`: These methods allow direct manipulation of player resources.

2.  **`RunManager` (Core/RunManager.cs):** This persistent singleton manages the main game flow, scene transitions, and handles player node changes on the map.
    *   `RunManager.OnRunSceneLoaded()`: This method is called when the "Run" scene is loaded and handles map generation and UI initialization.
    *   `RunManager` uses `SceneManager.LoadScene` to transition between `Battle`, `Shop`, and `Run` scenes. This is crucial for direct scene loading.
    *   `_saveHotkeyAction` (Input System): `RunManager` already uses the new Input System for a save hotkey (`<Keyboard>/s`). This is an excellent precedent for adding more debug hotkeys.

3.  **`CombatController` (Combat/CombatController.cs):** Manages individual battles.
    *   `CombatController.Init`: Initializes a battle with player and enemy ship states.

4.  **`MapManager` (Map/MapManager.cs):** Manages map generation and node data.
    *   `MapManager.GenerateMapIfNeeded`: Generates the map.
    *   `MapManager.GetMapNodeData`: Retrieves data for a specific map node.

5.  **`GameInitializer` (Core/GameInitializer.cs):** Initializes `GameSession` and `RunManager`.
    *   It already has a `debugStartingShip` field, indicating a previous intention for debug functionality.

6.  **Input System (`UnityEngine.InputSystem`):** `RunManager` already uses `InputAction` for a save hotkey. This is the preferred way to handle input for a debug console/commands.

7.  **`Debug.Log` statements:** Many `Debug.Log` and `Debug.LogError` statements are present throughout the codebase, indicating that logging is already used for debugging.

## Conclusion/Recommendations

The most immediate and efficient approach is to implement a simple text-based debug console. This will provide core debugging capabilities and lay the groundwork for a more advanced visual admin panel later.

### Phase 1: Simple Debug Console Implementation

**Goal:** Implement a text-based debug console that can receive commands and display output, allowing basic manipulation of game state and scene transitions.

**Step 1: Create Debug Input Actions**

*   **Objective:** Define a new input action to toggle the debug console's visibility.
*   **Action:** Modify the existing `Assets/InputSystem_Actions.inputactions` file.
    *   Add a new Action Map named "Debug".
    *   Within the "Debug" Action Map, add an Action named "ToggleConsole".
    *   Bind "ToggleConsole" to the `~` (tilde) key.
*   **Verification:** After this step, the input system will be configured to recognize the console toggle.

**Step 2: Create Debug Console UI (UXML & USS)**

*   **Objective:** Design the visual layout and basic styling for the debug console.
*   **Action:** Create a new UXML file: `Assets/UI/DebugConsole.uxml`.
    *   This UXML will define a root `VisualElement` for the console panel.
    *   Inside, include a `ScrollView` element to display command output (history).
    *   Below the `ScrollView`, add a `TextField` element for user input.
*   **Action:** Create a new USS file: `Assets/UI/DebugConsole.uss`.
    *   Define basic styles for the console panel (e.g., background color, border, padding).
    *   Style the `ScrollView` and `TextField` for readability.
    *   Include a style rule to hide/show the console (e.g., `display: none;` vs. `display: flex;`).
*   **Verification:** The UXML and USS files will define the console's appearance.

**Step 3: Implement `DebugConsoleController.cs`**

*   **Objective:** Create the C# script that manages the debug console's logic, input handling, command parsing, and interaction with game systems.
*   **Action:** Create a new C# script: `Assets/Scripts/UI/DebugConsoleController.cs`.
    *   Make it a `MonoBehaviour`.
    *   **Fields:**
        *   `[SerializeField] private VisualTreeAsset debugConsoleUxml;` (Reference to `DebugConsole.uxml`)
        *   `[SerializeField] private StyleSheet debugConsoleUss;` (Reference to `DebugConsole.uss`)
        *   `private VisualElement _rootElement;`
        *   `private ScrollView _outputScrollView;`
        *   `private TextField _inputField;`
        *   `private InputAction _toggleConsoleInput;`
        *   `private InputAction _submitCommandInput;`
    *   **`Awake()` Method:**
        *   Instantiate the UXML and add it to the `rootVisualElement` of the current `UIDocument` (or a global UI overlay if available).
        *   Apply the USS.
        *   Get references to `_outputScrollView` and `_inputField` from the instantiated UXML.
        *   Initialize `_toggleConsoleInput` and `_submitCommandInput` from the "Debug" Action Map.
        *   Set initial visibility to hidden.
    *   **`OnEnable()` / `OnDisable()` Methods:**
        *   Enable/disable input actions and subscribe/unsubscribe to their `performed` events.
        *   Subscribe `_inputField.RegisterCallback<KeyDownEvent>(OnInputKeyDown);` to handle Enter key press.
    *   **`OnInputKeyDown(KeyDownEvent evt)` Method:**
        *   If `evt.keyCode == KeyCode.Return` (Enter key):
            *   Call `ProcessCommand(_inputField.value)`.
            *   Clear `_inputField.value`.
            *   `evt.StopPropagation();`
    *   **`ToggleConsole(InputAction.CallbackContext context)` Method:**
        *   Toggle the `_rootElement`'s display style (e.g., `display: Flex` / `display: None`).
        *   If showing, focus the `_inputField`.
    *   **`ProcessCommand(string command)` Method:**
        *   Add the command to the `_outputScrollView`.
        *   Parse the command string (e.g., `command.ToLower().Split(' ')`).
        *   Implement `if/else if` statements for various commands:
            *   `addgold <amount>`: `GameSession.Economy.AddGold(amount);`
            *   `addlives <amount>`: `GameSession.Economy.AddLives(amount);`
            *   `loadscene <sceneName>`: `UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);`
            *   `skipnode`: Manipulate `GameSession.CurrentRunState.currentEncounterId` and `GameSession.CurrentRunState.currentColumnIndex`, then call `GameSession.InvokeOnPlayerNodeChanged()`.
            *   `giveitem <itemId>`: `GameSession.Inventory.AddItem(new ItemInstance(GameDataRegistry.GetItem(itemId)));` (Requires `GameDataRegistry` access).
            *   `help`: List available commands.
        *   Log command results or errors to the `_outputScrollView`.
    *   **`Log(string message)` Method:**
        *   Appends a new `Label` with the message to `_outputScrollView`.
        *   Scrolls to the bottom of the `_outputScrollView`.
*   **Verification:** The script will handle input, display, and basic command execution.

**Step 4: Integrate Debug Console into the Game**

*   **Objective:** Ensure the debug console is present and functional in the game.
*   **Action:** Create a new prefab: `Assets/Prefabs/DebugConsole.prefab`.
    *   Attach the `DebugConsoleController.cs` script to this prefab.
    *   Assign the `DebugConsole.uxml` and `DebugConsole.uss` assets to the `[SerializeField]` fields in the Inspector.
*   **Action:** In the `Assets/Scenes/Boot.unity` scene:
    *   Add an instance of the `DebugConsole.prefab` to the scene.
    *   Ensure this GameObject is marked to persist across scene loads (e.g., by calling `DontDestroyOnLoad(gameObject)` in `DebugConsoleController.Awake()`).
*   **Verification:** The debug console will appear when toggled and commands can be entered.

### Phase 2: Visual Admin Panel Implementation

**Goal:** Implement a visual admin panel using UI Toolkit that provides interactive controls for game state manipulation and scene/encounter management, building upon the simple console.

### Analysis for Visual Admin Panel

Adding a visual admin panel would involve creating a dedicated UI layer that can display and modify game state through interactive elements (buttons, sliders, text fields). This would leverage the existing UI Toolkit framework and the `GameSession` as the central data source.

1.  **UI Framework (UI Toolkit):**
    *   **Evidence:** The project extensively uses UI Toolkit for various UI elements (`PlayerPanel`, `MapView`, `BattleUIController`, `ShopController`). This is the established UI framework and should be used for the admin panel.
    *   **Files:** `Assets/UI/PlayerPanel`, `Assets/UI/MapView.cs`, `Assets/UI/BattleUIController.cs`, `Assets/UI/ShopController.cs`, `Assets/UI Toolkit` folder.
    *   **Implication:** We would create new UXML and USS files for the admin panel's layout and styling.

2.  **Data Binding and Interaction:**
    *   **Evidence:** `PlayerPanelController` and `PlayerPanelDataViewModel` demonstrate a clear pattern of decoupling UI from game state. `PlayerPanelDataViewModel` subscribes to `GameSession` events (`OnPlayerShipInitialized`, `OnEconomyInitialized`, `OnInventoryInitialized`) to update its properties, which then notify the `PlayerPanelView` to update the UI.
    *   **Files:** `UI/PlayerPanel/PlayerPanelController.cs`, `UI/PlayerPanel/PlayerPanelDataViewModel.cs`, `Core/GameSession.cs` (for events).
    *   **Implication:** The admin panel would follow a similar pattern:
        *   **ViewModel:** A dedicated `AdminPanelDataViewModel` would expose properties for various game stats (gold, lives, current node, inventory items, etc.) and methods for modifying them. This ViewModel would subscribe to relevant `GameSession` events to keep its displayed data synchronized.
        *   **View:** The UXML-defined admin panel would bind its UI elements (labels, text fields, buttons) to the `AdminPanelDataViewModel`.
        *   **Interaction:** Buttons and input fields in the admin panel would trigger methods on the `AdminPanelDataViewModel`, which in turn would call methods on `GameSession` (e.g., `GameSession.Economy.AddGold()`, `GameSession.Inventory.AddItem()`) or `RunManager` (e.g., for scene loading or map manipulation).

3.  **Modularity and Integration:**
    *   **Evidence:** `UIManager` (if present, or `RunManager` instantiating UI elements) is responsible for instantiating and managing various UI components. `GlobalUIOverlay` is used to ensure correct z-ordering for elements like tooltips.
    *   **Files:** `UI/UIManager.cs` (if it exists, otherwise `RunManager.cs` for UI instantiation), `UI/GlobalUIOverlay.cs` (if it exists).
    *   **Implication:**
        *   The admin panel would be managed by a new `AdminPanelController` script.
        *   This controller would be instantiated, potentially by `UIManager` or `RunManager`, and attached to the `GlobalUIOverlay`'s `rootVisualElement` to ensure it renders above other game UI.
        *   It would be toggled visible/hidden, similar to how the simple debug console would be.
        *   It would need access to `GameSession`, `RunManager`, `MapManager`, and `GameDataRegistry` to perform its functions.

### Detailed Plan for Phase 2: Visual Admin Panel

**Goal:** Implement a visual admin panel using UI Toolkit that provides interactive controls for game state manipulation and scene/encounter management.

**Step 1: Design Admin Panel UI (UXML & USS)**

*   **Objective:** Create the visual layout and styling for the admin panel.
*   **Action:** Create `Assets/UI/AdminPanel.uxml`.
    *   Define a root `VisualElement` for the panel.
    *   Include sections for:
        *   **Game State:** `TextField`s/`Slider`s for Gold, Lives, Current HP, Max HP. Buttons for adding/removing items.
        *   **Scene/Encounter Control:** Buttons for loading specific scenes (Battle, Shop, Run, Summary). Dropdowns for selecting specific encounters. Buttons for skipping map nodes.
        *   **Player/Enemy Stats:** Display of current player/enemy stats.
    *   Use `VisualElement`s, `Label`s, `TextField`s, `Button`s, `DropdownField`s, `Slider`s as appropriate.
*   **Action:** Create `Assets/UI/AdminPanel.uss` for styling the admin panel (background, colors, layout, element styles).
*   **Verification:** The UXML and USS files will define the admin panel's appearance.

**Step 2: Implement `AdminPanelDataViewModel.cs`**

*   **Objective:** Create a ViewModel to expose game data to the Admin Panel UI and handle data updates.
*   **Action:** Create `Assets/Scripts/UI/AdminPanelDataViewModel.cs`.
    *   Implement `INotifyPropertyChanged`.
    *   Expose properties for:
        *   `Gold`, `Lives`, `CurrentHp`, `MaxHp` (from `GameSession`).
        *   `CurrentSceneName` (from `SceneManager`).
        *   `CurrentEncounterId` (from `GameSession`).
        *   Lists of available items, encounters, scenes (from `GameDataRegistry`).
    *   Implement methods to:
        *   `AddGold(int amount)`
        *   `AddLives(int amount)`
        *   `LoadScene(string sceneName)`
        *   `SkipNode()`
        *   `GiveItem(string itemId)`
    *   Subscribe to relevant `GameSession` events (`OnEconomyChanged`, `OnPlayerShipChanged`, `OnPlayerNodeChanged`) to update its properties and raise `PropertyChanged` events.
*   **Verification:** The ViewModel will provide data to the UI and react to game state changes.

**Step 3: Implement `AdminPanelController.cs`**

*   **Objective:** Manage the Admin Panel UI, bind it to the ViewModel, and handle user interactions.
*   **Action:** Create `Assets/Scripts/UI/AdminPanelController.cs`.
    *   Make it a `MonoBehaviour`.
    *   **Fields:**
        *   `[SerializeField] private VisualTreeAsset adminPanelUxml;`
        *   `[SerializeField] private StyleSheet adminPanelUss;`
        *   `private VisualElement _rootElement;`
        *   `private AdminPanelDataViewModel _viewModel;`
    *   **`Awake()` Method:**
        *   Instantiate the UXML and apply the USS.
        *   Initialize `_viewModel = new AdminPanelDataViewModel();` (passing `GameSession` or relevant services).
        *   Get references to UI elements from the instantiated UXML.
        *   Bind UI elements to `_viewModel` properties using `BindingUtility` (e.g., `BindingUtility.BindLabelText(goldLabel, _viewModel, nameof(_viewModel.Gold))`).
        *   Register callbacks for buttons and input fields to call methods on `_viewModel`.
        *   Set initial visibility to hidden.
    *   **`OnEnable()` / `OnDisable()` Methods:** Handle input actions for toggling the panel (can reuse the "ToggleConsole" action or add a new one).
*   **Verification:** The controller will manage the UI and its interactions with the ViewModel.

**Step 4: Integrate Admin Panel into the Game**

*   **Objective:** Ensure the admin panel is present and functional.
*   **Action:** Create a new prefab: `Assets/Prefabs/AdminPanel.prefab`.
    *   Attach `AdminPanelController.cs` to this prefab.
    *   Assign the `AdminPanel.uxml` and `AdminPanel.uss` assets.
*   **Action:** In the `Assets/Scenes/Boot.unity` scene:
    *   Add an instance of the `AdminPanel.prefab` to the scene.
    *   Ensure it persists across scene loads (e.g., `DontDestroyOnLoad`).
*   **Verification:** The admin panel will appear when toggled and allow interactive control.
