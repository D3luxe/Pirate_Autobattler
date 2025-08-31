
using UnityEngine;
using UnityEngine.SceneManagement;
using PirateRoguelike.Data;
using PirateRoguelike.UI; // Import the UI namespace
using UnityEngine.UIElements;
using UnityEngine.InputSystem; // NEW: Input System namespace

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private RunConfigSO runConfig;
    [SerializeField] private ShipSO debugStartingShip; // For testing
    [SerializeField] private GameObject playerPanelPrefab; // NEW: Player Panel UI
    [SerializeField] private GameObject mapViewPrefab; // NEW: Map View UI Prefab
    [SerializeField] private GameObject rewardUIPrefab;
    [SerializeField] private GameObject mapManagerPrefab;
    [SerializeField] private GameObject tooltipManagerPrefab; // NEW: Tooltip Manager Prefab

    private PlayerPanelController _playerPanelController;
    private MapView _mapView;
    private TooltipController _tooltipController; // NEW: Tooltip Controller reference

    private InputAction _saveHotkeyAction; // NEW: Input Action for save hotkey

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // NEW: Initialize save hotkey action
        _saveHotkeyAction = new InputAction("SaveGame", type: InputActionType.Button, binding: "<Keyboard>/s");
        _saveHotkeyAction.performed += OnSaveHotkeyPerformed;

        // Instantiate MapManager
        if (mapManagerPrefab != null)
        {
            Instantiate(mapManagerPrefab, transform);
            // Generate map data immediately after MapManager is instantiated
            if (MapManager.Instance != null)
            {
                // Pass the seed from GameSession to MapManager
                if (GameSession.CurrentRunState != null)
                {
                    MapManager.Instance.GenerateMapIfNeeded(GameSession.CurrentRunState.randomSeed);
                }
                else
                {
                    Debug.LogError("GameSession.CurrentRunState is null in RunManager.Awake()! Cannot generate map with seed.");
                }
            }
            else
            {
                Debug.LogError("MapManager Instance is null after instantiation!");
            }
        }
        else
        {
            Debug.LogError("MapManager Prefab is not assigned in RunManager!");
        }

        

        // Instantiate MapView UI from prefab
        if (mapViewPrefab != null)
        {
            GameObject mapViewInstance = Instantiate(mapViewPrefab, transform);
            _mapView = mapViewInstance.GetComponent<MapView>();
            // The UIDocument reference will now be handled by the prefab setup
        }
        else
        {
            Debug.LogError("MapView Prefab is not assigned in RunManager!");
        }

        if (_playerPanelController != null && _mapView != null)
        {
            _playerPanelController.SetMapPanel(_mapView);
        }

        if (rewardUIPrefab != null)
        {
            Instantiate(rewardUIPrefab, transform);
        }
        else
        {
            Debug.LogError("RewardUI Prefab is not assigned in RunManager!");
        }

        

        // Instantiate MapView UI from prefab
        if (mapViewPrefab != null)
        {
            GameObject mapViewInstance = Instantiate(mapViewPrefab, transform);
            _mapView = mapViewInstance.GetComponent<MapView>();
            // The UIDocument reference will now be handled by the prefab setup
        }
        else
        {
            Debug.LogError("MapView Prefab is not assigned in RunManager!");
        }

        if (_playerPanelController != null && _mapView != null)
        {
            _playerPanelController.SetMapPanel(_mapView);
        }

        // Instantiate PlayerPanel UI
        if (playerPanelPrefab != null)
        {
            GameObject panelInstance = Instantiate(playerPanelPrefab, transform);
            _playerPanelController = panelInstance.GetComponent<PlayerPanelController>();
        }
        else
        {
            Debug.LogError("PlayerPanel Prefab is not assigned in RunManager!");
        }

        // Instantiate TooltipManager UI
        if (tooltipManagerPrefab != null)
        {
            GameObject tooltipInstance = Instantiate(tooltipManagerPrefab, transform);
            _tooltipController = tooltipInstance.GetComponent<TooltipController>();
        }
        else
        {
            Debug.LogError("TooltipManager Prefab is not assigned in RunManager!");
        }

        // Initialize TooltipController after both PlayerPanel and TooltipManager are instantiated
        if (_tooltipController != null && _playerPanelController != null)
        {
            UIDocument playerPanelUIDocument = _playerPanelController.GetComponent<UIDocument>();
            if (playerPanelUIDocument != null)
            {
                _tooltipController.Initialize(playerPanelUIDocument.rootVisualElement);
            }
            else
            {
                Debug.LogError("PlayerPanel Prefab does not have a UIDocument component!");
            }
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        GameSession.OnPlayerNodeChanged += HandlePlayerNodeChanged; // NEW: Subscribe to node change event
    }

    void OnEnable()
    {
        _saveHotkeyAction.Enable(); // NEW: Enable the input action
    }

    void OnDisable()
    {
        _saveHotkeyAction.Disable(); // NEW: Disable the input action
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameSession.OnPlayerNodeChanged -= HandlePlayerNodeChanged; // NEW: Unsubscribe from node change event
        _saveHotkeyAction.performed -= OnSaveHotkeyPerformed; // NEW: Unsubscribe from event
        _saveHotkeyAction.Dispose(); // NEW: Dispose the input action
        if (Instance == this) // Only reset if this is the persistent instance
        {
            GameSession.EndRun();
        }
    }

    private void OnSaveHotkeyPerformed(InputAction.CallbackContext context)
    {
        if (GameSession.CurrentRunState != null)
        {
            GameSession.UpdateCurrentRunStateForSaving(); // Update the run state with current inventory and equipped items
            SaveManager.SaveRun(GameSession.CurrentRunState);
            Debug.Log("Game saved via hotkey!");
        }
        else
        {
            Debug.LogWarning("Cannot save: No active run state.");
        }
    }

    void Start()
    {
        // This will be called only once when the RunManager is first created.
        if (GameSession.CurrentRunState == null)
        {
            Debug.Log("No active run found. Starting a new debug run.");
            if (runConfig == null || debugStartingShip == null)
            {
                Debug.LogError("RunConfig or DebugStartingShip is not assigned in the RunManager Inspector!");
                return;
            }
            GameSession.StartNewRun(runConfig, debugStartingShip);
        }

        // Now that the GameSession is guaranteed to be initialized, initialize the UI.
        if (_playerPanelController != null)
        {
            _playerPanelController.Initialize();
        }

        // Initialize TooltipController after PlayerPanel is initialized
        if (_tooltipController != null && _playerPanelController != null)
        {
            UIDocument playerPanelUIDocument = _playerPanelController.GetComponent<UIDocument>(); // Get UIDocument from the instantiated PlayerPanel
            if (playerPanelUIDocument != null)
            {
                _tooltipController.Initialize(playerPanelUIDocument.rootVisualElement);
            }
            else
            {
                Debug.LogError("PlayerPanel Prefab does not have a UIDocument component!");
            }
        }
    }

    private void HandlePlayerNodeChanged()
    {
        if (GameSession.CurrentRunState == null || string.IsNullOrEmpty(GameSession.CurrentRunState.currentEncounterId))
        {
            Debug.LogWarning("HandlePlayerNodeChanged called with invalid GameSession state.");
            return;
        }

        var mapData = MapManager.Instance.GetMapGraphData();
        if (mapData == null)
        {
            Debug.LogError("MapGraphData is null. Cannot handle node change.");
            return;
        }

        var currentNode = mapData.nodes.Find(n => n.id == GameSession.CurrentRunState.currentEncounterId);
        if (currentNode == null)
        {
            Debug.LogError($"Could not find node with ID: {GameSession.CurrentRunState.currentEncounterId}");
            return;
        }

        if (System.Enum.TryParse<PirateRoguelike.Data.EncounterType>(currentNode.type, true, out var encounterType))
        {
            Debug.Log($"Player moved to node {currentNode.id} of type {encounterType}.");

            switch (encounterType)
            {
                case PirateRoguelike.Data.EncounterType.Battle:
                    // Before loading, hide the map to prevent it from overlapping the next scene
                    if (_mapView != null) _mapView.Hide();
                    SceneManager.LoadScene("Battle");
                    break;
                case PirateRoguelike.Data.EncounterType.Shop:
                    if (_mapView != null) _mapView.Hide();
                    SceneManager.LoadScene("Shop");
                    break;
                case PirateRoguelike.Data.EncounterType.Treasure:
                    Debug.Log("Treasure encounter triggered. Implementation pending.");
                    // Future: Grant rewards directly or show a simple reward UI
                    break;
                case PirateRoguelike.Data.EncounterType.Boss:
                     if (_mapView != null) _mapView.Hide();
                    SceneManager.LoadScene("Battle"); // Assuming boss fights use the same scene
                    break;
                case PirateRoguelike.Data.EncounterType.Elite:
                     if (_mapView != null) _mapView.Hide();
                    SceneManager.LoadScene("Battle"); // Elite fights also use the Battle scene
                    break;
                default:
                    Debug.LogWarning($"Unhandled encounter type: {encounterType}");
                    break;
            }
        }
        else
        {
            Debug.LogError($"Failed to parse encounter type: {currentNode.type}");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Run")
        {
            OnRunSceneLoaded();
        }
    }

    private void OnRunSceneLoaded()
    {
        // Check for battle rewards first thing
        if (GameSession.CurrentRunState != null && GameSession.CurrentRunState.battleRewards != null && GameSession.CurrentRunState.battleRewards.Count > 0)
        {
            Debug.Log($"RunManager: Detected {GameSession.CurrentRunState.battleRewards.Count} battle rewards.");
            // RewardUI logic remains for now
        }

        if (MapManager.Instance != null)
        {
            if (GameSession.CurrentRunState != null)
            {
                MapManager.Instance.GenerateMapIfNeeded(GameSession.CurrentRunState.randomSeed);
            }
            else
            {
                Debug.LogError("GameSession.CurrentRunState is null in RunManager.OnRunSceneLoaded()! Cannot generate map with seed.");
            }
        }
        else
        {
            Debug.LogError("MapManager Instance is null!");
        }

        if (GameSession.Economy != null)
        {
            Debug.Log($"Run scene initialized. Gold: {GameSession.Economy.Gold}, Lives: {GameSession.Economy.Lives}");
        }
        else
        {
            Debug.LogError("GameSession.Economy is null!");
        }

        // Call Show() on the MapView to trigger auto-scrolling
        if (_mapView != null)
        {
            _mapView.Show();
        }
        else
        {
            Debug.LogError("MapView instance is null in OnRunSceneLoaded!");
        }
    }
}



