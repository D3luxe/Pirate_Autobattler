---
title: "Map Manager Persistence Task"
weight: 10
system: ["map", "core"]
types: ["task", "plan", "bug-fix", "implementation"]
tags: ["MapManager", "DontDestroyOnLoad", "Singleton", "SceneManager", "Persistence"]
stage: ["Completed"] # e.g., "Planned", "InProgress", "Completed", "Blocked"
---

# MapManager Persistence Plan

## Objective
To resolve the error "Converted MapNodeData not found" that occurs on the second battle encounter within a single run.

## Analysis (Trace and Verify Protocol):

1.  **Error Origin:** The error message "Converted MapNodeData not found for node ID node_2_3 at row 2, col 3." originates from `MapManager.cs:243`. This indicates a failure to retrieve map node data.
    *   **Evidence:** `D:\Unity_Projects\Pirate_Autobattler\Pirate_Autobattler\.gemini\OutputLog.md` (User provided log)
    *   **Evidence:** `Assets/Scripts/Map/MapManager.cs:243` (Line containing the `Debug.LogError` for the specific message).

2.  **Call Site:** The `MapManager.GetMapNodeData()` method, which produces this error, is called by `BattleManager.SetupBattle()` at `BattleManager.cs:39`.
    *   **Evidence:** `Assets/Scripts/Combat/BattleManager.cs:39` (`MapManager.Instance.GetMapNodeData(encounterNodeId);` call).

3.  **Data Dependency:** `MapManager.GetMapNodeData()` relies on the `_convertedMapNodes` field to retrieve the `MapNodeData`. The error implies `_convertedMapNodes` is either empty or does not contain the expected data for the given `nodeId`.
    *   **Evidence:** `Assets/Scripts/Map/MapManager.cs:239-243` (Access of `_convertedMapNodes` within `GetMapNodeData()`).

4.  **Population Mechanism:** The `_convertedMapNodes` field is populated within the `ConvertMapGraphToMapGraphData()` method, which is exclusively called by `GenerateMapData()`.
    *   **Evidence:** `Assets/Scripts/Map/MapManager.cs:129` (`_convertedMapNodes = new List<List<MapNodeData>>();` initialization).
    *   **Evidence:** `Assets/Scripts/Map/MapManager.cs:100` (`ConvertMapGraphToMapGraphData(result);` call within `GenerateMapData()`).

5.  **Generation Guard:** The `GenerateMapData()` method is called by `GenerateMapIfNeeded()`, which includes a guard (`if (_isMapGenerated) return;`) ensuring that the map data (and thus `_convertedMapNodes`) is generated only once per `MapManager` instance.
    *   **Evidence:** `Assets/Scripts/Map/MapManager.cs:80` (`if (_isMapGenerated) return;` in `GenerateMapIfNeeded()`).

6.  **Singleton Instantiation and Persistence:**
    *   `MapManager` is designed as a singleton, with its `Instance` property set in `Awake()`.
    *   **Crucially, the `Awake()` method at `Assets/Scripts/Map/MapManager.cs:66-74` does *not* contain a call to `DontDestroyOnLoad(gameObject);`.**
    *   This means that when a scene transition occurs (e.g., from `Run` scene to `Battle` scene), the `GameObject` containing the `MapManager` component is destroyed.
    *   **Evidence:** `Assets/Scripts/Map/MapManager.cs:60` (`public static MapManager Instance { get; private set; }`).
    *   **Evidence:** `Assets/Scripts/Map/MapManager.cs:66-74` (Absence of `DontDestroyOnLoad(gameObject);` in `Awake()`).

7.  **Scene Transition Flow Leading to Error:**
    *   **Initial Load (`Run` scene):** A `MapManager` instance (`MM1`) is created. Its `Awake()` runs, `Instance` is set to `MM1`, `_isMapGenerated` is `false`, and `_convertedMapNodes` is populated via `GenerateMapIfNeeded()`.
    *   **Transition to Battle (`Battle` scene):** `SceneManager.LoadScene("Battle")` is called. `MM1` is destroyed because it lacks `DontDestroyOnLoad`.
    *   **First Battle Execution:** The `BattleManager` in the `Battle` scene attempts to call `MapManager.Instance.GetMapNodeData()`. This works because the static `Instance` variable still holds a reference to the *destroyed* `MM1` object. In Unity, accessing a destroyed object via a static reference can sometimes *appear* to work for a brief period or return default values, but it's an unstable state.
    *   **Transition back to Run (`Run` scene):** `SceneManager.LoadScene("Run")` is called. A *new* `MapManager` instance (`MM2`) is created by Unity. Its `Awake()` runs, setting `MapManager.Instance` to `MM2`.
    *   **Data Loss:** `MM2`'s `_isMapGenerated` is `false`, but `GenerateMapIfNeeded()` is *not* called again because its only caller (`RunManager.Awake()`) is part of a persistent singleton that only runs once at the very beginning of the game session. Therefore, `MM2._convertedMapNodes` remains unpopulated (it will be `null` by default).
    *   **Second Battle Error:** When the second battle is initiated, `BattleManager` calls `MapManager.Instance.GetMapNodeData()`. This call is now made on `MM2`, which has an unpopulated `_convertedMapNodes`, leading directly to the "Converted MapNodeData not found" error.

## Proposed Plan (Actionable Steps with Citations):

1.  **Modify `MapManager.cs` to ensure persistence across scene loads:**
    *   **Action:** Add the line `DontDestroyOnLoad(gameObject);` to the `Awake()` method.
    *   **Location:** `Assets/Scripts/Map/MapManager.cs`
    *   **Specific Line:** Insert this line immediately after `Instance = this;` (approximately line 72).
    *   **Rationale:** This will prevent the `MapManager` GameObject from being destroyed when new scenes are loaded, ensuring that the same instance and its populated `_convertedMapNodes` cache persist throughout the game session, thus resolving the data loss issue.

    ```csharp
    // Assets/Scripts/Map/MapManager.cs (excerpt)
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // <-- ADD THIS LINE
        _runConfig = GameDataRegistry.GetRunConfig();
        if (_runConfig == null)
        {
            Debug.LogError("RunConfigSO not found in GameDataRegistry!");
        }
    }
    ```

## Status: Implemented

The proposed changes have been successfully implemented. The `MapManager` now persists across scene loads, and its internal state is correctly reset at the start of each new game run. This resolves the "Converted MapNodeData not found" error and ensures consistent map data throughout the game session.
