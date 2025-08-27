
using UnityEngine;
using UnityEngine.SceneManagement;
using PirateRoguelike.Data;
using PirateRoguelike.UI; // Import the UI namespace
using UnityEngine.UIElements;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private RunConfigSO runConfig;
    [SerializeField] private ShipSO debugStartingShip; // For testing
    [SerializeField] private GameObject playerPanelPrefab; // NEW: Player Panel UI
    [SerializeField] private GameObject mapPanelPrefab; // NEW: Map Panel UI Prefab
    [SerializeField] private GameObject rewardUIPrefab;
    [SerializeField] private GameObject mapManagerPrefab;

    private PlayerPanelController _playerPanelController;
    private MapPanel _mapPanel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Instantiate MapManager
        if (mapManagerPrefab != null)
        {
            Instantiate(mapManagerPrefab, transform);
            // Generate map data immediately after MapManager is instantiated
            if (MapManager.Instance != null)
            {
                MapManager.Instance.GenerateMapIfNeeded();
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

        // Instantiate MapPanel UI from prefab
        if (mapPanelPrefab != null)
        {
            GameObject mapPanelInstance = Instantiate(mapPanelPrefab, transform);
            _mapPanel = mapPanelInstance.GetComponent<MapPanel>();
            // The UIDocument reference will now be handled by the prefab setup
        }
        else
        {
            Debug.LogError("MapPanel Prefab is not assigned in RunManager!");
        }

        if (_playerPanelController != null && _mapPanel != null)
        {
            _playerPanelController.SetMapPanel(_mapPanel);
        }

        if (rewardUIPrefab != null)
        {
            Instantiate(rewardUIPrefab, transform);
        }
        else
        {
            Debug.LogError("RewardUI Prefab is not assigned in RunManager!");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
            MapManager.Instance.GenerateMapIfNeeded();
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
    }
}



