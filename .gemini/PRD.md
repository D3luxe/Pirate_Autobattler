# Product Requirements Document

## Project: Pirate Autobattler (Working Title)
Type: 2D Roguelike Autobattler  
Engine: Unity3D

---

## 1. High-Level Vision
- **Target Audience:** Casual gamers and card game fans.  
- **Core Fantasy:** Build a pirate crew, enhance your ship, and defeat rival pirate lords.  
- **Key Differentiator:** Players can upgrade not only their crew and tools (items), but also their ship itself.  
- **Inspirations:** Slay the Spire, Super Auto Pets, The Bazaar.  

---

## 2. Game Structure
- **Run Length:** 20–30 minutes.  
- **Progression:** Player navigates a branching map with ~15 encounters before the final boss fight.  
- **Win Condition:** Defeat the Pirate Lord.  
- **Lose Condition:** Run out of lives (persistent across the run).  

---

## 3. Starter Ship System
- At the start of each run, players choose from 3 randomly offered starter ships.  
- Properties include base health, item slots, passive effects, and sometimes built-in items.  
- Ships may have unique passive mechanics tied to tags.  
- Rare ship events may offer sidegrade ships. Event-exclusive ships can only be obtained through their events.  
- Ship replacement is always direct (cannot be stored). Overflow items move to inventory.  
- **Cost:** Ships in shops cost ~2× average item price. Rare ships may be free, discounted, or require special conditions like sacrificing life or trading items.  

---

## 4. Encounters (Expanded)

### Map & Flow
- **Structure:** Each run spans ~15 encounters plus a final boss (Pirate Lord).  
- **Map Visibility:** Entire map is visible from the start. Players see only the encounter type (Battle, Event, Shop, Port, Boss), never detailed contents.  
- **Branching Rules:**  
  - Paths can branch and merge.  
  - Paths should not cross if avoidable.  
  - Every node has at least 1 inbound and 1 outbound connection (no dead ends).  
  - All paths ultimately flow toward the end boss node.

- **Layered Progression:** The map is a layered, one-way graph. Players move **one row forward per step**; no backtracking.  
- **Boss Preview:** The boss is previewed at the top from the start of the act.  
- **Design Guarantees (per act):**  
  - **Pre-boss Port:** At least one Port on the row immediately before the Boss.  
  - **Mid-act Treasure:** At least one Treasure node spawns within a configured row window.  
  - **Path Validity:** At least one valid path from starting row to Boss.  

### Encounter Types
1. **Battle**  
   - Default encounter type; majority of nodes.  
   - Enemy ship + item loadouts scale with encounter depth.  
   - Rewards: Gold (scaled) + random item choice (rarity scaled).  
   - Loss: Player loses 1 life.  

1. **Elite**  
   - Harder flagship/fleet fights with premium rewards (relics/trinkets, bonus gold).  
   - Purpose: power spikes that create run snowball potential.  
   - Spacing rules and early-row caps apply (see Rules Schema).  
   - Optional flag: **Burning Elite** (special-marked) used for meta goals (e.g., Emerald key).  

1. **Port (Healing Node)**  
   - Heals a percentage of total lives (suggested 30%).  
   - At least 1 Port should appear on each path.  
   - **Guarantee:** At least one Port appears on the **row immediately before the Boss** (pre-boss Port window).  
   - If player is at full lives, they gain a small gold bonus instead (suggested 10g).  
   - No items awarded.  

1. **Shop**  
   - Offers 3 items + 1 ship (see Shop System).  
   - Shops must appear at least once every 5 encounters on any given path.  

1. **Treasure (Cache)**  
   - Awards a non-boss relic/trinket and/or gold.  
   - **Guarantee:** At least one Treasure spawns within the configured **mid-act window** (see Rules Schema).  

1. **Event**  
   - Authored narrative choices with deterministic or weighted outcomes.  
   - May offer removals, upgrades, trades, or encounter modifiers.  

1. **Unknown (?)**  
   - Hidden node that **resolves at runtime** to Event | Battle | Shop | Treasure.  
   - Uses a **weighted table with a per-act pity system** that increases the chance of outcomes you haven’t seen recently.  
   - Can be modified by relics/flags (e.g., “no hallway battles in Unknowns”).  

1. **Boss**  
   - Final act fight. Distinct visuals/audio and reward table.  


### Unknown Node Resolution (Pity System)
- **Goal:** Make Unknowns flexible and fair by increasing the likelihood of outcomes the player hasn’t seen in this act.  
- **State:** Maintain a per-act `pityState = {Event, Battle, Shop, Treasure}`.  
- **Weights:** Start with configured `start` weights; after each Unknown resolves, increase pity for all **non-chosen** outcomes, clamped by `caps`.  
- **API:** `resolveUnknown(nodeId, context, rng) -> { outcome, newPityState }`  
- **Algorithm:**  
  1) Combine `start` + accumulated pity to get current weights.  
  2) Apply modifiers from relics/flags/player-state (multiplicative).  
  3) Sample outcome via seeded RNG.  
  4) Increase pity for outcomes ≠ chosen; clamp to caps.  
  5) Return outcome + updated pity state (persist to save).  


### Map Validation & Repair Pass
After generation, run an audit and perform minimal repairs until all guarantees pass or we fail with an actionable report.

- **Validate:**  
  - At least one bottom→boss path exists.  
  - Pre-boss Port present on the last row before Boss.  
  - Treasure present within the mid-act window.  
  - Type counts within min/max; targets approached.  
  - Spacing constraints: Elite/Shop/Port minimum gaps; early-elite cap.  

- **Repair (bounded iterations):**  
  - Local type swaps to satisfy counts/windows without breaking connectivity.  
  - Minor edge rewiring within the same layer to fix isolation or spacing.  
  - If unsatisfiable, emit an **AuditReport** listing the violated rules.  


### Rules Schema (Data-Driven)
Acts are tuned with data only; code reads these rules at runtime.

```json
{
  "counts": {
    "min": {"Elite": 2, "Shop": 1, "Port": 3, "Treasure": 1},
    "max": {"Elite": 4, "Shop": 3, "Port": 6, "Treasure": 2},
    "targets": {"Elite": 3, "Shop": 2, "Port": 4, "Treasure": 1}
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
    "start":    {"Event": 60, "Battle": 25, "Shop": 10, "Treasure": 5},
    "pity":     {"Event":  0, "Battle":  5, "Shop":  3, "Treasure": 2},
    "caps":     {"Event": 60, "Battle": 55, "Shop": 25, "Treasure": 15}
  },
  "flags": {
    "enableMetaKeys": false,
    "enableBurningElites": false
  }
}
```


### Deterministic Generation & RNG Streams
- All generation and resolution are reproducible given a seed.  
- Use a platform-stable 64-bit PRNG and **split sub-seeds** per phase to avoid cross-talk:  
  - `skeleton`, `typing`, `repairs`, `decorations`, `unknownResolution`.  
- Persist the root seed and sub-seed lineage to saves/replays.  
- No reliance on non-deterministic iteration order.  

