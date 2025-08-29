# Gemini Prompt — Rarity Weighting Redesign (Milestones + Elite Modifier)

## ROLE
- You are a senior Unity C# gameplay engineer + tech designer; 

## IMPORTANT
- Before changing any code, first output brief assumptions + a high-level plan (≤3 bullets), build a symbol map (classes/methods/fields/properties/events with full signatures + file paths) and a dependency/call graph from the provided files; list all inbound/outbound references for anything you’ll touch, produce a RefUpdate checklist, and **wait** for my approval; if context is missing, request exact files by path; do not invent symbols.

## Task
Redesign our item **rarity weighting** to use a **milestone/keyframe** system. Briefly compare it to our current system, then specify rules and provide pseudocode.

## Context (Current System)
- 4 rarities: **Bronze, Silver, Gold, Diamond**.
- Floors are **0-indexed**; run length is **15 floors (0..14)**.
- Per rarity we currently define: `FloorAvailable`, `FloorUnavailable`, `StartingWeight`, `EndingWeight`.
- If current floor is inside that window, weight is linearly interpolated from `StartingWeight → EndingWeight`; otherwise weight = 0.
- Per-floor probabilities are computed by **normalizing** the weights across all rarities.

## New System: Milestone / Keyframe Table
Replace the per-rarity start/end fields with a **single table** of milestone floors and **weights** for each rarity.

- **Milestone floors**: ascending list that **brackets the run** (first ≤ 0, last ≥ 14).
- At each milestone, give a **weight** per rarity. Weights are *relative* (they **don’t have to sum to 100**).
- For any floor `f`:
  1. Find milestone pair `Fk ≤ f ≤ Fk+1`.
  2. Compute `t = (f - Fk) / (Fk+1 - Fk)`.
  3. For each rarity `r`, interpolate: `w_r(f) = (1 - t) * w_r(Fk) + t * w_r(Fk+1)`.
  4. **Normalize** to probabilities: `p_r(f) = w_r(f) / Σ_j w_j(f)`.
- Clamp outside range: floors `< first milestone` use the first row; floors `> last milestone` use the last row.
- **Zeros are allowed** (they gate a rarity); **negative weights are not**.

**Example structure (illustrative only):**
```
Milestones (floors): [0, 5, 10, 14]
Weights:
  Floor 0:  Bronze=80, Silver=20, Gold=0,  Diamond=0
  Floor 5:  Bronze=40, Silver=30, Gold=30, Diamond=0
  Floor 10: Bronze=20, Silver=30, Gold=40, Diamond=20
  Floor 14: Bronze=0,  Silver=10, Gold=40, Diamond=50
```

## Elite Rarity Modifier (Floor Shift)
Add an integer `EliteModifier`.
- When generating an **Elite** item, use an **effective floor**:
  `f_eff = clamp(floor + EliteModifier, first_milestone, last_milestone)`
- Compute weights/probabilities using `f_eff` instead of `floor`.  
  (Example: `EliteModifier = 2` on floor 7 → `f_eff = 9`, increasing higher-tier odds if the table ramps up.)

## Deliverables
- All relevant backend systems updated to reflect these changes
- Updated RunConfigSOEditor layout

---
