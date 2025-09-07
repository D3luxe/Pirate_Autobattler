using UnityEngine;
using UnityEngine.SceneManagement;
using PirateRoguelike.Data;
using PirateRoguelike.Services; // Added for ItemManipulationService
using Pirate.MapGen;
using PirateRoguelike.UI;
using UnityEngine.UIElements;

namespace PirateRoguelike.Core
{
    public class GameInitializer : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject runManagerPrefab;
        [SerializeField] private GameObject uiManagerPrefab; // ADDED BACK
        [SerializeField] private RunConfigSO runConfig;
        [SerializeField] private ShipSO debugStartingShip;

        [Header("UI Prefabs")]
        [SerializeField] private GameObject globalUIOverlayPrefab;
        [SerializeField] private GameObject playerPanelPrefab;
        [SerializeField] private GameObject mapViewPrefab;
        [SerializeField] private GameObject tooltipManagerPrefab;
        [SerializeField] private GameObject debugConsolePrefab;
        [SerializeField] private GameObject rewardUIPrefab;

        [Header("UI Asset Registry")]
        [SerializeField] private UIAssetRegistry uiAssetRegistry;

        void Start()
        {
            // Initialize core systems
            AbilityManager.Initialize();
            ItemManipulationService.Instance.Initialize(new PirateRoguelike.Core.GameSessionWrapper());

            // Determine whether to load a saved game or start a new one
            if (GameSession.ShouldLoadSavedGame)
            {
                if (GameSession.SavedRunStateToLoad != null)
                {
                    if (runConfig == null)
                    {
                        Debug.LogError("GameInitializer: RunConfigSO is not assigned. Cannot load saved game.");
                        return;
                    }
                    GameSession.LoadRun(GameSession.SavedRunStateToLoad, runConfig);
                    GameSession.ShouldLoadSavedGame = false;
                    GameSession.SavedRunStateToLoad = null;
                }
                else
                {
                    Debug.LogError("GameInitializer: ShouldLoadSavedGame is true but SavedRunStateToLoad is null. Starting new game instead.");
                    if (runConfig == null || debugStartingShip == null)
                    {
                        Debug.LogError("GameInitializer: RunConfig or DebugStartingShip is not assigned for new game fallback!");
                        return;
                    }
                    if (MapManager.Instance != null) MapManager.Instance.ResetMap();
                    GameSession.StartNewRun(runConfig, debugStartingShip);
                }
            }
            else
            {
                if (runConfig == null || debugStartingShip == null)
                {
                    Debug.LogError("GameInitializer: RunConfig or DebugStartingShip is not assigned for new game!");
                    return;
                }
                if (MapManager.Instance != null) MapManager.Instance.ResetMap();
                GameSession.StartNewRun(runConfig, debugStartingShip);
            }

            // Instantiate persistent managers *after* GameSession is initialized
            if (runManagerPrefab != null && RunManager.Instance == null)
            {
                Instantiate(runManagerPrefab);
                RunManager.Instance.Initialize();
            }

            // Instantiate UIManager first
            UIManager uiManager = null;
            if (uiManagerPrefab != null)
            {
                GameObject uiManagerInstanceGO = Instantiate(uiManagerPrefab);
                uiManager = uiManagerInstanceGO.GetComponent<UIManager>();
                if (uiManager != null)
                {
                    ServiceLocator.Register<UIManager>(uiManager);
                }
                else
                {
                    Debug.LogError("UIManager Prefab does not have a UIManager component!");
                }
            }
            else
            {
                Debug.LogError("UIManager Prefab is not assigned in GameInitializer!");
            }

            // Instantiate and register other UI components as children of UIManager
            UIDocument globalUIOverlayDocument = null;
            if (globalUIOverlayPrefab != null && uiManager != null)
            {
                GameObject globalUIInstance = Instantiate(globalUIOverlayPrefab, uiManager.transform); // Make child of UIManager
                globalUIOverlayDocument = globalUIInstance.GetComponent<UIDocument>();
                if (globalUIOverlayDocument == null)
                {
                    Debug.LogError("GlobalUIOverlay Prefab does not have a UIDocument component!");
                }
            }
            else
            {
                Debug.LogError("GlobalUIOverlay Prefab is not assigned or UIManager is null in GameInitializer!");
            }

            GlobalUIService globalUIService = null;
            if (globalUIOverlayDocument != null && uiManager != null)
            {
                globalUIService = uiManager.gameObject.AddComponent<GlobalUIService>(); // Add to UIManager GO
                globalUIService.Initialize(globalUIOverlayDocument);
                ServiceLocator.Register<GlobalUIService>(globalUIService);
            }
            else
            {
                Debug.LogError("GlobalUIOverlay Document is null or UIManager is null. Cannot initialize GlobalUIService!");
            }

            PlayerPanelController playerPanelController = null;
            if (playerPanelPrefab != null && uiManager != null)
            {
                GameObject panelInstance = Instantiate(playerPanelPrefab, uiManager.transform); // Make child of UIManager
                playerPanelController = panelInstance.GetComponent<PlayerPanelController>();
                ServiceLocator.Register<PlayerPanelController>(playerPanelController);
            }
            else
            {
                Debug.LogError("PlayerPanel Prefab is not assigned or UIManager is null in GameInitializer!");
            }

            MapView mapView = null;
            if (mapViewPrefab != null && uiManager != null)
            {
                GameObject mapViewInstance = Instantiate(mapViewPrefab, uiManager.transform); // Make child of UIManager
                mapView = mapViewInstance.GetComponent<MapView>();
                ServiceLocator.Register<MapView>(mapView);
            }
            else
            {
                Debug.LogError("MapView Prefab is not assigned or UIManager is null in GameInitializer!");
            }

            TooltipController tooltipController = null;
            if (tooltipManagerPrefab != null && uiManager != null)
            {
                GameObject tooltipInstance = Instantiate(tooltipManagerPrefab, uiManager.transform); // Make child of UIManager
                tooltipController = tooltipInstance.GetComponent<TooltipController>();
                ServiceLocator.Register<TooltipController>(tooltipController);
            }
            else
            {
                Debug.LogError("TooltipManager Prefab is not assigned or UIManager is null in GameInitializer!");
            }

            DebugConsoleController debugConsoleController = null;
            if (debugConsolePrefab != null && uiManager != null)
            {
                GameObject debugConsoleInstance = Instantiate(debugConsolePrefab, uiManager.transform); // Make child of UIManager
                debugConsoleController = debugConsoleInstance.GetComponent<DebugConsoleController>();
                ServiceLocator.Register<DebugConsoleController>(debugConsoleController);
            }
            else
            {
                Debug.LogError("DebugConsole Prefab is not assigned or UIManager is null in GameInitializer!");
            }

            RewardUIController rewardUIController = null;
            if (rewardUIPrefab != null && uiManager != null)
            {
                GameObject rewardUIInstance = Instantiate(rewardUIPrefab, uiManager.transform); // Make child of UIManager
                rewardUIController = rewardUIInstance.GetComponent<RewardUIController>();
                ServiceLocator.Register<RewardUIController>(rewardUIController);
            }
            else
            {
                Debug.LogError("RewardUI Prefab is not assigned or UIManager is null in GameInitializer!");
            }

            if (uiAssetRegistry != null)
            {
                ServiceLocator.Register<UIAssetRegistry>(uiAssetRegistry);
            }
            else
            {
                Debug.LogError("UIAssetRegistry is not assigned in GameInitializer!");
            }

            // Initialize UIManager with resolved dependencies
            if (uiManager != null)
            {
                uiManager.Initialize(playerPanelController, mapView, tooltipController, globalUIOverlayDocument, debugConsoleController, rewardUIController);
            }
            else
            {
                Debug.LogError("UIManager instance is null. Cannot initialize UIManager!");
            }

            SceneManager.LoadScene("Run");
        }

        private void OnApplicationQuit()
        {
            // Ensure systems are shut down properly
            AbilityManager.Shutdown();
        }
    }
}