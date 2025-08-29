using System;
using System.Collections.Generic;
using UnityEngine;
using PirateRoguelike.Data;
using Pirate.MapGen; // NEW: Added for MapGraphData

public static class GameSession
{
    public static RunState CurrentRunState { get; set; }
    public static EconomyService Economy { get; set; }
    public static Inventory Inventory { get; set; }
    public static ShipState PlayerShip { get; set; }

    // Flags for loading saved games
    public static bool ShouldLoadSavedGame { get; set; } = false;
    public static RunState SavedRunStateToLoad { get; set; } = null;

    public static event Action OnPlayerNodeChanged; // New event

    public static void InvokeOnPlayerNodeChanged()
    {
        OnPlayerNodeChanged?.Invoke();
    }

    public static void EndRun()
    {
        CurrentRunState = null;
        Economy = null;
        Inventory = null;
        PlayerShip = null;
    }

    // Called at the start of a new run
    public static void StartNewRun(RunConfigSO config, ShipSO startingShip)
    {
        CurrentRunState = new RunState
        {
            playerLives = config.startingLives, // Initialize lives from config
            gold = config.startingGold, // Initialize gold from config
            currentColumnIndex = -1, // Start before the first column
            // currentEncounterNode and currentEncounterId will be set when entering first encounter
            playerShipState = new ShipState(startingShip).ToSerializable(),
            inventoryItems = new List<SerializableItemInstance>(),
            mapGraphData = new MapGraphData(), // Initialize with an empty MapGraphData
            randomSeed = (ulong)System.DateTime.Now.Ticks, // Use ulong for seed and System.DateTime.Now.Ticks for initial randomness
            rerollsThisShop = 0 // Initialize reroll count
        };
        Debug.Log("Starting a new run. RNG seed: " + CurrentRunState.randomSeed.ToString());
        // Convert ulong seed to int for Unity's Random.InitState by XORing upper and lower 32 bits
        int unitySeed = (int)(CurrentRunState.randomSeed & 0xFFFFFFFF) ^ (int)(CurrentRunState.randomSeed >> 32);
        UnityEngine.Random.InitState(unitySeed); // Initialize Unity's random with the main seed
        Economy = new EconomyService(config, CurrentRunState);
        Inventory = new Inventory(config.inventorySize);
        PlayerShip = new ShipState(startingShip);

        // Add starter items to inventory
        ItemSO starterItem1 = GameDataRegistry.GetItem("item_cannon_wood");
        if (starterItem1 != null) Inventory.AddItem(new ItemInstance(starterItem1));
        ItemSO starterItem2 = GameDataRegistry.GetItem("item_deckhand");
        if (starterItem2 != null) Inventory.AddItem(new ItemInstance(starterItem2));
    }

    // Called to load a saved run
    public static void LoadRun(RunState loadedState, RunConfigSO config)
    {
        CurrentRunState = loadedState;

        Economy = new EconomyService(config, CurrentRunState);

        Inventory = new Inventory(config.inventorySize);
        foreach (var itemData in CurrentRunState.inventoryItems)
        {
            Inventory.AddItem(new ItemInstance(itemData));
        }

        PlayerShip = new ShipState(CurrentRunState.playerShipState);

        Debug.Log("Loading a run. RNG seed: " + CurrentRunState.randomSeed.ToString());
        // Convert ulong seed to int for Unity's Random.InitState by XORing upper and lower 32 bits
        int unitySeed = (int)(CurrentRunState.randomSeed & 0xFFFFFFFF) ^ (int)(CurrentRunState.randomSeed >> 32);
        UnityEngine.Random.InitState(unitySeed);
        // Reconstruct enemy ship if in battle
        if (CurrentRunState.enemyShipState != null)
        {
            // This assumes CombatController will handle setting its Enemy property
            // when the Battle scene loads.
        }

        // Reconstruct map in MapManager
        // MapManager.Instance.SetMapNodes(CurrentRunState.mapNodes);
    }

    public static void EndBattle(bool playerWon, RunConfigSO config, ShipState currentEnemyShipState = null)
    {
        // Save current state before processing battle outcome and loading new scene
        Economy.SaveToRunState(CurrentRunState);
        UpdateCurrentRunStateForSaving(); // Call the new update method

        // If in battle, save enemy state
        if (currentEnemyShipState != null)
        {
            CurrentRunState.enemyShipState = currentEnemyShipState.ToSerializable();
        }
        else
        {
            CurrentRunState.enemyShipState = null; 
        }

        if (playerWon)
        {
            int currentDepth = CurrentRunState.currentColumnIndex;
            int goldReward = config.rewardGoldPerWin + (currentDepth / 2);
            Economy.AddGold(goldReward);
            Debug.Log($"Player won! Gold: {Economy.Gold}");

            // Generate item rewards
            int mapLength = MapManager.Instance != null ? MapManager.Instance.mapLength : 1; // Get mapLength from MapManager
            
            // Determine if the current encounter was Elite
            bool isEliteEncounter = false;
            if (CurrentRunState.mapGraphData != null && !string.IsNullOrEmpty(CurrentRunState.currentEncounterId))
            {
                var currentNode = CurrentRunState.mapGraphData.nodes.Find(n => n.id == CurrentRunState.currentEncounterId);
                if (currentNode != null && System.Enum.TryParse<PirateRoguelike.Data.EncounterType>(currentNode.type, true, out var encounterType))
                {
                    isEliteEncounter = (encounterType == PirateRoguelike.Data.EncounterType.Elite);
                }
            }

            List<ItemSO> itemRewards = RewardService.GenerateBattleRewards(currentDepth, config, mapLength, isEliteEncounter);
            List<SerializableItemInstance> serializableItemRewards = new List<SerializableItemInstance>();
            foreach (var itemSO in itemRewards)
            {
                serializableItemRewards.Add(new ItemInstance(itemSO).ToSerializable());
            }
            CurrentRunState.battleRewards = serializableItemRewards;

            // --- DEBUG.LOG STATEMENTS FOR BATTLE REWARDS --- START
            if (CurrentRunState.battleRewards != null)
            {
                Debug.Log($"GameSession.EndBattle: battleRewards generated. Count: {CurrentRunState.battleRewards.Count}");
                foreach (var reward in CurrentRunState.battleRewards)
                {
                    Debug.Log($"  Reward Item ID: {reward.itemId}");
                }
            }
            else
            {
                Debug.Log("GameSession.EndBattle: battleRewards is null after generation.");
            }
            // --- DEBUG.LOG STATEMENTS FOR BATTLE REWARDS --- END

            // TODO: The Run scene should now handle showing the reward UI based on CurrentRunState.battleRewards.
            if (RunManager.Instance == null)
            {
                Debug.LogWarning("RunManager instance not found. Loading from Boot scene to ensure initialization.");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Boot");
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Run");
            }
        }
        else
        {
            Economy.LoseLife();
            Debug.Log($"Player lost! Lives remaining: {Economy.Lives}");
            if (Economy.Lives <= 0)
            {
                Debug.Log("GAME OVER!");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Summary"); // Load Summary scene on game over
            }
            else
            {
                // If lost but not game over, return to map
                UnityEngine.SceneManagement.SceneManager.LoadScene("Run");
            }
        }

        // Save the updated run state
        SaveManager.SaveRun(CurrentRunState);

        // The scene loading is now handled within the if/else for win/loss
    }

    public static void UpdateCurrentRunStateForSaving()
    {
        CurrentRunState.playerShipState = PlayerShip.ToSerializable();

        List<SerializableItemInstance> inventorySerializable = new List<SerializableItemInstance>();
        foreach (var item in Inventory.Items)
        {
            if (item != null)
            {
                inventorySerializable.Add(item.ToSerializable());
            }
        }
        CurrentRunState.inventoryItems = inventorySerializable;
    }
}
