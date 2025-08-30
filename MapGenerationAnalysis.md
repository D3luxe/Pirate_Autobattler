# Map Generation System Analysis

This document provides a deep dive into the map generation implementation within the Pirate Autobattler project, focusing on `Assets/Scripts/Map/MapManager.cs` and `Assets/Scripts/MapGeneration/MapGenerator.cs`. The analysis aims to identify redundancies, clarify `NodeType` assignment, and understand current limitations.

## 1. Overview of the Map Generation Process

The map generation process is orchestrated by `MapManager` and executed by `MapGenerator`, following a three-phase approach:

*   **Phase A: Skeleton Generation (`MapGenerator.GenerateSkeleton`)**: Creates the basic graph structure (nodes and edges) without specific encounter types.
*   **Phase B: Typing Under Constraints (`MapGenerator.ApplyTypingConstraints`)**: Assigns `NodeType` to the generated nodes based on predefined rules and constraints.
*   **Phase C: Validation & Repair (`MapGenerator.GenerateMap`'s internal loop)**: Validates the generated map against rules and attempts to repair any violations.

After generation, `MapManager` converts the `MapGraph` into game-usable data structures (`MapGraphData` and `List<List<MapNodeData>>`) and maps `NodeTypes` to specific `EncounterSOs`.

## 2. Key Parameters and Configuration

### 2.1. `MapManager.cs` Parameters

These are the public or hardcoded parameters directly influencing the `MapGenerator`:

*   **`mapLength` (int)**:
    *   **Purpose**: Defines the total number of rows (depth) in the generated map.
    *   **Usage**: Directly passed as `ActSpec.Rows` to `MapGenerator`.
    *   **Default**: 15.
*   **`nodesPerColumnRange` (Vector2Int)**:
    *   **Purpose**: Intended to define the minimum and maximum number of nodes per column (row).
    *   **Usage**: Only `nodesPerColumnRange.y` (the maximum value) is used, passed as `ActSpec.Columns` to `MapGenerator`. The `x` component (minimum value) is currently **unused**.
*   **`Branchiness` (float)**:
    *   **Purpose**: Controls the density of connections (edges) between nodes in successive rows. A higher value means more branching paths.
    *   **Usage**: Hardcoded to `0.5f` within `MapManager.GenerateMapData`. It is **not exposed** as a public configurable parameter in `MapManager`.
*   **`_runConfig.rules` (RulesSO)**:
    *   **Purpose**: This `ScriptableObject` holds the primary configuration for node counts, spacing rules, and special row windows. It is loaded from `GameDataRegistry`.
    *   **Usage**: Passed directly to `MapGenerator.GenerateMap`.

### 2.2. `RulesSO` Parameters (Consumed by `MapGenerator`)

The `RulesSO` object provides detailed constraints for map generation:

*   **`Rules.Windows.MidTreasureRows` (List<int>)**:
    *   **Purpose**: Specifies the row indices where a `Treasure` node is eligible to be placed.
*   **`Rules.Counts.Targets` (Dictionary<NodeType, int>)**:
    *   **Purpose**: Defines the *total desired count* for each `NodeType` across the entire map.
    *   **Important Note**: These counts represent the *overall target*, and are decremented by nodes that are guaranteed to be placed (e.g., Boss, Pre-boss Port, Mid-act Treasure).
*   **`Rules.Spacing`**:
    *   **`EliteMinGap` (int)**: Minimum row separation required between `Elite` nodes.
    *   **`ShopMinGap` (int)**: Minimum row separation required between `Shop` nodes.
    *   **`PortMinGap` (int)**: Minimum row separation required between `Port` nodes.
    *   **`EliteEarlyRowsCap` (int)**: A row index. `Elite` nodes are prioritized for placement in rows *after* this cap. A fallback mechanism exists to place elites in earlier rows if the desired count cannot be met otherwise, potentially violating this cap.

## 3. `NodeType` Assignment Logic (`MapGenerator.ApplyTypingConstraints`)

This phase is critical for determining the type of each node. Nodes are assigned types in a specific order:

1.  **Boss Node**:
    *   The node in the last row (`actSpec.Rows - 1`) is explicitly set to `NodeType.Boss`.
    *   **Redundancy**: This node is also initially set to `NodeType.Boss` during `GenerateSkeleton` (Phase A). While harmless, it's a redundant assignment.
2.  **Pre-boss Port**:
    *   One `Port` node is guaranteed to be placed in the row immediately preceding the boss row (`actSpec.Rows - 2`). It is tagged `"preBossPort"`.
3.  **Mid-act Treasure**:
    *   One `Treasure` node is guaranteed to be placed in one of the rows specified by `rules.Windows.MidTreasureRows`. It is tagged `"midActTreasure"`.
4.  **Elites**:
    *   Placed based on `rules.Counts.Targets[NodeType.Elite]`.
    *   Placement prioritizes later rows first, respecting `rules.Spacing.EliteEarlyRowsCap`.
    *   `rules.Spacing.EliteMinGap` is enforced.
    *   If `actSpec.Flags.EnableBurningElites` is true, one `Elite` node is tagged `"burning"`.
    *   A fallback loop attempts to place remaining elites in earlier rows if the desired count is not met, even if it means placing them before `EliteEarlyRowsCap`.
5.  **Shops**:
    *   Placed based on `rules.Counts.Targets[NodeType.Shop]`.
    *   `rules.Spacing.ShopMinGap` is enforced.
6.  **Ports (Remaining)**:
    *   Placed based on `rules.Counts.Targets[NodeType.Port]` (after the pre-boss port has been placed and its count decremented).
    *   `rules.Spacing.PortMinGap` is enforced.
7.  **Unknowns**:
    *   If `rules.Counts.Targets[NodeType.Unknown]` is greater than zero, remaining unplaced nodes are assigned `NodeType.Unknown` until the target count is met.
8.  **Battles**:
    *   All remaining unplaced nodes (after all other specific types and `Unknown` nodes have been assigned) are assigned `NodeType.Battle`. A warning is logged if this occurs, suggesting that `rules.Counts.Targets` might need adjustment to include more non-Battle or `Unknown` nodes.

## 4. Validation and Repair (`MapGenerator.GenerateMap` - Phase C)

The system attempts to validate and repair the generated map up to `maxRepairIterations` (default 50).

*   **Prioritized Violations and Repairs**:
    1.  **"No valid path from start to boss."**: If this critical connectivity issue is detected, the entire skeleton generation and typing process is re-run.
    2.  **"No Port node found on the row immediately before the Boss."**: An available node in the pre-boss row is re-typed to `Port`.
    3.  **"No Treasure node found within the specified mid-act window."**: An available node in the mid-act window is re-typed to `Treasure`.
    4.  **Other Violations (Generic Fallback)**: For any other violations, a random unplaced node is re-typed to `Battle`. This is a very general repair mechanism and may not effectively address specific count or spacing violations.

## 5. `NodeType` to `EncounterSO` Mapping (`MapManager.ConvertMapGraphToMapGraphData`)

After `MapGenerator` assigns `NodeTypes`, `MapManager` is responsible for mapping these types to actual game content (`EncounterSO`):

*   `NodeType.Battle`: Mapped to a non-elite `Battle` encounter (`e.type == EncounterType.Battle && !e.isElite`).
*   `NodeType.Elite`: Mapped to an `Elite` encounter (`e.type == EncounterType.Elite`).
*   `NodeType.Boss`: Mapped to a specific encounter by ID: `"enc_boss"`.
*   `NodeType.Shop`: Mapped to a specific encounter by ID: `"enc_shop"`.
*   `NodeType.Event`: Mapped to a specific encounter by ID: `"enc_event"`.
*   `NodeType.Unknown`: Mapped to a specific encounter by ID: `"enc_unknown"`.
*   **Default/Fallback**: If no specific mapping is found, it defaults to a non-elite `Battle` encounter.

## 6. Identified Redundancies and Clarifications

*   **`nodesPerColumnRange.x` Unused**: The minimum value for nodes per column is defined in `MapManager` but never used in `ActSpec` or `MapGenerator`. This parameter is redundant in its current form.
*   **`Branchiness` Hardcoding**: The `Branchiness` value is hardcoded in `MapManager`. If this is intended to be a configurable game parameter, it should be exposed in the Unity Inspector or through `RunConfigSO`.
*   **Boss Node Double Assignment**: The `NodeType.Boss` is assigned to the last node in both `GenerateSkeleton` and `ApplyTypingConstraints`. While not a functional bug, it's redundant code.
*   **`NodeType` String Conversion**: In `MapManager.ConvertMapGraphToMapGraphData`, `NodeType` is converted to a string and then immediately parsed back to `NodeType`. This string conversion is unnecessary and adds overhead; direct `NodeType` usage would be more efficient if `MapGraphData.Node.type` could directly store the enum.
*   **Hardcoded `EncounterSO` Mapping**: The `switch` statement in `MapManager.ConvertMapGraphToMapGraphData` hardcodes the mapping from `NodeType` to specific `EncounterSO` IDs or search criteria. This makes it less flexible for content designers to define which encounters correspond to which node types without code changes.
*   **`RulesSO.Counts.Targets` Interpretation**: It's crucial to understand that `Rules.Counts.Targets` represents the *total desired count* for a `NodeType`, and this count is reduced by any guaranteed placements (Boss, Pre-boss Port, Mid-act Treasure). This means content designers need to account for these guaranteed placements when setting target counts.
*   **Elite Placement Fallback Behavior**: The fallback loop for placing elites can potentially violate `EliteEarlyRowsCap` if the desired count is high and there are not enough eligible nodes in later rows. This behavior should be understood when configuring elite counts and early row caps.
*   **Generic Repair Fallback**: The repair mechanism's fallback to re-typing a random node as `Battle` for unhandled violations is very generic. More specific repair logic might be needed for other types of count or spacing violations to ensure the map adheres more closely to the `RulesSO` specifications.

This analysis provides a comprehensive understanding of the current map generation system, highlighting its parameters, flow, and areas for potential refinement.
