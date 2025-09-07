using UnityEngine;
using UnityEngine.UIElements;
using PirateRoguelike.Core; // For RunManager.OnToggleConsole
using PirateRoguelike.Data;
using UnityEngine.SceneManagement;
using System.Linq; // For .FirstOrDefault()
using Pirate.MapGen; // Corrected namespace for MapManager
using PirateRoguelike.Services;
using UnityEngine.InputSystem; // For UIStateService

namespace PirateRoguelike.UI
{
    public class DebugConsoleController : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset debugConsoleUxml;
        [SerializeField] private StyleSheet debugConsoleUss;
        public InputActionAsset inputActionAsset; // This field is no longer used, but kept for compatibility if needed elsewhere.

        private VisualElement _rootElement;
        private ScrollView _outputScrollView;
        private TextField _inputField;

        private bool _isConsoleVisible = false;

        private void Awake()
        {
            Debug.Log("DebugConsoleController: Awake() called. Starting initialization.");

            // --- Step 1: Instantiate the UXML ---
            if (debugConsoleUxml == null)
            {
                Debug.LogError("DebugConsoleController: debugConsoleUxml is NULL. This should not happen if assigned in Inspector.");
                return; // Critical error, cannot proceed
            }

            _rootElement = debugConsoleUxml.Instantiate();
            if (_rootElement == null)
            {
                Debug.LogError("DebugConsoleController: _rootElement is NULL after instantiating debugConsoleUxml. UXML instantiation failed.");
                return; // Critical error, cannot proceed
            }
            Debug.Log($"DebugConsoleController: _rootElement instantiated successfully. Type: {_rootElement.GetType().Name}");

            // --- Step 2: Apply the USS ---
            if (debugConsoleUss == null)
            {
                Debug.LogWarning("DebugConsoleController: debugConsoleUss is NULL. Styling might be incorrect.");
            }
            else
            {
                _rootElement.styleSheets.Add(debugConsoleUss);
                Debug.Log("DebugConsoleController: debugConsoleUss applied.");
            }

            // --- Step 3: Get references to UI elements ---
            _outputScrollView = _rootElement.Q<ScrollView>("output-scroll-view");
            if (_outputScrollView == null)
            {
                Debug.LogError("DebugConsoleController: Failed to find ScrollView with name 'output-scroll-view'. Check UXML structure.");
            }
            else
            {
                Debug.Log("DebugConsoleController: _outputScrollView found.");
            }

            _inputField = _rootElement.Q<TextField>("input-field");
            if (_inputField == null)
            {
                Debug.LogError("DebugConsoleController: Failed to find TextField with name 'input-field'. Check UXML structure.");
            }
            else
            {
                Debug.Log("DebugConsoleController: _inputField found.");
            }

            // --- Step 4: Set initial visibility ---
            _rootElement.style.display = DisplayStyle.None;
            _isConsoleVisible = false;
            Debug.Log("DebugConsoleController: Initial visibility set to hidden. Awake() finished.");
        }

        public void Initialize(VisualElement rootElementToAttachTo)
        {
            Debug.Log("DebugConsoleController: Initialize() called. Attaching _rootElement.");
            if (_rootElement == null)
            {
                Debug.LogError("DebugConsoleController: _rootElement is NULL in Initialize()! Cannot attach to hierarchy.");
                return;
            }
            rootElementToAttachTo.Add(_rootElement);
            Debug.Log("DebugConsoleController: _rootElement added to rootElementToAttachTo. Logging initial message.");
            Log("Debug Console Initialized. Press ` to toggle.");
        }

        private void OnEnable()
        {
            Debug.Log("DebugConsoleController: OnEnable() called. Subscribing to RunManager.OnToggleConsole.");
            RunManager.OnToggleConsole += ToggleConsole;

            if (_inputField != null)
            {
                _inputField.RegisterCallback<KeyDownEvent>(OnInputKeyDown, TrickleDown.TrickleDown);
                Debug.Log("DebugConsoleController: KeyDownEvent registered for _inputField with TrickleDown.");
            }
            else
            {
                Debug.LogWarning("DebugConsoleController: _inputField is NULL in OnEnable(), cannot register events.");
            }
            Debug.Log("DebugConsoleController: OnEnable() finished.");
        }

        private void OnDisable()
        {
            Debug.Log("DebugConsoleController: OnDisable() called. Unsubscribing from RunManager.OnToggleConsole.");
            RunManager.OnToggleConsole -= ToggleConsole;

            if (_inputField != null)
            {
                _inputField.UnregisterCallback<KeyDownEvent>(OnInputKeyDown);
            }
            Debug.Log("DebugConsoleController: OnDisable() finished.");
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            //Debug.Log($"DebugConsoleController: evt.keyCode{evt.keyCode}");
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                ProcessCommand(_inputField.value);
                _inputField.value = "";
                evt.PreventDefault(); // Prevent default TextField behavior
            }
        }

        private void ToggleConsole()
        {
            Debug.Log("DebugConsoleController: ToggleConsole() called. Current visibility: " + _isConsoleVisible);
            if (_rootElement == null)
            {
                Debug.LogError("DebugConsoleController: _rootElement is NULL in ToggleConsole()! Cannot toggle visibility.");
                return;
            }
            _isConsoleVisible = !_isConsoleVisible;
            _rootElement.style.display = _isConsoleVisible ? DisplayStyle.Flex : DisplayStyle.None;

            // Set the global state flag
            UIStateService.IsConsoleOpen = _isConsoleVisible;

            if (_isConsoleVisible)
            {
                if (_inputField != null)
                {
                    _inputField.Focus();
                }
                else
                {
                    Debug.LogWarning("DebugConsoleController: Console visible, but _inputField is NULL, cannot focus.");
                }
            }
            else
            {
                if (_inputField != null)
                {
                    _inputField.Blur(); // Remove focus when hiding
                    Debug.Log("DebugConsoleController: Console hidden, input field blurred.");
                }
                else
                {
                    Debug.LogWarning("DebugConsoleController: Console hidden, but _inputField is NULL, cannot blur.");
                }
            }
        }

        private void ProcessCommand(string command)
        {
            Log($"> {command}"); // Log the command entered by the user

            string[] parts = command.ToLower().Split(' ');
            string cmd = parts[0];
            int goldAmount; // Declare here
            switch (cmd)
            {
                case "help":
                    Log("Available commands:");
                    Log("  help - Displays this help message.");
                    Log("  addgold <amount> - Adds gold to the player's economy.");
                    Log("  addlives <amount> - Adds lives to the player's economy.");
                    Log("  loadscene <sceneName> - Loads a specified scene (e.g., MainMenu, Boot, Run, Battle, Summary).");
                    Log("  skipnode - Advances the player to the next node on the map.");
                    Log("  giveitem <itemId> - Gives the player a specified item.");
                    Log("  startencounter <encounterId> - Starts a specific battle encounter.");
                    Log("  generatereward [goldAmount=10] [showItems=true] <isElite=false> <floorIndex=5> - Generates a reward window for testing.");
                    break;

                case "addgold":
                    if (parts.Length == 2)
                    {
                        
                        if (int.TryParse(parts[1], out goldAmount)) // Check if parsing succeeds
                        {
                            if (GameSession.Economy != null)
                            {
                                GameSession.Economy.AddGold(goldAmount);
                                Log($"Added {goldAmount} gold. Current gold: {GameSession.Economy.Gold}");
                            }
                            else
                            {
                                Log("Error: GameSession.Economy is not initialized.");
                            }
                        }
                        else
                        {
                            Log("Usage: addgold <amount> (Amount must be a valid integer)");
                        }
                    }
                    else
                    {
                        Log("Usage: addgold <amount>");
                    }
                    break;

                case "addlives":
                    if (parts.Length == 2 && int.TryParse(parts[1], out int livesAmount))
                    {
                        if (GameSession.Economy != null)
                        {
                            GameSession.Economy.AddLives(livesAmount);
                            Log($"Added {livesAmount} lives. Current lives: {GameSession.Economy.Lives}");
                        }
                        else
                        {
                            Log("Error: GameSession.Economy is not initialized.");
                        }
                    }
                    else
                    {
                        Log("Usage: addlives <amount>");
                    }
                    break;

                case "loadscene":
                    if (parts.Length == 2)
                    {
                        string sceneName = parts[1].ToLower();
                        Log($"Attempting to load scene: {sceneName}...");

                        // FIX: If loading the shop directly, we must manually set the game state
                        // that the RunManager would normally set when entering a shop node.
                        if (sceneName == "shop")
                        {
                            if (GameSession.CurrentRunState != null)
                            {
                                GameSession.CurrentRunState.NextShopItemCount = 3; // Set a default item count for debug purposes
                                Log("Set NextShopItemCount to 3 for debug shop load.");
                            }
                            else
                            {
                                Log("Error: Cannot set shop item count because GameSession is not active.");
                            }
                        }

                        SceneManager.LoadScene(sceneName);
                    }
                    else
                    {
                        Log("Usage: loadscene <sceneName>");
                    }
                    break;

                case "skipnode":
                    if (GameSession.CurrentRunState != null && MapManager.Instance != null)
                    {
                        // Find the current node
                        var mapData = MapManager.Instance.GetMapGraphData();
                        if (mapData == null)
                        {
                            Log("Error: Map data not available to skip node.");
                            return;
                        }

                        var currentNode = mapData.nodes.FirstOrDefault(n => n.id == GameSession.CurrentRunState.currentEncounterId);
                        if (currentNode == null)
                        {
                            Log("Error: Current node not found in map data. Cannot skip.");
                            return;
                        }

                        // Find a connected node in the next column
                        var nextNode = mapData.edges
                            .Where(e => e.fromId == currentNode.id)
                            .Select(e => mapData.nodes.FirstOrDefault(n => n.id == e.toId))
                            .FirstOrDefault(n => n != null && n.row == currentNode.row + 1);

                        if (nextNode != null)
                        {
                            GameSession.CurrentRunState.currentEncounterId = nextNode.id;
                            GameSession.CurrentRunState.currentColumnIndex = nextNode.row;
                            GameSession.InvokeOnPlayerNodeChanged();
                            Log($"Skipped to next node: {nextNode.id} (Row: {nextNode.row})");
                        }
                        else
                        {
                            Log("Could not find a next node to skip to. Perhaps at end of map?");
                        }
                    }
                    else
                    {
                        Log("Error: GameSession or MapManager not initialized for skipnode command.");
                    }
                    break;

                case "giveitem":
                    if (parts.Length == 2)
                    {
                        string itemId = parts[1];
                        ItemSO item = GameDataRegistry.GetItem(itemId);
                        if (item != null)
                        {
                            if (GameSession.Inventory != null)
                            {
                                if (GameSession.Inventory.AddItem(new ItemInstance(item)))
                                {
                                    Log($"Given item: {item.displayName}");
                                }
                                else
                                {
                                    Log($"Failed to add item {item.displayName} to inventory (full?).");
                                }
                            }
                            else
                            {
                                Log("Error: GameSession.Inventory is not initialized.");
                            }
                        }
                        else
                        {
                            Log($"Error: Item with ID '{itemId}' not found in GameDataRegistry.");
                        }
                    }
                    else
                    {
                        Log("Usage: giveitem <itemId>");
                    }
                    break;

                case "startencounter":
                    if (parts.Length == 2)
                    {
                        string encounterId = parts[1];
                        EncounterSO encounter = GameDataRegistry.GetEncounter(encounterId);
                        if (encounter != null)
                        {
                            if (GameSession.CurrentRunState != null)
                            {
                                GameSession.CurrentRunState.currentEncounterId = encounterId;
                                Log($"Starting encounter: {encounterId}");
                                SceneManager.LoadScene("Battle");
                            }
                            else
                            {
                                Log("Error: GameSession is not active. Please start a run first.");
                            }
                        }
                        else
                        {
                            Log($"Error: Encounter with ID '{encounterId}' not found in GameDataRegistry.");
                        }
                    }
                    else
                    {
                        Log("Usage: startencounter <encounterId>");
                    }
                    break;

                case "generatereward":
                    goldAmount = 10; // Default gold
                    bool showItems = true; // Default to showing items
                    bool isElite = false; // Default to non-elite
                    int floorIndex = 5; // Default floor index
                    int? itemCount = 3; // Default item count if showing items

                    if (parts.Length > 1)
                    {
                        // Parse goldAmount
                        int parsedGoldAmount;
                        if (!int.TryParse(parts[1], out parsedGoldAmount))
                        {
                            Log("Usage: generatereward [goldAmount] [showItems] [isElite] [floorIndex]");
                            return;
                        }
                        goldAmount = parsedGoldAmount;
                    }

                    if (parts.Length > 2)
                    {
                        // Parse showItems
                        bool parsedShowItems;
                        if (!bool.TryParse(parts[2], out parsedShowItems))
                        {
                            Log("Usage: generatereward [goldAmount] [showItems] [isElite] [floorIndex]");
                            return;
                        }
                        showItems = parsedShowItems;
                        if (!showItems) itemCount = 0; // If not showing items, set count to 0
                    }

                    if (parts.Length > 3)
                    {
                        // Parse isElite
                        bool parsedIsElite;
                        if (!bool.TryParse(parts[3], out parsedIsElite))
                        {
                            Log("Usage: generatereward [goldAmount] [showItems] [isElite] [floorIndex]");
                            return;
                        }
                        isElite = parsedIsElite;
                    }

                    if (parts.Length > 4)
                    {
                        // Parse floorIndex
                        int parsedFloorIndex;
                        if (!int.TryParse(parts[4], out parsedFloorIndex))
                        {
                            Log("Usage: generatereward [goldAmount] [showItems] [isElite] [floorIndex]");
                            return;
                        }
                        floorIndex = parsedFloorIndex;
                    }

                    // If showItems is true and itemCount was not explicitly set to 0 by parsing, ensure it's 3
                    if (showItems && itemCount == null) itemCount = 3;

                    RewardService.GenerateDebugReward(floorIndex, isElite, goldAmount, itemCount);

                    // Explicitly show the reward UI
                    if (UIManager.Instance != null && UIManager.Instance.RewardUIController != null)
                    {
                        UIManager.Instance.RewardUIController.ShowRewards(
                            RewardService.GetCurrentRewardItems().Select(item => new ItemInstance(item).ToSerializable()).ToList(),
                            RewardService.GetCurrentRewardGold()
                        );
                    }
                    else
                    {
                        Log("Error: UIManager or RewardUIController is not initialized. Cannot show debug rewards.");
                    }
                    Log($"Generated reward: Gold: {goldAmount}, Items: {(showItems ? (itemCount?.ToString() ?? "default") : "none")}, Elite: {isElite}, Floor: {floorIndex}.");
                    break;

                default:
                    Log($"Unknown command: {command}. Type 'help' for a list of commands.");
                    break;
            }
        }

        private void Log(string message)
        {
            if (_outputScrollView != null)
            {
                Label newLogEntry = new Label(message);
                _outputScrollView.Add(newLogEntry);
                // Manually scroll to bottom
                _outputScrollView.schedule.Execute(() => _outputScrollView.scrollOffset = new Vector2(0, _outputScrollView.contentContainer.layout.height));
            }
            else
            {
                Debug.LogWarning($"DebugConsole: Attempted to log '{message}' but _outputScrollView is NULL.");
            }
        }
    }
}