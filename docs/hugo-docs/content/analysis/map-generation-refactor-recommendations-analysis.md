---
title: "Map Generation Refactor Recommendations"
weight: 10
system: ["map"]
types: ["analysis", "recommendation", "plan","system-overview"]
status: "archived"
discipline: ["engineering", "design"]
stage: ["pre-production"]
---

# Map Generation Refactor Recommendations

## Problem/Topic

Recommendations for refactoring the map generation system to stabilize the weighted typing pass, implement robust re-roll validation, refine the repair policy, and improve testing and telemetry.

## Analysis

### Executive Summary

The primary focus should be on stabilizing the weighted typing pass by implementing robust re-roll validation and refining the repair policy. Concurrently, the system needs comprehensive testing, improved telemetry, and finalization of configuration schema for per-row-band generation odds.

### Detailed Recommendations

### Epic A: Config & Schema (Addressing "Per-Row-Band Generation Odds")

*   **A1: Rules SO fields + inspector UI (tooltips, ranges)**
    *   **Action:** Verify `RulesSO` contains a mechanism for `GenerationOdds` that supports per-row-band definitions. This likely means a `Dictionary<RowBand, OddsTable>` or a similar structure.
    *   **Verification:** Check the `RulesSO` definition and its custom editor (if any) to ensure these fields are exposed and configurable in the Unity Inspector with appropriate tooltips.
*   **A2: StructuralBans + serialization**
    *   **Action:** Confirm `RulesSO` includes fields for `StructuralBans` (e.g., `NoPortOnRowRMinus2`, `EliteUnlockRow`).
    *   **Verification:** Ensure these bans are correctly serialized and deserialized.
*   **A3: Pity counters in ActContext + save/load**
    *   **Action:** Verify that the `ActContext` (or equivalent game state object) correctly tracks pity counters for Battle, Treasure, and Shop.
    *   **Verification:** Confirm these counters persist across game sessions (save/load).

### Epic B: Fixed Rows & Connectivity (Partially addressed, but review needed)

*   **B1: Pre-assignment for rows 1, ⌈0.6Rão, R-1, R**
    *   **Action:** Review the existing pre-assignment logic to ensure it precisely matches the plan:
        *   Row 1 → **Monster** (locked)
        *   `treasureRow` (clamped `round(0.6 * R)`) → **Treasure** (all nodes locked)
        *   Row `R-1` → **Port** (all nodes locked)
        *   Row `R` → **Boss** (single node, locked)
    *   **Verification:** Generate maps with varying `R` values and visually inspect the fixed rows.
*   **B2: Boss creation + edges from R-1**
    *   **Action:** Confirm that the single Boss node is correctly created on Row `R` and *only* nodes from Row `R-1` connect to it.
    *   **Verification:** Inspect generated map graph data to ensure correct connectivity.
*   **B3: Row R-2 Port ban**
    *   **Action:** Verify that the hard ban on **Port** nodes for row `R-2` is correctly enforced during pre-assignment or initial weighting.
    *   **Verification:** Generate maps and confirm no Port nodes appear on `R-2`.

### Epic C: Weighted Typing + Validator (Addressing "Re-roll Validation")

*   **C1: GenerationOdds by row band**
    *   **Action:** Modify the node typing logic to pull weights from the `GenerationOdds` table based on the current node's row band, as defined in `RulesSO`.
    *   **Verification:** Observe generated map distributions to ensure weights are applied correctly per row band.
*   **C2: Validator (bans, consecutive, split uniqueness, uniform-row exception)**
    *   **Action:** This is the highest priority. Enhance the validation logic within the weighted typing pass (`AssignNodeTypesWeighted` or similar) to include:
        *   **Row bans:** Re-check `NoPortOnRowRMinus2` and `EliteUnlockRow` (no Elite before `eliteUnlockRow`).
        *   **Consecutive ban:** Implement checks to disallow Elite→Elite, Shop→Shop, Port→Port along any path from the current node's parents.
        *   **Child uniqueness:** For nodes with multiple children, ensure children have different types, *except* when the destination row is uniform by design (e.g., Treasure row, pre-boss Port row).
    *   **Verification:** Implement unit tests for each validation rule. Run extensive map generation tests and log any violations.
*   **C3: Re-roll with fallback**
    *   **Action:** Implement a robust re-roll mechanism. If a sampled node type fails validation, re-sample up to `N` attempts. If `N` attempts fail, fallback to **Monster**. Log instances of fallback.
    *   **Verification:** Monitor re-roll rates and fallback occurrences. Ensure map generation completes without infinite loops due to invalid types.

### Epic D: Unknown Resolution (Review and ensure alignment with plan)

*   **D1: Resolver with sequential procs and pity**
    *   **Action:** Review the existing Unknown node resolution logic to ensure it:
        *   Computes effective chances with pity (clamped).
        *   Sequentially rolls for Battle, Treasure, Shop (in that order).
        *   Falls back to **Event** if none proc.
    *   **Verification:** Simulate Unknown resolutions with varying pity counters and verify outcomes.
*   **D2: Structural-ban check within resolver**
    *   **Action:** Confirm that the Unknown resolver respects structural bans (e.g., Port-ban on R-2) by skipping banned candidates, but still increments pity for skipped types.
    *   **Verification:** Test Unknown nodes on `R-2` to ensure they never resolve to Port.
*   **D3: Telemetry for resolves**
    *   **Action:** Implement logging for pity vectors at each `Unknown` resolve.
    *   **Verification:** Verify telemetry data accurately reflects pity changes.

### Epic E: QA/Telemetry/Tests (Addressing "Boss Edge Validation", "Sophisticated Repair Policy", "Comprehensive Testing", "Telemetry & QA Hooks")

*   **E1: Integrity checks + error surfacing**
    *   **Action:** Implement comprehensive post-generation integrity checks:
        *   All row `R-1` Ports have an edge to Boss; no other row connects to Boss.
        *   DAG property holds; all starts reach Boss.
        *   No **Rest/Port** on row `R-2` (re-check after all passes).
    *   **Verification:** Ensure violations raise explicit errors with node coordinates for easy debugging.
*   **E2: Unit & integration suites listed in Phase 7**
    *   **Action:** Develop a comprehensive test suite covering:
        *   **Fixed rows:** Row 1 Monsters, row `⌈0.6R⌉` Treasure, row `R-1` all Port, row `R` Boss with correct edges.
        *   **Row ban:** No Port on row `R-2`.
        *   **Adjacency:** No Elite→Elite, Shop→Shop, Port→Port along any path.
        *   **Split uniqueness:** Enforced except when destination row is uniform.
        *   **Elite unlock:** No Elite before `⌈0.35R⌉`.
        *   **Unknown baseline:** 10/2/3% with Event fallback in Monte Carlo.
        *   **Unknown pity:** Chances increase linearly per visit until cap; pity resets on chosen type.
        *   **Resolution independence:** Allow Shop→Unknown→Shop adjacency after resolution.
        *   **Determinism:** Identical outcomes with same seed and traversal order.
    *   **Verification:** All tests pass.
*   **E3: Debug hooks & seed tools**
    *   **Action:** Implement debug commands for:
        *   Seed override (to reproduce specific map generations).
        *   Force next Unknown outcome (for testing specific resolution paths).
        *   Print path adjacency for a sampled path (for debugging connectivity).
    *   **Verification:** Confirm these tools function as expected.

### Epic F: Docs (Post-implementation)

*   **F1: README + tuning guide**
    *   **Action:** After all implementation and testing is complete, update the developer README with:
        *   Explanation of row anchors & bans, odds tables, re-roll flow, validator order.
        *   Detailed Unknown resolution algorithm, pity math, scopes, and examples.
        *   A tuning guide for designers (weights by row band, pity tuning, elite unlock shifts).
    *   **Verification:** A new developer or designer should be able to understand and tune the system using only the updated documentation.

### Discrepancies & Clarifications

*   **`MapValidator.AreCountsWithinLimits` vs. Telemetry:**
    *   **Clarification Needed:** The plan emphasizes "no global hard targets" for typing, yet telemetry still expects "per-type counts." It needs to be decided if telemetry should align with the "no global hard targets" philosophy (focusing on re-roll rates and pity vectors) or if there's a specific reason to track per-type counts despite not enforcing them during generation.
    *   **Recommendation:** If per-type counts are still desired for telemetry, ensure they are collected *after* generation and repair, reflecting the final map composition, rather than being a validation target during generation.
*   **`AreSpacingRulesRespected` vs. "along any path":**
    *   **Action:** The current `AreSpacingRulesRespected` checks vertical spacing between *any two nodes of the same type*. The plan specifies "Along any path: no Elite→Elite, Shop→Shop, Port→Port consecutives." This implies a path-tracing check rather than a simple vertical check.
    *   **Recommendation:** Refactor `AreSpacingRulesRespected` (or create a new validation method) to traverse paths from a given node and check for consecutive type bans along those paths. This is a critical part of the "Consecutive ban" validation in Phase 2.

## Conclusion/Recommendations

This document provides a comprehensive set of recommendations for improving the map generation system, focusing on robustness, testability, and configurability.