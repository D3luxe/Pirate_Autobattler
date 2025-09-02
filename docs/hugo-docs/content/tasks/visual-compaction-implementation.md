---
title: "Visual Compaction Implementation Task"
weight: 10
system: ["ui", "map"]
types: ["task", "plan", "implementation", "visual", "algorithm"]
tags: ["MapView", "MapPanel", "UXML", "USS", "MapNode", "Visual Layout", "Barycentric Sort", "Bezier Curve", "UI Toolkit"]
stage: ["Completed"] # e.g., "Planned", "InProgress", "Completed", "Blocked"
---

# Visual Row Compaction Implementation Plan

## 1. Objective

This document outlines the plan to implement a render-time visual layout pass for the map nodes, as described in the `Gemini_Visual_Row_Compaction_Prompt.md`. The goal is to center and evenly space nodes within each row to create a more organic and visually appealing map, without altering the underlying game logic based on lanes and rows.

## 2. Analysis of Existing System

An analysis of the current map system files reveals that the existing structure is well-suited for this new rendering approach.

*   **`Assets/UI/MapPanel.uxml`**: The structure uses a `ScrollView` containing a `MapCanvas` element. This `MapCanvas` is the container where nodes and edges are drawn.
*   **`Assets/UI/MapView.uss`**: The stylesheet confirms that both `.map-canvas` and `.map-node` elements use `position: absolute`. This is a critical prerequisite, as the new algorithm relies on programmatically setting the `x` and `y` coordinates of each node. The current implementation does not use a grid or flex layout for nodes, which means the new positioning logic can be integrated smoothly.
*   **`Assets/Scripts/UI/MapView.cs`**: This script is the logical place to implement the changes. It is responsible for taking the `MapGraph` data and generating the `VisualElement` nodes on the `MapCanvas`. The new algorithm will be a new positioning logic within this script.

The current setup aligns perfectly with the UI Toolkit notes in the prompt, making the implementation a matter of adding a new layout calculation pass before rendering the nodes.

## 3. Implementation Steps

The implementation will be contained primarily within `Assets/Scripts/UI/MapView.cs`. The core idea is to calculate all node positions first and then render them in a separate pass.

### Step 1: Data Structures for Visual Positioning

In `MapView.cs`, we will need a place to store the calculated visual positions separate from the logical `MapNode` data. A dictionary is suitable for this.

```csharp
// In MapView.cs
private Dictionary<string, Vector2> visualNodePositions = new Dictionary<string, Vector2>();
```

This dictionary will map a node's ID to its calculated `visualX` and `visualY` coordinates.

### Step 2: Modify the Map Rendering Pipeline

The main map generation method (likely named `RenderMap` or similar in `MapView.cs`) will be updated to follow this sequence:
1.  Clear existing nodes and edges.
2.  **Calculate all visual positions.** (New Logic)
3.  Create and place `VisualElement`s for each node using the calculated positions.
4.  Draw the Bezier curve edges using the calculated positions.

### Step 3: Implement the Visual Layout Algorithm

This is the core of the task. We will create a new method, e.g., `CalculateVisualLayout(MapGraph mapGraph)`, that implements the algorithm from the prompt.

1.  **Group Nodes by Row**:
    *   Create a `Dictionary<int, List<MapNode>>` to hold the nodes for each row.
    *   Iterate through `mapGraph.nodes` and populate this dictionary.

2.  **Process Rows Sequentially (from top to bottom)**:
    *   Loop from `row = 0` to the maximum row number.
    *   **Barycentric Sort**: For the current row's nodes (`row > 0`), sort them based on the average `visualX` of their parent nodes. The parent positions will have been calculated in the previous iteration. For `row == 0`, sort by the original `lane` index.
    *   **Calculate Row Geometry**:
        *   Get the count `k` of nodes in the current (sorted) row.
        *   Calculate the `span`, spacing `s`, and `left` offset as per the algorithm to center the nodes.
    *   **Place Nodes and Apply Jitter**:
        *   Iterate through the sorted nodes of the current row.
        *   Calculate the base `x` position: `left + (j * s)`.
        *   Apply a deterministic jitter using `UnityEngine.Random` seeded with the map's decoration seed.
        *   Enforce `minSep` by ensuring the current node's `x` is not too close to the previous one.
        *   Calculate `visualY` based on the row index and `rowHeight`.
        *   Store the final `(visualX, visualY)` in the `visualNodePositions` dictionary.

### Step 4: Update Node and Edge Rendering

1.  **Node Placement**:
    *   After `CalculateVisualLayout` completes, the `visualNodePositions` dictionary will be fully populated.
    *   When creating the `VisualElement` for each node, retrieve its position from the dictionary and apply it to the element's style:
      ```csharp
      var nodeElement = new VisualElement();
      nodeElement.AddToClassList("map-node");
      // ... other classes
      Vector2 pos = visualNodePositions[node.Id];
      nodeElement.style.left = pos.x;
      nodeElement.style.top = pos.y;
      ```

2.  **Edge Rendering**:
    *   The existing edge drawing logic, which likely uses a `Painter2D` to draw Bezier curves, will be updated.
    *   When drawing an edge from a parent to a child, retrieve both nodes' positions from the `visualNodePositions` dictionary.
    *   Use these visual positions as the start and end points for the Bezier curve.
    *   Set the Bezier control points to create the desired "S" curve, with a vertical offset of approximately `0.4 * rowHeight`.

### Step 5: Configuration

Expose the rendering constants (`contentWidth`, `gutters`, `maxRowSpan`, `rowHeight`, `padY`, `minSep`, `jitterAmp`) as serialized fields in the `MapView.cs` inspector to allow for easy iteration and design tweaks without changing code.

## 4. Expected Outcome

The result will be a map where nodes in each row are dynamically centered and spaced based on how many nodes are in that row, rather than being fixed to static "lane" positions. This will produce a more aesthetically pleasing, less rigid map layout while preserving the original graph structure for all gameplay and pathfinding logic.
