# Refactoring Plan for `GenerateSkeleton` Function

This document outlines the necessary steps to refactor the `GenerateSkeleton` function in `Assets/Scripts/MapGeneration/MapGenerator.cs` to align with the new specification for parametric row/column map skeleton generation.

## Current Functionality (`MapGenerator.cs:GenerateSkeleton`)

The existing `GenerateSkeleton` function operates as follows:

*   **Node Creation:**
    *   It iterates through rows (`r`).
    *   For the first (`r=0`) and last (`r=actSpec.Rows-1`) rows, exactly one node is created.
    *   For intermediate rows, a random number of nodes (between 2 and `actSpec.Columns-1`) is created.
    *   The `Col` property of a `Node` is a sequential index *within that row* (e.g., `node_r_0`, `node_r_1`), not a fixed global column position.
*   **Edge Wiring:**
    *   It iterates through rows to connect nodes between the current and next row.
    *   It ensures every node in the next row has at least one incoming connection from the current row.
    *   It ensures every node in the current row has at least one outgoing connection to the next row.
    *   Additional "branching" connections are added randomly based on `actSpec.Branchiness`.
    *   There is no concept of "crossing" edges or specific path generation.
*   **Boss Node Assignment:**
    *   The node in the very last row is explicitly assigned `NodeType.Boss`.

## New Specification (`.gemini/map_skeleton_refactor_essential_spec_parametric_rows_cols.md`)

The new specification dictates a different approach:

*   **Grid (Parametric):** A `C x R` grid where `C = actSpec.Columns` and `R = actSpec.Rows`. `Col` (`c`) is a fixed horizontal index from `0` to `C-1`.
*   **Goal:** Generate **six monotone upward paths** from `r=0` to `rTop` on this `C x R` grid.
*   **Path Building Rules:**
    *   **Start:** Choose a start column uniformly in `[0..C-1]`. The first two paths must have different start columns.
    *   **Step `r → r+1`:** From `(r, c)`, move to one of the **three nearest columns** on row `r+1` (by `|c' - c|`, random tie-breaks).
    *   **No Crossings:** A new edge `(r, c) → (r+1, c')` must not cross any existing edge between `r` and `r+1`. Merging at nodes and reusing identical edges are allowed.
*   **Pruning:** After all 6 paths are built, delete every node/edge not on at least one of the paths.
*   **Failure Handling:** Bounded backtracking with a decision stack. If stack empties, restart the current path (capped restarts).
*   **Determinism:** All randomness must use the provided `IRandomNumberGenerator` for tie-breaking and choices.

## Discrepancies and Refactoring Needs

The core logic of `GenerateSkeleton` needs to be almost entirely replaced. The current implementation's variable node count per row and random connection strategy are incompatible with the fixed grid and structured path generation required by the new specification.

Here's a breakdown of the key discrepancies and the corresponding refactoring needs:

1.  **Node Creation Philosophy:**
    *   **Current:** Variable nodes per row; `Col` is a relative index.
    *   **New Spec:** Fixed `C x R` grid; `Col` is a global index.
    *   **Refactoring:** The initial node creation loop must be modified to create `actSpec.Columns` nodes for *every* row, ensuring `Node.Col` represents its true column index on the grid.

2.  **Edge Wiring vs. Path Generation:**
    *   **Current:** Focuses on ensuring basic connectivity and adding random branchiness.
    *   **New Spec:** Requires explicit generation of 6 distinct, monotone upward paths with specific rules.
    *   **Refactoring:** The entire existing edge wiring logic must be removed and replaced with a new algorithm that iteratively builds each of the 6 paths.

3.  **Crossing Check:**
    *   **Current:** No crossing detection.
    *   **New Spec:** Strict "no crossings" rule.
    *   **Refactoring:** A new mechanism is needed to store existing edges for each row segment (`edgesByRow[r]`) and a function to check if a candidate edge would cross any of these.

4.  **Candidate Selection:**
    *   **Current:** Random selection for connections.
    *   **New Spec:** "Three nearest" columns selection with distance-based sorting and random tie-breaking.
    *   **Refactoring:** A dedicated helper function is required to identify and filter these candidates.

5.  **Pruning:**
    *   **Current:** All generated nodes/edges are kept.
    *   **New Spec:** Unused nodes/edges must be removed after path generation.
    *   **Refactoring:** A post-processing step is needed to identify and remove nodes and edges that are not part of any of the 6 generated paths.

6.  **Boss Node Assignment:**
    *   **Current:** `GenerateSkeleton` explicitly sets the boss node type.
    *   **New Spec:** Boss node assignment is a typing concern (Phase B), not part of skeleton generation (Phase A).
    *   **Refactoring:** The line setting `NodeType.Boss` in `GenerateSkeleton` must be removed.

## Detailed Refactoring Steps

The following steps outline the refactoring process for the `GenerateSkeleton` function in `D:/Unity_Projects/Pirate_Autobattler/Pirate_Autobattler/Assets/Scripts/MapGeneration/MapGenerator.cs`.

### I. Initial Setup and Data Structures

1.  **Modify Node Creation (Phase A.1):**
    *   Locate the first `for` loop in `GenerateSkeleton` that creates nodes.
    *   Change the inner loop to iterate `c` from `0` to `actSpec.Columns - 1` for *every* row `r`.
    *   Ensure the `Node.Col` property is set to this global `c` index.

    ```csharp
    // BEFORE:
    // for (int r = 0; r < actSpec.Rows; r++)
    // {
    //     int nodesInRow = (r == 0 || r == actSpec.Rows - 1) ? 1 : (int)(rng.NextULong() % (ulong)(actSpec.Columns - 2)) + 2;
    //     if (r == 0) nodesInRow = 1;
    //     if (r == actSpec.Rows - 1) nodesInRow = 1;
    //     for (int c = 0; c < nodesInRow; c++)
    //     {
    //         graph.Nodes.Add(new Node
    //         {
    //             Id = $"node_{r}_{c}",
    //             Row = r,
    //             Col = c, // This 'c' is relative to the row
    //             Type = NodeType.Unknown
    //         });
    //     }
    // }

    // AFTER:
    for (int r = 0; r < actSpec.Rows; r++)
    {
        for (int c = 0; c < actSpec.Columns; c++)
        {
            graph.Nodes.Add(new Node
            {
                Id = $"node_{r}_{c}",
                Row = r,
                Col = c, // This 'c' is now the global column index
                Type = NodeType.Unknown
            });
        }
    }
    ```

2.  **Remove Boss Node Assignment:**
    *   Locate and remove the lines at the end of `GenerateSkeleton` that explicitly set the boss node type.

    ```csharp
    // REMOVE THESE LINES:
    // Node bossNode = graph.Nodes.FirstOrDefault(n => n.Row == actSpec.Rows - 1);
    // if (bossNode != null)
    // {
    //     bossNode.Type = NodeType.Boss;
    // }
    ```

3.  **Introduce Helper Data Structures:**
    *   Inside `GenerateSkeleton`, declare the following:
        *   `HashSet<string> usedNodeIds = new HashSet<string>();`: To track nodes that are part of any generated path.
        *   `HashSet<string> usedEdgeIds = new HashSet<string>();`: To track edges that are part of any generated path.
        *   `List<Tuple<int, int>>[] edgesByRow = new List<Tuple<int, int>>[actSpec.Rows - 1];`: An array to store `(srcCol, dstCol)` tuples for edges in each row segment, used for crossing checks. Initialize each element as a new `List<Tuple<int, int>>`.
        *   `Stack<PathStep> pathDecisionStack`: A custom struct/class `PathStep` will be needed to store backtracking information (e.g., `row`, `currentCol`, `chosenNextCol`, `remainingCandidates`).

### II. Path Generation Logic (Core Refactoring)

This section replaces the entire "Wire edges" section of the current `GenerateSkeleton` function.

1.  **Outer Loop for 6 Paths:**
    *   Introduce a loop to generate 6 paths.

    ```csharp
    // Replace the entire existing edge wiring logic (from "for (int r = 0; r < actSpec.Rows - 1; r++)" onwards) with this:

    List<int> pathStartColumns = new List<int>(); // To ensure first two starts differ

    for (int p = 0; p < 6; p++) // Generate 6 paths
    {
        // Path generation logic for a single path will go here
        // ... (see subsequent steps)
    }
    ```

2.  **Path Generation (Inside the `for (int p = 0; p < 6; p++)` loop):**

    *   **Path Restart Mechanism:**
        *   Implement a `pathRestarts` counter (e.g., `int pathRestarts = 0;`).
        *   Wrap the path generation logic in a `while (true)` loop that breaks on successful path completion or `pathRestarts` exceeding a cap (e.g., 10).

    *   **Start Column Selection:**
        *   `int startCol;`
        *   If `p == 0`: `startCol = (int)(rng.NextULong() % (ulong)actSpec.Columns);`
        *   If `p == 1`:
            *   `do { startCol = (int)(rng.NextULong() % (ulong)actSpec.Columns); } while (startCol == pathStartColumns[0]);`
        *   Else (`p > 1`): `startCol = (int)(rng.NextULong() % (ulong)actSpec.Columns);`
        *   Add `startCol` to `pathStartColumns`.
        *   `int currentCol = startCol;`
        *   `pathDecisionStack.Clear();` // Clear stack for new path

    *   **Iterate Rows (`r = 0` to `actSpec.Rows - 2`):**
        *   Inside this loop, for each step from `(r, currentCol)` to `(r+1, chosenNextCol)`:
            *   **Candidate Selection (`GetThreeNearestCandidates`):**
                *   Call a helper method (to be implemented later) to get up to 3 nearest columns on row `r+1`.
                *   `List<int> candidates = GetThreeNearestCandidates(currentCol, actSpec.Columns, rng);`
            *   **Crossing Check (`FilterCandidatesByCrossing`):**
                *   Call a helper method (to be implemented later) to filter `candidates` based on existing edges in `edgesByRow[r]`.
                *   `List<int> filteredCandidates = FilterCandidatesByCrossing(candidates, r, currentCol, edgesByRow[r]);`
            *   **Decision and Edge Addition (with Backtracking):**
                *   If `filteredCandidates.Any()`:
                    *   Choose `chosenNextCol` uniformly at random from `filteredCandidates` using `rng`.
                    *   Add the edge `(node_{r}_{currentCol}, node_{r+1}_{chosenNextCol})` to `graph.Edges`.
                    *   Add `(currentCol, chosenNextCol)` to `edgesByRow[r]`.
                    *   Mark `node_{r}_{currentCol}` and `node_{r+1}_{chosenNextCol}` as `usedNodeIds`.
                    *   Mark the newly created edge as `usedEdgeIds`.
                    *   Push `PathStep` onto `pathDecisionStack` (containing `r`, `currentCol`, `chosenNextCol`, and `remainingCandidates`).
                    *   `currentCol = chosenNextCol;`
                *   Else (no valid candidates):
                    *   **Backtrack:**
                        *   `pathRestarts++;`
                        *   If `pathRestarts` exceeds cap, `break` the inner `while` loop to restart the entire path.
                        *   While `pathDecisionStack` is not empty and `pathDecisionStack.Peek().RemainingCandidates.Count == 0`:
                            *   Pop `PathStep` from stack.
                            *   Remove corresponding edge from `graph.Edges` and `edgesByRow[poppedStep.Row]`.
                            *   Unmark nodes/edges from `usedNodeIds`/`usedEdgeIds` if they are no longer part of any path (this might require more complex tracking or a full re-prune after backtracking). For simplicity, initially, just remove the edge.
                        *   If `pathDecisionStack` is empty after popping, restart the path (break inner `while` loop).
                        *   Else:
                            *   Get `lastStep = pathDecisionStack.Pop()`.
                            *   Remove `lastStep.ChosenNextCol` from `lastStep.RemainingCandidates`.
                            *   Remove the edge corresponding to `lastStep` from `graph.Edges` and `edgesByRow[lastStep.Row]`.
                            *   `currentCol = lastStep.CurrentCol;`
                            *   `r = lastStep.Row - 1;` (decrement `r` so the outer loop re-evaluates this row)
                            *   `continue;` (to re-evaluate the current row with new options)

### III. Pruning

1.  **After the `for (int p = 0; p < 6; p++)` loop completes:**
    *   Create new lists for `graph.Nodes` and `graph.Edges`.
    *   Iterate through the original `graph.Nodes`. If a node's `Id` is in `usedNodeIds`, add it to the new `graph.Nodes` list.
    *   Iterate through the original `graph.Edges`. If an edge's `Id` (or `FromId`/`ToId` pair) is in `usedEdgeIds`, add it to the new `graph.Edges` list.
    *   Assign these new lists back to `graph.Nodes` and `graph.Edges`.

### IV. Helper Methods (New or Modified)

These methods should be added as private methods within the `MapGenerator` class.

1.  **`private List<int> GetThreeNearestCandidates(int currentCol, int totalColumns, IRandomNumberGenerator rng)`:**
    *   Calculates distances from `currentCol` to all other columns `0` to `totalColumns - 1`.
    *   Sorts columns by distance.
    *   Uses `rng` for tie-breaking if distances are equal.
    *   Returns the first up to 3 unique column indices.

    ```csharp
    private List<int> GetThreeNearestCandidates(int currentCol, int totalColumns, IRandomNumberGenerator rng)
    {
        List<Tuple<int, int>> distances = new List<Tuple<int, int>>(); // Item1: column, Item2: distance

        for (int c = 0; c < totalColumns; c++)
        {
            distances.Add(Tuple.Create(c, Math.Abs(c - currentCol)));
        }

        // Sort by distance, then by a random tie-breaker
        // Using a custom comparer for deterministic random tie-breaking
        distances.Sort((a, b) =>
        {
            int distCompare = a.Item2.CompareTo(b.Item2);
            if (distCompare != 0) return distCompare;
            // Deterministic tie-break: use RNG to decide order if distances are equal
            return (rng.NextULong() % 2 == 0) ? 1 : -1; // Randomly sort if distances are equal
        });

        return distances.Take(3).Select(t => t.Item1).ToList();
    }
    ```

2.  **`private bool CheckForCrossing(int a, int b, int c, int d)`:**
    *   Checks if two edges `(r, a) → (r+1, b)` and `(r, c) → (r+1, d)` cross.

    ```csharp
    private bool CheckForCrossing(int a, int b, int c, int d)
    {
        // If they share an endpoint, they do not cross (merge/travel allowed)
        if (a == c || b == d) return false;

        // They cross if their source-column order and target-column order are opposite
        return (a < c && b > d) || (a > c && b < d);
    }
    ```

3.  **`private List<int> FilterCandidatesByCrossing(List<int> candidates, int currentRow, int currentCol, List<Tuple<int, int>> existingEdgesInRow)`:**
    *   Filters a list of `candidates` (potential `chosenNextCol`) by checking for crossings with `existingEdgesInRow`.

    ```csharp
    private List<int> FilterCandidatesByCrossing(List<int> candidates, int currentRow, int currentCol, List<Tuple<int, int>> existingEdgesInRow)
    {
        List<int> filtered = new List<int>();
        foreach (int candidateNextCol in candidates)
        {
            bool crossesExisting = false;
            foreach (var existingEdge in existingEdgesInRow)
            {
                if (CheckForCrossing(currentCol, candidateNextCol, existingEdge.Item1, existingEdge.Item2))
                {
                    crossesExisting = true;
                    break;
                }
            }
            if (!crossesExisting)
            {
                filtered.Add(candidateNextCol);
            }
        }
        return filtered;
    }
    ```

4.  **`private struct PathStep` (or class):**
    *   A simple data structure for the backtracking stack.

    ```csharp
    // Define this struct/class within MapGenerator or as a nested type
    private struct PathStep
    {
        public int Row;
        public int CurrentCol;
        public int ChosenNextCol;
        public List<int> RemainingCandidates; // Candidates that were not chosen yet for this step
    }
    ```

### V. Acceptance Checks (Post-Refactoring Verification)

These are crucial for testing the refactored `GenerateSkeleton` function. They should be implemented in unit tests or a separate validation utility.

*   **Start Diversity:** Verify that the first two generated paths start in different columns.
*   **Six Paths:** Confirm that exactly 6 monotone upward paths exist from `r=0` to `rTop`.
*   **No Crossings:** For every row `r`, ensure no pair of edges violates the crossing rule.
*   **Three-Nearest Honored:** For every step in every path, verify that the chosen target column was indeed one of the (up to) three nearest valid candidates.
*   **Pruned:** Confirm that no nodes or edges exist in the final `MapGraph` that are not part of at least one of the 6 paths.

This refactoring is a significant undertaking, as it replaces the fundamental generation algorithm. Careful implementation and rigorous testing will be essential.
