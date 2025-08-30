# Map Skeleton Refactor — Essential Spec (Parametric Rows/Cols)

**Grid (parametric):**
- Columns `C = actSpec.Columns` → `c ∈ [0 .. C-1]`
- Rows `R = actSpec.Rows` → `r ∈ [0 .. R-1]`
- `r = 0` is the bottom (first floor). `rTop = R-1` is the top.

**Goal:** Replace skeleton creation with **six monotone upward paths** from `r=0` to `rTop` on an `C×R` grid, then **prune** unused nodes/edges. Downstream nodeType assignment remains unchanged.

---

## Functional Rules
1. **Prebuild grid** of `C×R` candidate nodes `(r, c)`; no edges yet.
2. **Build 6 paths, one at a time:**
   - **Start (row 0):** choose a start column uniformly in `[0..C-1]`. Enforce that the **first two starts are different columns**.
   - **Step `r → r+1`:** from `(r, c)` move to **one of the 3 nearest columns on row `r+1`** (by `|c' − c|`, random tie‑breaks).
   - **No crossings:** a new edge `(r, c) → (r+1, c')` **must not cross** any existing edge between `r` and `r+1`. **Merging** at nodes (shared endpoints) and **reusing identical edges** are allowed.
   - Continue until reaching `rTop`.
3. **Prune:** After all 6 paths, **delete** every node/edge not on at least one of the paths.

**Boundary note:** "Three nearest" means **up to** three distinct columns in `[0..C-1]` with minimal `|c'−c|`. If fewer than three exist (e.g., small `C`), use what’s available.

---

## Crossing Check (adjacent‑row edges)
All edges go strictly from `r` to `r+1`. Two edges **cross** iff their source‑column order and target‑column order are opposite.

Given placed `e = (r, a) → (r+1, b)` and candidate `x = (r, c) → (r+1, d)`:
- If **share endpoint** (`a == c` or `b == d`): **not** a crossing (merge/travel allowed).
- Else they cross iff `(a < c && b > d) || (a > c && b < d)`.

Maintain `edgesByRow[r] = [(srcCol, dstCol), …]` for checks on each row segment.

---

## Candidate Selection (three‑nearest)
For current `(r, c)`:
1. Compute columns `0..C-1` sorted by `|col − c|` (tie‑break randomly).
2. Take the **first up to 3** as `candidates`.
3. Filter with **Crossing Check**.
4. If ≥1 remain, pick **uniformly at random**.

---

## Failure Handling (bounded backtracking)
If all candidates at a step are invalid:
- **Backtrack** using a small decision stack (e.g., last 4–6 rows): pop a step, remove its edge, and try its remaining alternatives.
- If a step exhausts options, continue popping.
- If the stack empties, **restart the current path** (choose a new `r=0` start; still ensure the **first two starts differ** overall). Cap restarts (e.g., 10).

This preserves the strict **“choose among the (up to) 3 nearest”** rule and ensures progress.

---

## Algorithm (concise)
1) **Grid:** create all nodes `(r, c)` for `r ∈ [0..R-1]`, `c ∈ [0..C-1]`.
2) **For p in 1..6:**
   - Pick `startCol ∈ [0..C-1]` (the first two paths must have different `startCol`).
   - For `r = 0..(R-2)`:
     - `candidates = (up to) three‑nearest columns on r+1` from current `c` by `|c'−c|`.
     - Remove candidates that would **cross** anything in `edgesByRow[r]`.
     - If none → **backtrack**; if exhausted → **restart** path.
     - Else choose one uniformly, **add edge**, append `(c, chosen)` to `edgesByRow[r]`, set `c = chosen`.
3) **Prune:** remove nodes/edges not used by any of the 6 paths.

---

## Determinism
All randomness must flow through the project RNG (seed‑deterministic). Use RNG for tie‑breaking in distance sorts and among‑candidate choices.

---

## Acceptance Checks (postconditions)
- **Start diversity:** the first two path start columns differ.
- **Six paths:** there are 6 monotone `r=0 → rTop` paths.
- **No crossings:** for every row `r`, no pair of edges violates the inversion rule.
- **Three‑nearest honored:** every step’s target column is within the step’s (up to) top‑3 nearest.
- **Pruned:** no stray nodes/edges remain outside those paths.

