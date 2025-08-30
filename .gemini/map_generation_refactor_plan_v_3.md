# Map Generation Refactor Plan — v3 (Guide-Compat Mode)

**Goal:** Bring the current map generation system in line with the finalized intended behavior discussed across divergences (1–7), including the updated Boss/pre‑boss rows and the Unknown→(Battle/Treasure/Shop/Event) resolution with pity. This version removes variants and legacy/feature‑flag concerns.

---

## Executive Summary of Behavior

- **Fixed rows:**
  - Row **1**: all **Monsters**.
  - Row **⌈0.6R⌉** (15-row default → row **9**): **all Treasure**.
  - Row **R-1**: **all Port** (pre‑boss rest row).
  - Row **R**: **single Boss** node; all row **R‑1** nodes connect to it.
- **Row bans:** No **Port** on row **R‑2** (to avoid Port→Port into the pre‑boss Ports).
- **Adjacency rules:** Along any path: no **Elite→Elite**, **Shop→Shop**, **Port→Port** consecutives; at splits, children must have **different types**, except when the destination row is **uniform by design** (e.g., Treasure row, pre‑boss Port row).
- **Elites unlock** from row **⌈0.35R⌉** (15-row default → row **6**).
- **Typing method:** Use **weighted odds + re‑rolls** (no global hard targets). Structural bans are enforced at sample time.
- **Unknown nodes:** Assignable at generation; on entry they resolve **independently of adjacency rules** to: **Battle**, **Treasure**, **Shop**, or fallback **Event**, using **sequential procs with pity**. Structural bans still apply at resolution (e.g., Port‑ban on R‑2).

---

## Phase 0 — Schema & Config

1) **Rules ScriptableObject additions**
   - `OddsTable GenerationOdds` (per row band; must include `Unknown`).
   - `StructuralBans` (e.g., `NoPortOnRowRMinus2`, `EliteUnlockRow`).
   - `UnknownResolutionRules Unknown` with defaults:
     - `BaseBattle=0.10`, `PityBattle=0.10`
     - `BaseTreasure=0.02`, `PityTreasure=0.02`
     - `BaseShop=0.03`, `PityShop=0.03`
     - `IgnoreAdjacency=true`, `RespectStructuralBans=true`
     - `PityCounterScope=PerAct`, caps at 1.0
2) **Graph metadata**
   - Node: `type`, `locked`, `rowIndex`.
   - Act context: pity counters (Battle, Treasure, Shop) + RNG substreams.

**Acceptance:** Inspector exposes fields with tooltips; defaults match the above values.

---

## Phase 1 — Pre‑Assignment Pass (Fixed Rows)

- Compute anchors for R rows:
  - `treasureRow = clamp(3, R-1, round(0.6 * R))`
  - `eliteUnlockRow = max(2, ceil(0.35 * R))`
- Apply fixed rows:
  - Row 1 → **Monster** (locked)
  - Row `treasureRow` → **Treasure** (all nodes locked)
  - Row `R-1` → **Port** (all nodes locked)
  - Row `R` → **Boss** (single node, locked); connect all row `R-1` nodes → Boss
- Enforce row bans:
  - **Port weight = 0** and **hard ban** on row `R-2`.

**Acceptance:** For any `R≥3`, generation produces the above layout; only row `R‑1` nodes connect to Boss; no Port on `R‑2`.

---

## Phase 2 — Weighted Typing Pass

- For every **unlocked** node (row-wise top→bottom):
  1. Pull weights from `GenerationOdds(rowBand)`.
  2. Zero weights for banned types (structural bans, elite lockout before `eliteUnlockRow`).
  3. Sample; validate; if invalid, **re‑roll up to N attempts** then fallback to **Monster**.

**Validation:**
- Check **row bans** first (e.g., Port on R‑2).
- **Consecutive ban** vs. each parent: disallow Elite→Elite, Shop→Shop, Port→Port.
- **Child uniqueness** per parent for this step, **except** when the destination row is uniform (Treasure row, pre‑boss Port row).

**Acceptance:** Typing completes without repair loops; distributions match configured odds over 1000 maps (within tolerance).

---

## Phase 3 — Unknown Resolution Engine

- On entering an `Unknown` node:
  - Compute effective chances with pity (clamped):
    - Battle, Treasure, Shop (in that order).
  - **Sequentially roll**; stop at first success.
  - If none procs → **Event**.
  - **Ignore adjacency rules** at resolution; **respect structural bans** (a banned candidate is skipped but its pity still increments).
  - **Pity update:** chosen type resets its pity to 0; the other types **+1**; Event causes all three to **+1**.
  - Persist pity according to `PityCounterScope` (default: PerAct).

**Acceptance:**
- With pity=0 and neutral structure, 10k resolves ≈ Battle 10%, Treasure 2%, Shop 3%, Event ~85%.
- Deterministic across runs with the same seed.

---

## Phase 4 — Connectivity & Integrity Checks

- Validate:
  - All row `R‑1` Ports have an edge to Boss; no other row connects to Boss.
  - DAG property holds; all starts reach Boss.
  - No **Rest/Port** on row `R‑2`.

**Acceptance:** All checks pass; violations raise explicit errors with node coordinates.

---

## Phase 5 — Repair Policy (Minimal, Rule‑First)

- If validation fails post-typing:
  - **Re‑type** only the offending nodes using the same sampling/validation.
  - Never change locked nodes; never downgrade fixed row types.
  - Avoid a blanket “convert to Monster” except as last resort after K attempts; log waivers.

**Acceptance:** Repairs converge within K attempts in >99% of randomized cases.

---

## Phase 6 — Telemetry & QA Hooks

- Emit per-map stats: per-type counts, re‑roll rates, violations, pity vectors at each `Unknown` resolve, RNG seeds.
- Debug commands: seed override; force next Unknown outcome; print path adjacency for a sampled path.

**Acceptance:** QA can reproduce any run from a single seed and inspect Unknown decisions.

---

## Phase 7 — Tests (Unit + Integration)

1. **Fixed rows:** Row 1 Monsters, row `⌈0.6R⌉` Treasure, row `R‑1` all Port, row `R` Boss with correct edges.
2. **Row ban:** No Port on row `R‑2`.
3. **Adjacency:** No Elite→Elite, Shop→Shop, Port→Port along any path.
4. **Split uniqueness:** Enforced except when destination row is uniform.
5. **Elite unlock:** No Elite before `⌈0.35R⌉`.
6. **Unknown baseline:** 10/2/3% with Event fallback in Monte Carlo.
7. **Unknown pity:** Chances increase linearly per visit until cap; pity resets on chosen type.
8. **Resolution independence:** Allow Shop→Unknown→Shop adjacency after resolution.
9. **Determinism:** Identical outcomes with same seed and traversal order.

---

## Phase 8 — Documentation

- Update developer README:
  - Row anchors & bans, odds tables, re‑roll flow, validator order.
  - Unknown resolution algorithm, pity math, scopes, and examples.
- Include a tuning guide for designers (weights by row band, pity tuning, elite unlock shifts).

**Acceptance:** A new developer can implement a theme variant or adjust weights using only the README and inspector.

---

## Work Breakdown (Epics → Stories)

**Epic A: Config & Schema**
- A1: Rules SO fields + inspector UI (tooltips, ranges)
- A2: StructuralBans + serialization
- A3: Pity counters in ActContext + save/load

**Epic B: Fixed Rows & Connectivity**
- B1: Pre‑assignment for rows 1, ⌈0.6R⌉, R‑1, R
- B2: Boss creation + edges from R‑1
- B3: Row R‑2 Port ban

**Epic C: Weighted Typing + Validator**
- C1: GenerationOdds by row band
- C2: Validator (bans, consecutive, split uniqueness, uniform‑row exception)
- C3: Re‑roll with fallback

**Epic D: Unknown Resolution**
- D1: Resolver with sequential procs and pity
- D2: Structural-ban check within resolver
- D3: Telemetry for resolves

**Epic E: QA/Telemetry/Tests**
- E1: Integrity checks + error surfacing
- E2: Unit & integration suites listed in Phase 7
- E3: Debug hooks & seed tools

**Epic F: Docs**
- F1: README + tuning guide

---

## Acceptance Gate (Definition of Done)

- All tests in Phase 7 pass.
- Monte Carlo stability: 1000 maps show no banned patterns; distributions align with config.
- QA can replicate Unknown outcomes from seed; pity behaves as documented.

---

## Open Config Questions (for later tuning)

- Should Unknown respect **any** soft budget dampeners (e.g., progressively reduce Shop after several Shops in one act)? Default: **No**.
- Elite weight shaping across bands (early=0, mid=low, late=med), exact defaults TBD.
- Designer-facing presets for different difficulty curves.

