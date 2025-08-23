using UnityEngine;
using UnityEngine.SceneManagement;
using PirateRoguelike.Data;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private RunConfigSO runConfig;
    [SerializeField] private ShipSO debugStartingShip; // For testing
    [SerializeField] private GameObject inventoryUIPrefab;
    [SerializeField] private GameObject mapManagerPrefab;
    [SerializeField] private GameObject rewardUIPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Instantiate InventoryUI and MapManager prefabs and make them children of this GameObject
        if (inventoryUIPrefab != null)
        {
            Instantiate(inventoryUIPrefab, transform);
        }
        else
        {
            Debug.LogError("InventoryUI Prefab is not assigned in RunManager!");
        }

        if (mapManagerPrefab != null)
        {
            Instantiate(mapManagerPrefab, transform);
        }
        else
        {
            Debug.LogError("MapManager Prefab is not assigned in RunManager!");
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
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Run")
        {
            OnRunSceneLoaded();
        }
        else
        {
            // Hide the map when we are not in the Run scene
            if (MapUI.Instance != null)
            {
                MapUI.Instance.gameObject.SetActive(false);
            }
        }
    }

    private void OnRunSceneLoaded()
    {
        // Check for battle rewards first thing
        if (GameSession.CurrentRunState != null && GameSession.CurrentRunState.battleRewards != null && GameSession.CurrentRunState.battleRewards.Count > 0)
        {
            Debug.Log($"RunManager: Detected {GameSession.CurrentRunState.battleRewards.Count} battle rewards.");
            if (RewardUIController.Instance != null)
            {
                RewardUIController.Instance.ShowRewards(GameSession.CurrentRunState.battleRewards, GameSession.Economy.Gold);
                GameSession.CurrentRunState.battleRewards = null; // Clear rewards after showing
            }
            else
            {
                Debug.LogError("RewardUIController instance not found!");
            }
        }

        if (InventoryUI.Instance != null)
        {
            InventoryUI.Instance.Initialize();
            InventoryUI.Instance.SetInventoryVisibility(true, true); // Show both inventory and equipment
            InventoryUI.Instance.SetEquipmentInteractability(true);
        }
        else
        {
            Debug.LogError("InventoryUI Instance is null!");
        }

        if (MapManager.Instance != null)
        {
            MapManager.Instance.GenerateMapIfNeeded();
            if (MapUI.Instance != null)
            {
                MapUI.Instance.gameObject.SetActive(true);
                MapUI.Instance.RenderMap(MapManager.Instance.GetMapNodes());
            }
            else
            {
                Debug.LogError("MapUI Instance is null!");
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
    }
}
