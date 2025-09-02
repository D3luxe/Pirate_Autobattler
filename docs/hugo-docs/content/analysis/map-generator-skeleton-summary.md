---
title: "Generate Skeleton Summary"
weight: 10
system: ["map"]
types: ["analysis", "summary","system-overview"]
status: "archived"
discipline: ["engineering"]
stage: ["production"]
---

# `GenerateSkeleton` Function Summary

## Problem/Topic

Summary of the `GenerateSkeleton` function, part of the `MapGenerator` class, responsible for Phase A of the map generation process: creating the basic layered Directed Acyclic Graph (DAG) skeleton of the map.

## Analysis

### 1. Node Creation:

The function iterates through each row, from `r = 0` to `actSpec.Rows - 1`.
*   For the first row (`r = 0`) and the last row (`r = actSpec.Rows - 1`), exactly one node is created.
*   For all intermediate rows (`0 < r < actSpec.Rows - 1`), the number of nodes created is determined randomly. This number ranges from 2 up to `actSpec.Columns - 1`. The specific number is calculated using `rng.NextULong() % (ulong)(actSpec.Columns - 2)) + 2`.
*   Within each row, nodes are assigned a `Col` index sequentially, starting from `0` up to `nodesInRow - 1`. This `Col` value represents the node's horizontal index within that specific row, not a fixed column position across the entire graph.
*   Each node is assigned a unique ID (`node_row_col`), its `Row` and `Col` indices, and an initial `NodeType.Unknown` (as the actual type of the node is determined in a later phase).

### 2. Edge Wiring:

The function iterates through each row (excluding the last) to connect nodes between the current and the next row.
*   **Ensuring Incoming Connections:** For every node in the *next* row, it ensures there is at least one incoming connection from a randomly selected node in the current row.
*   **Ensuring Outgoing Connections:** For every node in the *current* row, it ensures there is at least one outgoing connection to a randomly selected node in the next row.
*   **Adding Branching Connections:** Based on the `actSpec.Branchiness` (a value between 0 and 1), it adds additional random connections from nodes in the current row to nodes in the next row. This increases the "branchiness" or complexity of the map by creating multiple paths.

### 3. Boss Node Assignment:

Finally, it identifies the single node in the very last row (`actSpec.Rows - 1`) and explicitly sets its `Type` to `NodeType.Boss`. This marks the end point of the map.

## Conclusion/Recommendations

The function returns a `MapGraph` object, which contains the generated nodes and their connections (edges), forming the structural foundation for the subsequent map generation phases.
