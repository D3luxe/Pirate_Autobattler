---
title: "Map Generation Design"
weight: 10
system: ["map"]
types: ["system-overview"]
status: "approved"
---

# Map Generation Design

This document outlines the architecture of the map generation system.

## Core Philosophy

The map generator operates on a **Sparse-Path-First** philosophy. This means the primary goal is to first create a structurally sound and valid skeleton of paths for the player, and only then to apply node types and other gameplay elements. This approach ensures that every generated map is guaranteed to be completable, as the core path network is validated before any gameplay rules are applied.

This is a departure from a "dense grid" approach, where a full grid of nodes is created and then culled. Here, we build up from a valid foundation.

## Generation Phases

The process is divided into two main phases:

### Phase A: Skeleton Generation (`GenerateSkeleton`)

1.  **Initial Path Generation:** The system generates exactly 6 distinct paths from the top row to the bottom row. In this initial step, paths are allowed to connect to any node in the final row, and the backtracking algorithm is used to handle crossing violations for the main body of the map.

2.  **Boss Row Consolidation:** After the 6 initial paths are created, a deterministic, forceful cleanup process ensures the final structure is correct:
    *   **Rewiring:** All edges that lead into the final row are rewired to point to the single, central boss node.
    *   **Pruning (Final Row):** All nodes in the final row, except for the central boss node, are deleted.
    *   **Deduplication:** Any duplicate edges that were created as a result of the rewiring are merged, combining their path data.

3.  **Main Pruning:** With the boss row consolidated, a final pruning pass removes any other nodes and edges that are not part of the 6 completed, rewired paths.

This "generate-then-consolidate" approach is highly robust and guarantees that all 6 paths cleanly converge on the single boss node without complex or fragile pathfinding enforcement.

### Phase B: Typing & Decoration (`ApplyTypingConstraints`)

Once a valid skeleton is produced, this phase "decorates" the sparse graph with node types according to gameplay rules.

1.  **Guaranteed Nodes (`AssignFixedAndGuaranteedNodes`):** This step places critical gameplay nodes onto the existing skeleton.
    *   **Boss:** The single node in the final row is assigned the `Boss` type.
    *   **Pre-Boss Ports:** All nodes on the row immediately preceding the boss are assigned the `Port` type. This provides a guaranteed rest stop.
    *   **Mid-Act Treasure:** One `Treasure` node is guaranteed to be placed on a random path within a specified row window (e.g., rows 4-6). This is done by selecting one of the existing nodes on a path, not by converting an entire row.
    *   **First Row:** All nodes in the first row (the starting points of the paths) are assigned the `Battle` type.
2.  **Weighted Random Typing:** All remaining nodes in the skeleton that have not been given a guaranteed type are then assigned a type (`Battle`, `Elite`, `Shop`, etc.) based on a weighted random selection process that respects adjacency rules and other constraints defined in the `RulesSO`.

### Phase C: Validation & Repair

After typing, a final validation pass is performed to check for gameplay rule violations (e.g., two `Elite` nodes in a row). A repair system makes minor attempts to fix these issues by re-typing individual nodes. Because the skeleton is guaranteed to be structurally sound, this phase no longer needs to worry about critical pathing issues like the map being incompletable.
