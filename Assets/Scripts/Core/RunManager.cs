using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using PirateRoguelike.Data;
using UnityEngine.InputSystem;
using PirateRoguelike.Saving;
using Pirate.MapGen;
using PirateRoguelike.UI;
using PirateRoguelike.Services; // Added

namespace PirateRoguelike.Core
{
    public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }
    public static event Action OnToggleConsole;

    [Header("Configuration")]
    [SerializeField] private RunConfigSO runConfig;
    [SerializeField] private ShipSO debugStartingShip; // For testing
    [SerializeField] private GameObject mapManagerPrefab;

    [Header("Input")]
    public InputActionAsset inputActionAsset;
    private InputAction _saveHotkeyAction;
    private InputAction _toggleConsoleAction;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (inputActionAsset != null)
        {
            var debugMap = inputActionAsset.FindActionMap("Debug");
            if (debugMap != null)
            {
                _saveHotkeyAction = debugMap.FindAction("SaveGame");
                _saveHotkeyAction.performed += OnSaveHotkeyPerformed;

                _toggleConsoleAction = debugMap.FindAction("ToggleConsole");
                _toggleConsoleAction.performed += HandleToggleConsole;

                //debugMap.Enable(); // Enable the whole map
            }
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        GameSession.OnPlayerNodeChanged += HandlePlayerNodeChanged;
    }

    public void Initialize()
    {
        if (mapManagerPrefab != null)
        {
            Instantiate(mapManagerPrefab, transform);
        }
        else
        {
            Debug.LogError("MapManager Prefab is not assigned in RunManager!");
        }
    }

    void OnEnable()
    {
        _saveHotkeyAction.Enable();
    }
    void OnDisable()
    {
        _saveHotkeyAction.Disable();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameSession.OnPlayerNodeChanged -= HandlePlayerNodeChanged;
        _saveHotkeyAction.performed -= OnSaveHotkeyPerformed;
        _saveHotkeyAction.Dispose();
        if (Instance == this)
        {
            GameSession.EndRun();
        }
    }

    private void OnSaveHotkeyPerformed(InputAction.CallbackContext context)
    {
        if (GameSession.CurrentRunState != null)
        {
            GameSession.UpdateCurrentRunStateForSaving();
            SaveManager.SaveRun(GameSession.CurrentRunState);
            Debug.Log("Game saved via hotkey!");
        }
        else
        {
            Debug.LogWarning("Cannot save: No active run state.");
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
                case PirateRoguelike.Data.EncounterType.Boss:
                case PirateRoguelike.Data.EncounterType.Elite:
                    SceneManager.LoadScene("Battle");
                    break;
                case PirateRoguelike.Data.EncounterType.Shop:
                    // Retrieve the EncounterSO for the current node
                    PirateRoguelike.Data.EncounterSO shopEncounter = GameDataRegistry.GetEncounter(currentNode.id);
                    if (shopEncounter != null)
                    {
                        GameSession.CurrentRunState.NextShopItemCount = shopEncounter.shopItemCount;
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find shop encounter SO for node ID: {currentNode.id}. Using default shop item count.");
                        GameSession.CurrentRunState.NextShopItemCount = 3; // Fallback to default
                    }
                    SceneManager.LoadScene("Shop");
                    break;
                case PirateRoguelike.Data.EncounterType.Treasure:
                    // Treasure is handled in the Run scene directly without a scene change.
                    // TODO: Create and call RewardService.GenerateTreasureReward
                    // RewardService.GenerateTreasureReward(GameSession.CurrentRunState.currentColumnIndex);
                    if (ServiceLocator.Resolve<RewardUIController>() != null)
                    {
                        // TODO: Create a method to show only treasure rewards
                        // ServiceLocator.Resolve<RewardUIController>().ShowRewards(RewardService.GetCurrentRewardItems(), RewardService.GetCurrentRewardGold());
                        Debug.Log("Treasure encounter triggered. UI Implementation pending.");
                    }
                    else
                    {
                        Debug.LogError("RewardUIController not available to show treasure reward.");
                    }
                    break;
                case PirateRoguelike.Data.EncounterType.Port:
                    SceneManager.LoadScene("Port");
                    break;
                case PirateRoguelike.Data.EncounterType.Event:
                    var eventEncounter = GameDataRegistry.GetEncounter(currentNode.encounterId);
                    if (eventEncounter != null)
                    {
                        GameSession.DebugEncounterToLoad = eventEncounter; // Use the debug hook to pass the specific encounter
                        SceneManager.LoadScene("Event");
                    }
                    else
                    {
                        Debug.LogError($"Could not find EncounterSO with ID '{currentNode.encounterId}' for node '{currentNode.id}'.");
                    }
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
        // Always clear the debug encounter when returning to the run scene.
        if (GameSession.DebugEncounterToLoad != null)
        {
            GameSession.DebugEncounterToLoad = null;
        }

        if (GameSession.CurrentRunState != null && GameSession.CurrentRunState.battleRewards != null && GameSession.CurrentRunState.battleRewards.Count > 0)
        {
            Debug.Log($"RunManager: Detected {GameSession.CurrentRunState.battleRewards.Count} battle rewards.");
            // Show Reward UI via ServiceLocator
            if (ServiceLocator.Resolve<RewardUIController>() != null)
            {
                ServiceLocator.Resolve<RewardUIController>().ShowRewards(GameSession.CurrentRunState.battleRewards, RewardService.GetCurrentRewardGold());
                GameSession.CurrentRunState.battleRewards = null; // Clear rewards after showing
            }
            else
            {
                Debug.LogError("RewardUIController is null. Cannot show battle rewards.");
            }
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

        // Initialize and show UI now that all data is ready
        ServiceLocator.Resolve<PlayerPanelController>().Initialize(new GameSessionWrapper());
        ServiceLocator.Resolve<MapView>().Show();
    }

    private void HandleToggleConsole(InputAction.CallbackContext context)
    {
        OnToggleConsole?.Invoke();
    }
}
}