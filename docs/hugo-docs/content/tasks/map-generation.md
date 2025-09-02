---
title: "Map Generation Task"
weight: 10
system: ["map"]
types: ["task", "plan", "refactoring", "feature"]
tags: ["MapGenerator", "MapManager", "NodeType", "UnknownNodeResolver", "Rules"]
stage: ["Planned"] # e.g., "Planned", "InProgress", "Completed", "Blocked"
---

# Map Generation Plan

## Assumptions:
*   The project is a Unity project, and all implementations will be in C#.
*   The map generation will result in a graph-like structure of interconnected nodes, organized into rows.
*   The total number of rows (R) will be provided as an input parameter.
*   A `Rules` ScriptableObject will encapsulate all configuration data required for map generation and `Unknown` node resolution.
*   The final output will be a data structure representing the typed map and a validation report detailing any rule violations.
*   I am responsible for implementing the generation logic, the `Unknown` node resolution, and associated tests.

## High-Level Plan:
1.  **Refactor `MapGenerator.ApplyTypingConstraints`**:
    *   Introduce a new method, `AssignNodeTypesWeighted`, to implement the weighted odds and re-rolls for node typing.
    *   This method will respect row bans, dynamic elite unlock rules, and adjacency rules during the re-roll attempts.
    *   Ensure fixed rows (Monster/Battle, Treasure, Port, Boss) are assigned first and are immutable.
2.  **Implement `UnknownNodeResolver`**:
    *   Create a dedicated class/method to handle the runtime resolution of `Unknown` nodes.
    *   This resolver will incorporate the pity system, respecting structural bans but ignoring adjacency rules during resolution.
    *   Manage and persist pity counts per act.
3.  **Strict Fixed Row Enforcement**:
    *   Modify `MapGenerator` to explicitly set all nodes in Row 0 to `Battle` (assuming `Monster` maps to `Battle` `NodeType`).
    *   Ensure all nodes in the calculated Treasure row (`⌈0.6R⌉`) are `Treasure`.
    *   Ensure all nodes in the pre-boss Port row (`R-1`) are `Port`.
4.  **Dynamic Elite Unlock Row**:
    *   Update `MapGenerator` to calculate the Elite unlock row dynamically based on `⌈0.35R⌉`.
5.  **Integrate Adjacency Rules**:
    *   Enhance the weighted typing process to enforce "no consecutive" (Elite, Shop, Port) and "children must be different types" rules.
6.  **Update `MapManager.ConvertMapGraphToMapGraphData`**:
    *   Adjust `EncounterSO` assignment for fixed rows and `Unknown` nodes to correctly reflect the new resolution mechanism.
