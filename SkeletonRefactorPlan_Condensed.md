# Condensed Refactoring Plan for `GenerateSkeleton` Function

This document outlines the essential rules, intended functionality, and actionable steps to refactor the `GenerateSkeleton` function in `Assets/Scripts/MapGeneration/MapGenerator.cs`.

## Core Functional Rules (New Specification)

The refactored `GenerateSkeleton` must adhere to the following:

*   **Grid:** Operate on a fixed `C x R` grid, where `C = actSpec.Columns` and `R = actSpec.Rows`. Node `Col` (`c`) must represent its global column index (`0` to `C-1`).
*   **Path Generation:** Generate exactly **six monotone upward paths** from `r=0` to `rTop`.
    *   **Start:** Each path starts at `r=0` in a randomly chosen column. The first two paths must start in *different* columns.
    *   **Step:** From `(r, c)`, move to one of the **three nearest columns** on row `r+1` (by `|c' - c|`). Random tie-breaking applies.
    *   **No Crossings:** New edges `(r, c) → (r+1, c')` must not cross any existing edges between `r` and `r+1`. Merging at nodes and reusing identical edges are allowed.
*   **Pruning:** After all 6 paths are generated, remove all nodes and edges that are not part of at least one of these paths.
*   **Failure Handling:** Implement bounded backtracking. If a path cannot proceed, backtrack. If backtracking exhausts options, restart the current path (with a cap on restarts).
*   **Determinism:** All randomness must use the provided `IRandomNumberGenerator` for all choices and tie-breaking.

## Key Discrepancies from Current Implementation

*   The current implementation uses variable node counts per row and random connections, which is incompatible with the fixed grid and structured path generation required.
*   The current `GenerateSkeleton` explicitly assigns the `Boss` node type; this must be removed as it's a typing concern (Phase B), not skeleton generation (Phase A).

## Actionable Refactoring Steps

### I. Initial Setup and Data Structures

1.  **Modify Node Creation:** Change the initial node creation loop in `GenerateSkeleton` to create `actSpec.Columns` nodes for *every* row. Ensure `Node.Col` is set to the global column index `c`.
2.  **Remove Boss Node Assignment:** Delete the lines at the end of `GenerateSkeleton` that explicitly set the `NodeType.Boss`.
3.  **Introduce Helper Data Structures:** Declare `HashSet<string> usedNodeIds`, `HashSet<string> usedEdgeIds`, `List<Tuple<int, int>>[] edgesByRow` (for crossing checks), and a `Stack<PathStep>` (for backtracking).

### II. Path Generation Logic

1.  **Outer Loop:** Replace the entire existing edge wiring logic with a loop that runs 6 times to generate each path.
2.  **Path Generation (per path):**
    *   Implement a path restart mechanism with a counter.
    *   **Start Column Selection:** Select a `startCol` for each path, ensuring the first two paths have different `startCol` values.
    *   **Row Iteration:** For each row `r` from `0` to `actSpec.Rows - 2`:
        *   **Candidate Selection:** Get up to three nearest columns on row `r+1` from the `currentCol`.
        *   **Crossing Check:** Filter these candidates to remove any that would cross existing edges in `edgesByRow[r]`.
        *   **Decision & Edge Addition:** If valid candidates remain, choose one, add the edge to `graph.Edges`, mark nodes/edges as `used`, and update `edgesByRow[r]`. Push the step onto `pathDecisionStack`.
        *   **Backtracking:** If no valid candidates, trigger backtracking: pop from `pathDecisionStack`, remove the corresponding edge, and try remaining alternatives. If the stack empties, restart the current path.

### III. Pruning

1.  **Post-Generation Pruning:** After all 6 paths are generated, iterate through `graph.Nodes` and `graph.Edges`. Remove any node or edge that is not present in `usedNodeIds` or `usedEdgeIds` respectively.

### IV. Helper Methods

Add the following private helper methods to the `MapGenerator` class:

1.  **`private List<int> GetThreeNearestCandidates(int currentCol, int totalColumns, IRandomNumberGenerator rng)`:** Returns up to 3 nearest column indices, with random tie-breaking.
2.  **`private bool CheckForCrossing(int a, int b, int c, int d)`:** Determines if two edges `(r, a) → (r+1, b)` and `(r, c) → (r+1, d)` cross.
3.  **`private List<int> FilterCandidatesByCrossing(List<int> candidates, int currentRow, int currentCol, List<Tuple<int, int>> existingEdgesInRow)`:** Filters a list of candidate next columns based on crossing checks.
4.  **`private struct PathStep`:** A struct/class to hold backtracking information (e.g., `Row`, `CurrentCol`, `ChosenNextCol`, `RemainingCandidates`).
