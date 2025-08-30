# Map Generation Design

This document outlines the architecture of the map generation system.

## Core Philosophy

The map generator operates on a **Sparse-Path-First** philosophy. This means the primary goal is to first create a structurally sound and valid skeleton of paths for the player, and only then to apply node types and other gameplay elements. This approach ensures that every generated map is guaranteed to be completable, as the core path network is validated before any gameplay rules are applied.

This is a departure from a "dense grid" approach, where a full grid of nodes is created and then culled. Here, we build up from a valid foundation.

## Generation Phases

The process is divided into two main phases:

### Phase A: Skeleton Generation (`GenerateSkeleton`)

1.  **Path Generation:** The system generates exactly 6 distinct paths from the top row to the bottom row.
2.  **Path Rules:**
    *   Paths are composed of nodes and edges connecting them between adjacent rows.
    *   Edges are not allowed to cross visually between rows unless they share a common start or end node. This allows for clean splits and merges.
    *   All paths are guaranteed to converge on the single Boss node in the final row. This is strictly enforced during generation.
3.  **Internal Validation & Hardening:** The `GenerateSkeleton` method is self-contained and hardened. After generating the 6 paths, it performs an internal validation check to ensure that all of its own rules (especially the boss connection) have been met. If the generated skeleton is found to be invalid, it is discarded entirely, and the process restarts from scratch. This guarantees that `GenerateSkeleton` only ever outputs a 100% structurally valid graph.
4.  **Pruning:** Once the 6 valid paths are established, all nodes and edges that are not part of this network are pruned from the graph.

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
