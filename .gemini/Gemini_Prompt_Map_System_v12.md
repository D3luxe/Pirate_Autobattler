# Prompt for Gemini — Recreate the Act Map System (Parity with PRD v12)

**Role:** You are a senior game systems engineer.  
**Objective:** **Recreate** the act map system for a Slay-the-Spire–style *Pirate Autobattler*, end-to-end and **in parity with PRD v12**. Implement deterministic generation, node typing/guarantees, Unknown-node pity resolution, validation & repair, serialization, and tests—cleanly factored and production-ready.

---

## Target Tech (fill already chosen)
- **LANGUAGE/ENGINE:** C# (Unity 2022 LTS or newer)  
- **PACKAGE FORMAT:** Unity package + demo scene + headless test harness (EditMode + PlayMode tests)  
- **TARGET PLATFORM:** Windows/Mac/Linux Desktop  
- **PRNG:** 64-bit Xoshiro256** with SplitMix64 for seed splitting (or equivalent, but keep behavior stable and documented)

> **Important:** All rules and behaviors below must be **data-driven** and **deterministic**, matching the PRD v12 guarantees. Do not rely on non-deterministic iteration orders.

---

## Functional Requirements (Map & Flow)

1. **Layered, One-Way Map**
   - The map is a layered DAG (rows). Movement is **one row forward per step**; no backtracking.
   - **Boss preview** is visible at the top from the start of the act.
   - There must be **at least one valid path** from the starting row to the Boss.

2. **Node Types**
   - `Battle`, `Elite`, `Port` (healing), `Shop`, `Treasure` (Cache), `Event`, `Unknown (?)`, `Boss`.
   - **Elite** is a formal node type; may be flagged `burning=true` when the feature flag is enabled.
   - **Unknown (?)** resolves at runtime to `Event | Battle | Shop | Treasure` via weighted randomness with a **per-act pity** system.

3. **Design Guarantees (per act)**
   - **Pre-boss Port:** At least one **Port** is present on the **row immediately before the Boss**.
   - **Mid-act Treasure:** At least one **Treasure** spawns within a configurable **mid-act row window**.
   - **Spacing & Density:** Enforce type counts (min/max/targets), spacing (e.g., elite/shop/port gaps), and an **early-elite cap**.

4. **Optional Meta Flags**
   - `enableBurningElites`: Spawns one marked **Burning Elite** per act (until claimed) with special rewards.
   - `enableMetaKeys`: (optional extension) Meta goals (e.g., keys) can be hooked into Port/Treasure/Burning Elite if enabled. Default **off** to match PRD.

---

## Architecture & Public API (Unity/C#)

Implement a small library with these modules (namespaces suggested: `Pirate.MapGen`):

- **Generation**
  - `GenerationResult GenerateMap(ActSpec act, Rules rules, ulong seed)`

- **Typing (internal)**
  - Helpers to place/retag types under constraints.

- **Validation**
  - `AuditReport Validate(MapGraph graph, Rules rules)`

- **Repair**
  - `MapGraph Repair(MapGraph graph, Rules rules, IRng rng, int maxIterations = 50)`

- **Unknowns**
  - `UnknownOutcome ResolveUnknown(NodeId nodeId, UnknownContext ctx, IRng rng)`

- **Serialize**
  - `string Serialize(MapGraph graph, GenerationMeta meta)`
  - `(MapGraph graph, GenerationMeta meta) Deserialize(string json)`

- **Simulation / Analysis**
  - `IEnumerable<Path> EnumeratePaths(MapGraph graph)`
  - `bool IsReachableBottomToTop(MapGraph graph)`
  - `IReadOnlyList<NodeId> GetReachableNextRow(MapGraph graph, NodeId from)`

- **UI Binding (headless adapters)**
  - `UnknownForecast GetUnknownForecast(NodeId nodeId, PityState pity, Modifiers mods)`  // for tooltips
  - `BossId GetBossPreview(ActId act)`
  - `MetaKeyStatus GetMetaKeyStatus()` (enabled only if `enableMetaKeys`)

### Core Data Schemas (language-agnostic → mirror in C#)

```yaml
ActSpec:
  actId: string
  rows: int
  columns: int
  branchiness: float           # 0..1 target edges per node
  flags:
    enableMetaKeys: bool
    enableBurningElites: bool

MapGraph:
  rows: int
  nodes: Node[]                # unique id, row, col
  edges: Edge[]                # fromId -> toId (only to row+1)

Node:
  id: string
  row: int
  col: int
  type: enum(NodeType)         # Battle|Elite|Port|Shop|Treasure|Event|Unknown|Boss
  tags: string[]               # e.g., "burning", "preBossPort"
  meta: object                 # UI/icon/tooltips etc.

GenerationResult:
  graph: MapGraph
  audits: AuditReport
  seed: u64
  subSeeds:
    skeleton: u64
    typing: u64
    repairs: u64
    decorations: u64
    unknownResolution: u64
  warnings: string[]
```

---

## Rules Schema (Data-Driven — keep in `rules.json`)

```json
{
  "counts": {
    "min": { "Elite": 2, "Shop": 1, "Port": 3, "Treasure": 1 },
    "max": { "Elite": 4, "Shop": 3, "Port": 6, "Treasure": 2 },
    "targets": { "Elite": 3, "Shop": 2, "Port": 4, "Treasure": 1 }
  },
  "spacing": {
    "eliteMinGap": 2,
    "shopMinGap": 2,
    "portMinGap": 1,
    "eliteEarlyRowsCap": 4
  },
  "windows": {
    "preBossPortRow": "lastRow-1",
    "midTreasureRows": [6, 9]
  },
  "unknownWeights": {
    "start":    { "Event": 60, "Battle": 25, "Shop": 10, "Treasure": 5 },
    "pity":     { "Event":  0, "Battle":  5, "Shop":  3, "Treasure": 2 },
    "caps":     { "Event": 60, "Battle": 55, "Shop": 25, "Treasure": 15 }
  },
  "flags": {
    "enableMetaKeys": false,
    "enableBurningElites": false
  }
}
```

> Designers should be able to **tune acts via data only**. Changing rule values must not require code changes.

---

## Generation Algorithm (Phase Summary + Pseudocode)

**Phase A — Skeleton (Layered DAG)**
1. Create bottom-row spawn nodes (2–4).  
2. For each row `r → r+1`, wire edges using `branchiness`; ensure:
   - No isolated nodes; in-degree ≥1 from row 2+; out-degree ≤2 (configurable).
   - At least one path continues to the top.
3. Reserve the top row for **Boss**; mark `top-1` as **preBossPortEligible**.

**Phase B — Typing Under Constraints**
1. Place **Boss** at top; place ≥1 **Port** on `top-1` (pre-boss guarantee).  
2. Place **Treasure** in `windows.midTreasureRows`.  
3. Place **Elites** with spacing + early-row cap; flag one **Burning** when enabled.  
4. Distribute **Shops** with min spacing and avoid very early rows unless allowed.  
5. Fill remaining nodes to meet `targets`: extra **Ports** as needed → **Unknowns** → default to **Battle**.  

**Phase C — Validation & Repair**
- `Validate()` audits: path existence, pre-boss Port, mid-window Treasure, counts within min/max, spacing rules.  
- `Repair()` performs minimal local swaps/retypes and limited edge rewiring within a layer; re-validate up to N iterations; if unsatisfiable, return `AuditReport` with violations.

**Phase D — Finalization**
- Attach UI metadata (icons/tooltips), precompute reachability cache, and initialize **pity state**.  
- Persist `seed` and **sub-seeds** for deterministic replays.

---

## Unknown Node Resolution (Pity System)

**API:**  
`UnknownOutcome ResolveUnknown(NodeId nodeId, UnknownContext ctx, IRng rng)`

**Inputs:**  
- `pityState = { Event, Battle, Shop, Treasure }` for the current act.  
- `modifiers` from relics/flags/player state (multiplicative weight scalars).  
- Deterministic RNG substream: `unknownResolution`.

**Algorithm:**  
1) `weights := start + pityAccumulated`  
2) `weights := ApplyModifiers(weights, modifiers)`  
3) `outcome := SampleDiscrete(weights, rng)`  
4) Increase pity for all outcomes **≠ outcome**, clamp by `caps`  
5) Return `{ outcome, newPityState }` and persist state to save/replay

---

## Determinism & RNG Streams

- Use a platform-stable 64-bit PRNG. **Split sub-seeds** per phase:  
  - `skeleton`, `typing`, `repairs`, `decorations`, `unknownResolution`.  
- Include the root seed and **lineage** in serialized meta.  
- No dependency on hash-map iteration order or frame time.

---

## UI/UX Bindings (Headless Interfaces)

- **Reachability Highlighting:** `GetReachableNextRow(from)` for hover-highlights.  
- **Unknown Forecast Tooltips:** `GetUnknownForecast(nodeId, pityState, modifiers)` returns current weighted table.  
- **Boss Preview:** `GetBossPreview(act)` available at map start.  
- **Meta Key Status (optional):** `GetMetaKeyStatus()` only when `enableMetaKeys`.

---

## Testing & QA

1. **Determinism**
   - Same inputs (seed, rules, act spec) ⇒ **byte-stable** `GenerationResult` across platforms.  
   - Sub-seed isolation: resolving Unknowns must not retroactively change pre-finalization map.

2. **Property-Based Tests**
   - Path exists bottom→Boss; pre-boss Port present; mid-window Treasure present.  
   - Counts/spacing/early-elite-cap always satisfied.  
   - `Repair()` converges in ≤ N iterations or emits actionable `AuditReport`.

3. **Fuzzing**
   - 10k randomized seeds across safe `ActSpec`/`Rules` bounds with zero violations.

4. **Unknowns**
   - Pity increments & caps observed; long-run outcome frequencies match expected.  
   - Modifier toggles (e.g., “no hallway battles in Unknowns”) behave as intended.

5. **Golden Snapshots**
   - Check in exemplar `rules.json` + several generated acts (JSON) for regression.

---

## Deliverables

- **Unity package** with namespaces, strong types, XML docs.  
- **Demo scene** showing map generation, node icons, hover reachability, Unknown forecasts.  
- **Headless test harness** (NUnit): determinism, properties, fuzz, unknowns.  
- **Docs** (`README.md`, `RULES.md`) including how to tune acts via data only and verify determinism.  
- **Artifacts**: `rules.json`, sample serialized `GenerationResult` JSONs, and golden snapshots.
